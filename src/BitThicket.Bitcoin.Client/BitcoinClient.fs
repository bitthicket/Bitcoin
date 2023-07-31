namespace BitThicket.Bitcoin.Client
open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading.Tasks

open Microsoft.Extensions.Logging
open Microsoft.Extensions.Logging.Console
open BitThicket.Bitcoin.Protocol

// disable warning about implicit conversions because we're going to have a lot of
// array/span/memory to ReadOnlySpan/Memory conversions
#nowarn "3391"

/// Represents a connection to a bitcoin node. uri should be a well-formed uri string.
/// format: "tcp://hostname:port"
/// Note: all operations are async unless otherwise indicated.
type BitcoinClient(uri:string, supportedVersion:uint32, services:NodeServices, ?minVersion:uint32, ?agent, ?logger) =
    let uri = Uri(uri)
    let minVersion = defaultArg minVersion 31800u
    let agentString = defaultArg agent "Bit Thicket Badger 0.1"
    let logger = defaultArg logger (LoggerFactory.Create(fun builder ->
                                                            builder.AddConsole()
                                                            |> ignore).CreateLogger("BitcoinClient"))

    let socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
    let mutable endpoint : IPEndPoint option = None

    let resolve (uri:Uri) force = async {
        match endpoint with
        | Some ep when not force -> return ep
        | _ ->
            let! hostAddresses = Async.FromBeginEnd(
                                    (fun (callback, state) -> Dns.BeginGetHostAddresses(uri.Host, callback, state)),
                                    Dns.EndGetHostAddresses)
            let ep = IPEndPoint(hostAddresses[0], uri.Port)
            endpoint <- Some ep
            return ep
    }

    let connect () = async {
        let! ipEndPoint = resolve uri false

        return! Async.FromBeginEnd(
            (fun (callback, state) -> socket.BeginConnect(ipEndPoint, callback, state)),
            socket.EndConnect
        )
    }

    let rec recv (socket:Socket) buf pos len totalReceived = async {
        let! received = Async.FromBeginEnd((fun (callback, state) ->
                                                socket.BeginReceive(buf, pos, len, SocketFlags.None, callback, state)),
                                            socket.EndReceive)
        if totalReceived + received < len then
            if received = 0 then
                failwithf "Connection closed by remote host. Received %d bytes" totalReceived
                return -1
            else
                return! recv socket buf (pos + received) len (totalReceived + received)
        else
            return totalReceived + received
    }

    let rec send (socket:Socket) data = async {
        let! sent = Async.FromBeginEnd((fun (callback, state) ->
                                                socket.BeginSend(data, 0, data.Length, SocketFlags.None, callback, state)),
                                        socket.EndSend)
        return sent
    }

    let makeVersionMessage version services =
        let localEp = socket.LocalEndPoint :?> IPEndPoint
        { version = version
          services = services
          timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                      |> Convert.ToUInt64
                      |> LanguagePrimitives.UInt64WithMeasure
          receiverServices = NodeServices.Unspecified
          receiverAddress = endpoint.Value.Address
          receiverPort = endpoint.Value.Port |> Convert.ToUInt16
          senderServices = services
          senderAddress = localEp.Address
          senderPort = localEp.Port |> Convert.ToUInt16
          nonce = Random.Shared.NextInt64() |> Convert.ToUInt64
          serverAgent = agentString
          blockHeight = 0u }
        |> Version

    member this.IsConnected = socket.Connected


    member this.Connect() = async {
        try
            logger.LogTrace("Connecting to {uri}", uri)
            do! connect()

            logger.LogTrace("TCP connection opened; sending version message ({version})", supportedVersion)
            let version = makeVersionMessage supportedVersion NodeServices.Unspecified
            match! this.Send(version) with
            | Ok () ->
                // wait for verack
                match! this.Receive Commands.verack with
                | Ok _ ->
                    logger.LogTrace("verack received; receiving version")
                    match! this.Receive Commands.version with
                    | Ok (Version payload) ->
                        logger.LogTrace("version received: {version}", payload.version)
                        if payload.version < minVersion then
                            logger.LogTrace("Peer does not support required minimum version")
                            return Error "Peer does not support required minimum version"
                        else
                            return! this.Send(VerAck)
                    | Ok _ ->
                        logger.LogError("Unexpected response from peer")
                        return Error "Unexpected response from peer"
                    | Error err -> return Error err
                | Error err -> return Error err
            | err ->
                logger.LogError("Error sending version message: {error}", err)
                return err
        with
            | e ->
                logger.LogError("Error connecting to {uri}: {error}", uri, e.Message)
                return Error e.Message
    }

    member this.Disconnect(?reuseSocket) = async {
        let reuse = defaultArg reuseSocket true
        return! Async.FromBeginEnd(
            (fun (callback, state) -> socket.BeginDisconnect(reuse, callback, state)),
            socket.EndDisconnect
        )
    }

    member this.Send(payload:Message) = async {
        try
            let encoded = Encoding.encode payload
            let! _ = send socket encoded

            return Ok ()
        with
            | e -> return Error e.Message
    }

    member this.Receive(?expected:Commands.Command) = async {
        try
            let headerBuf = Array.zeroCreate Encoding.headerSize

            let! len = recv socket headerBuf 0 Encoding.headerSize 0
            if len < Encoding.headerSize then
                return Error "incomplete header received"
            else if len > Encoding.headerSize then
                return Error "received more than header size"
            else
                let header = Encoding.decodeHeader headerBuf

                logger.LogDebug(sprintf "Received header: %A" header)

                match expected with
                | Some expectedCommand when not <| areEqualSpans (expectedCommand.bytes.AsSpan()) (header.command.AsSpan()) ->
                    let unexpectedCmdText = Encoding.UTF8.GetString(header.command.AsSpan())
                    let msg = sprintf "Expected command %s, received %s" expectedCommand.text unexpectedCmdText
                    return Error msg
                | _ ->

                    // will throw exception if larger than int.MaxValue; we should never see this.
                    let expectedPayloadLen = Convert.ToInt32 header.payloadLength
                    let payload = Array.zeroCreate expectedPayloadLen

                    let! len = recv socket payload 0 expectedPayloadLen 0
                    if len <> expectedPayloadLen then
                        return sprintf "Expected %d bytes, received %d" header.payloadLength len |> Error
                    else
                        return Encoding.decode header payload
        with
        | e -> return Error e.Message
    }


    interface IAsyncDisposable with
        member this.DisposeAsync() =
            task {
                if this.IsConnected then
                    do! this.Disconnect()
                socket.Dispose()
            } |> ValueTask


