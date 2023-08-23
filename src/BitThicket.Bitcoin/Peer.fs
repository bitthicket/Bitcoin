module BitThicket.Bitcoin.Peer

open System
open System.Net
open System.Net.Sockets
open System.Threading.Tasks
open BitThicket.Bitcoin.Protocol

// disable warning about implicit conversions because we're going to have a lot of
// array/span/memory to ReadOnlySpan/Memory conversions
#nowarn "3391"

type PeerContext internal (uri:string, reuseSocket:bool, ?agent:string) =

    member val internal Uri = Uri(uri)
    /// this refers to the version supported by the peer on the other side of the socket; the receiver
    member val internal SupportedVersion = 0u with get,set
    member val internal Agent = defaultArg agent "Bit Thicket Badger 0.1" with get,set
    member val internal EndPoint : IPEndPoint option = None with get,set
    member val internal Socket : Socket option = Some(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)) with get,set


    interface IAsyncDisposable with
        member this.DisposeAsync() =
            task {
                match this.Socket with
                | Some socket ->
                    do! socket.DisconnectAsync(reuseSocket)
                    socket.Dispose()
                    this.Socket <- None
                | _ -> ()
            } |> ValueTask

let private resolve (context:PeerContext) force = async {
    match context.EndPoint with
    | Some ep when not force -> return ep
    | _ ->
        let! hostAddresses = Async.FromBeginEnd(
                                (fun (callback, state) -> Dns.BeginGetHostAddresses(context.Uri.Host, callback, state)),
                                Dns.EndGetHostAddresses)
        let ep = IPEndPoint(hostAddresses[0], context.Uri.Port)
        context.EndPoint <- Some ep
        return ep
}

let rec private recv (socket:Socket) buf pos len total = async {
    let! received = Async.FromBeginEnd((fun (callback, state) ->
                                            socket.BeginReceive(buf, pos, len, SocketFlags.None, callback, state)),
                                        socket.EndReceive)
    if total + received < len then
        if received = 0 then
            failwithf "Connection closed by remote host. Received %d bytes" total
            return -1 // meaningless, just to make the compiler happy
        else
            return! recv socket buf (pos + received) len (total + received)
    else
        return total + received
}

let private makeVersionMessage (socket:Socket) version services agent =
    let localEp = socket.LocalEndPoint :?> IPEndPoint
    let remoteEp = socket.RemoteEndPoint :?> IPEndPoint

    { version = version
      services = services
      timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                  |> Convert.ToUInt64
                  |> LanguagePrimitives.UInt64WithMeasure
      receiverServices = NodeServices.Unspecified
      receiverAddress = remoteEp.Address
      receiverPort = remoteEp.Port |> Convert.ToUInt16
      senderServices = services
      senderAddress = localEp.Address
      senderPort = localEp.Port |> Convert.ToUInt16
      nonce = Random.Shared.NextInt64() |> Convert.ToUInt64
      serverAgent = agent
      blockHeight = 0u }
    |> Version

/// ###### PUBLIC FUNCTIONS ######

let send (socket:Socket) data = async {
    try
        let! result = Async.FromBeginEnd((fun (callback, state) ->
                                              socket.BeginSend(data, 0, data.Length, SocketFlags.None, callback, state)),
                                         socket.EndSend)
        return Ok result
    with
    | ex -> return Error ex.Message
}

let receive (socket:Socket) (expected:Commands.Command option) = async {
    try
        let headerBuf = Array.zeroCreate Encoding.headerSize

        let! received = recv socket headerBuf 0 Encoding.headerSize 0
        if received < Encoding.headerSize then
            return Error "incomplete header received"
        elif received > Encoding.headerSize then
            return Error "received more than header size"
        else
            let header = Encoding.decodeHeader headerBuf
            let payloadLen = Convert.ToInt32(header.payloadLength)
            let payloadBuf = Array.zeroCreate payloadLen
            let! received = recv socket payloadBuf 0 payloadLen 0

            // TODO: need another function here to take a header and a byte
            // array and give back a Message

            return Unchecked.defaultof<Message> |> Ok
    with
    | ex -> return Error ex.Message
}

let connect logger uri minVersion supportedVersion services = async {
    try
        let context = PeerContext(uri, false)

        match context.Socket with
        | None -> return Error "failed to initialize socket"
        | Some socket ->
            let! ep = resolve context false
            do! Async.FromBeginEnd(
                    (fun (callback, state) -> socket.BeginConnect(ep, callback, state)),
                    socket.EndConnect)

            let versionMsg = makeVersionMessage socket supportedVersion services context.Agent
                            |> Encoding.encode

            match! send socket versionMsg with
            | Error msg -> return Error msg
            | Ok _ ->
                let! responseVersionMsg = receive socket (Some Commands.version)
                let! responseVerAckMsg = receive socket (Some Commands.verack)

                match! send socket (Encoding.encode VerAck) with
                | Error msg -> return Error msg
                | Ok _ ->
                    return Ok context
    with
    | ex -> return Error ex.Message
}