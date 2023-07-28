﻿namespace BitThicket.Bitcoin.Client
open System
open System.Net
open System.Net.Sockets
open BitThicket.Bitcoin.Protocol

// disable warning about implicit conversions because we're going to have a lot of
// array/span/memory to ReadOnlySpan/Memory conversions
#nowarn "3391"

/// Represents a connection to a bitcoin node. uri should be a well-formed uri string.
/// example: "tcp://hostname:port"
/// Note: all operations are async unless otherwise indicated.
type BitcoinClient(uri:string) =
    let uri = Uri(uri)
    let socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)

    let resolve (uri:Uri) = async {
        let! hostAddresses = Async.FromBeginEnd(
                                    (fun (callback, state) -> Dns.BeginGetHostAddresses(uri.Host, callback, state)),
                                    Dns.EndGetHostAddresses)
        return IPEndPoint(hostAddresses[0], uri.Port)
    }

    let connect () = async {
        let! ipEndPoint = resolve uri
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



    let socketReceive (socket:Socket) (len:int) = async {
        try
            let buf = Array.zeroCreate len
            let! received = recv socket buf 0 len 0

            if received < len then
                return Error "Received less bytes than expected"
            elif received > len then
                return Error "Recevied more bytes than expected"
            else
                return Ok buf
        with
        | e -> return Error e.Message
    }

    member this.IsConnected = socket.Connected


    member this.Connect() = async {
        try
            do! connect()
            return Ok ()
        with
            | e -> return Error e.Message
    }

    member this.Disconnect(?reuseSocket) = async {
        let reuse = defaultArg reuseSocket true
        return! Async.FromBeginEnd(
            (fun (callback, state) -> socket.BeginDisconnect(reuse, callback, state)),
            socket.EndDisconnect
        )
    }

    member this.Send(data:byte[]) = send socket data

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

                let expectedCommandSatisifed =
                    expected |> Option.forall (fun expectedCmd -> areEqualSpans (header.command.AsSpan()) expectedCmd.bytes)

                if not expectedCommandSatisifed then
                    return sprintf "Expected command %A, received %A" expected header.command |> Error
                else
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

    member this.SendVersion (payload:VersionMessage) = async {
        let encoded = Encoding.encode (Version payload)
        let! _ = this.Send encoded

        match! this.Receive Commands.verack with
        | Error err -> return Error err
        | Ok _ -> return Ok ()
    }


    interface IDisposable with
        member this.Dispose() =
            socket.Dispose()

