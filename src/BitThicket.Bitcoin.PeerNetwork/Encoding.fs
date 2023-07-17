namespace BitThicket.Bitcoin.PeerNetwork

module Encoding =

    open System
    open System.IO
    open System.Net
    open System.Runtime.InteropServices
    open System.Text
    open FSharp.Compiler.Interactive.Shell
    open FSharp.NativeInterop
    open FSharp.Reflection
    open BitThicket.Bitcoin.PeerNetwork.Protocol

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

    let getFsiSession() =
        let sbOut = StringBuilder()
        let sbErr = StringBuilder()
        use inStream = new StringReader(String.Empty)
        use outStream = new StringWriter(sbOut)
        use errStream = new StringWriter(sbErr)
        let args = [| "fsi.exe"; " --noninteractive" |]

        let config = FsiEvaluationSession.GetDefaultConfiguration()
        FsiEvaluationSession.Create(config, args, inStream, outStream, errStream), sbOut, sbErr

    do
        use fsi = getFsiSession()
        let code = makeMsgSizeFunc()