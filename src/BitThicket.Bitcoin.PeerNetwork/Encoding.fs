namespace BitThicket.Bitcoin.PeerNetwork

module Encoding =

    open System
    open System.Buffers.Binary
    open System.IO
    open System.Net
    open System.Runtime.InteropServices
    open System.Text
    open FSharp.Compiler.Interactive.Shell
    open FSharp.NativeInterop
    open FSharp.Reflection

    open BitThicket.Bitcoin
    open BitThicket.Bitcoin.PeerNetwork.Protocol

    let private messageSizeMap =
        FSharpType.GetUnionCases(typeof<MessagePayload>)
        |> Array.map (fun payloadCase ->
                        payloadCase.GetFields()
                        |> Array.map (fun payloadFields ->
                                        FSharpType.GetRecordFields(payloadFields.PropertyType)
                                        |> Array.map (fun field ->
                                                        if field.PropertyType.IsValueType then
                                                            Marshal.SizeOf(field.PropertyType)
                                                        else
                                                            0)
                                        |> Array.sum)
                        |> Array.sum
                        |> (+) 24 // for the header, which is fixed in size
                        |> (fun size -> (payloadCase.Tag, size)))
        |> Map.ofArray

    let private recordReaders =
        FSharpType.GetUnionCases(typeof<MessagePayload>)
        |> Array.map (fun payloadCase ->
                        let propertyInfo = payloadCase.GetFields()[0]
                        (payloadCase.Tag, FSharpValue.PreComputeRecordReader propertyInfo.PropertyType))
        |> Map.ofArray

    let private getMessageTag = FSharpValue.PreComputeUnionTagReader typeof<MessagePayload>

    let encodeMessagePayload msg =
        let tag = getMessageTag msg
        let buf = Array.zeroCreate<byte> 1024
        use writer = new ProtocolWriter(new MemoryStream(buf), Encoding.UTF8)

        match msg with
        | Version(version) ->
            recordReaders.[tag] version
            |> Array.iter (fun obj -> writer.Write(obj))

        buf






