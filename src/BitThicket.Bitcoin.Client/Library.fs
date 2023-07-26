namespace BitThicket.Bitcoin.Client
open System
open System.Net
open System.Net.Sockets
open BitThicket.Bitcoin.Protocol
open BitThicket.Bitcoin.Protocol.Protocol

/// Represents a connection to a bitcoin node. uri should be a well-formed uri string.
/// example: "tcp://hostname:port"
/// Note: all operations are async unless otherwise indicated.
type BitcoinClient(uri:string) =
    let uri = Uri(uri)
    let socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)

    member this.Connect() = async {
        let! hostAddresses = Async.FromBeginEnd(
                                    (fun (callback, state) -> Dns.BeginGetHostAddresses(uri.Host, callback, state)),
                                    Dns.EndGetHostAddresses)
        let ipEndPoint = IPEndPoint(hostAddresses.[0], uri.Port)

        return! Async.FromBeginEnd(
            (fun (callback, state) -> socket.BeginConnect(ipEndPoint, callback, state)),
            socket.EndConnect
        )
    }

    member this.Send(data:byte[]) = async {
        return! Async.FromBeginEnd(
            (fun (callback, state) -> socket.BeginSend(data, 0, data.Length, SocketFlags.None, callback, state)),
            socket.EndSend
        )
    }

    member this.Receive(data:byte[]) = async {
        try
            let! _  = Async.FromBeginEnd(
                (fun (callback, state) -> socket.BeginReceive(data, 0, data.Length, SocketFlags.None, callback, state)),
                socket.EndReceive
            )

            return
                match Encoding.decodePeerMessage (data.AsSpan()) with
                | VerAck -> Ok VerAck
                | _ -> Result.Error "Expected VerAck"
        with
        | e -> return Error e.Message
    }

    member this.SendVersion (payload:VersionMessage) = async {
        let encoded = Encoding.encodePeerMessage (Version payload)
        let! _ = this.Send encoded

        // TODO: need to get the response here

        return ()
    }


    interface IDisposable with
        member this.Dispose() =
            socket.Dispose()

