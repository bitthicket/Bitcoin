#r "nuget: FSharp.Compiler.Service"
#load "DomainModel.fsx"

module p2p =
    open System
    open System.Net
    open System.Runtime.InteropServices
    open System.Text
    open FSharp.NativeInterop
    open FSharp.Reflection
    open BitThicket.Bitcoin.PeerNetwork

    let makeMsgSizeFunc () =
        let code = StringBuilder()
        """
        open BitThicket.Bitcoin.PeerNetwork

        let msgSize (msg: Message) =
            match msg.payload with
        """
        |> code.Append
        |> ignore

        for msgType in FSharpType.GetUnionCases(typeof<MessagePayload>) do
            let payloadVar = msgType.Name.ToLower()
            $"""
                | {msgType.Name} {payloadVar} ->
                    [
            """
            |> code.Append
            |> ignore

            for field in msgType.GetFields().[0].PropertyType |> FSharpType.GetRecordFields do
                printfn "%A" field.Name
                printfn "%A" field.PropertyType

                let fieldType = field.PropertyType
                if fieldType.IsPrimitive then
                    let length = Marshal.SizeOf(fieldType)
                    $"""
                            {length}
                    """
                    |> code.Append
                    |> ignore
                elif fieldType = typeof<ReadOnlyMemory<byte>> then
                    $"""
                            {payloadVar}.{field.Name}.Length
                    """
                    |> code.Append
                    |> ignore
                elif fieldType = typeof<IPAddress> then
                    $"""
                            16
                    """
                    |> code.Append
                    |> ignore
                elif fieldType = typeof<string> then
                    $"""
                            Encoding.UTF8.GetByteCount({payloadVar}.{field.Name})
                    """
                    |> code.Append
                    |> ignore
                else
                    failwithf "Unhandled type: %A" fieldType

            code.Append("       ] | |> Array.sum")
            |> ignore

        code.ToString()


module cli =

    open System
    open System.IO
    open System.Net
    open System.Net.Sockets
    open System.Text
    open System.Threading

    open FSharp.Compiler.Interactive.Shell

    open BitThicket.Bitcoin.PeerNetwork


    // let socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
    // socket.ConnectAsync("localhost", 18333, CancellationToken.None).AsTask()
    // |> Async.AwaitTask

    // let versionPayload = {
    //     version = 70015u
    //     services = uint64 NodeServices.Unspecified
    //     timestamp = DateTimeOffset.Now.ToUnixTimeSeconds() |> uint64
    //     receiverServices = uint64 NodeServices.Network
    //     receiverAddress = IPAddress.Parse("127.0.0.1")
    //     receiverPort = 18333us
    //     senderServices = uint64 NodeServices.Unspecified
    //     senderAddress = IPAddress.Parse("127.0.0.1")
    //     senderPort = socket.LocalEndPoint :?> IPEndPoint |> fun ep -> uint16 ep.Port
    //     nonce = 0UL
    //     serverAgent = "BitThicket.Bitcoin"
    //     blockHeight = 0
    // }

    // socket.DisconnectAsync(false).AsTask()
    // |> Async.AwaitTask

    let getFsiSession () =
        let sbOut = StringBuilder()
        let sbErr = StringBuilder()
        use inStream = new StringReader(String.Empty)
        use outStream = new StringWriter(sbOut)
        use errStream = new StringWriter(sbErr)
        let args = [| "fsi.exe"; " --noninteractive" |]

        let config = FsiEvaluationSession.GetDefaultConfiguration()
        FsiEvaluationSession.Create(config, args, inStream, outStream, errStream), sbOut, sbErr