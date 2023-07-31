module BitThicket.Bitcoin.Protocol.Encoding

open System
open System.Security.Cryptography
open System.Text

open BitThicket.Bitcoin.Protocol.Protocol

// disable warning about implicit conversions because we're going to have a lot of
// array/span/memory to ReadOnlySpan/Memory conversions
#nowarn "3391"

// TODO: this needs to be configurable, injectable
let private currentNetworkMagic = NETWORK_MAGIC_TESTNET

// TODO: one day this file should be generated from the domain model; the compiler knows pretty much
// everything prior to encoding time other than length of variable-length strings.
// We should be able to pre-compute all the serialization offsets and lengths

let headerSize = 24

let private versionPayloadBaseSize =
    [
        4 //version
        8 //services
        8 //timestamp
        8 //receiverServices
        16 //receiverAddress
        2 //receiverPort
        8 //senderServices
        16 //senderAddress
        2 //senderPort
        8 //nonce
        4 //blockHeight
    ] |> List.sum

let private calculatePayloadSize = function
    // Messages
    | Version payload ->
        versionPayloadBaseSize
        + getVariableIntLength (int64 payload.serverAgent.Length)
        + Encoding.UTF8.GetByteCount(payload.serverAgent)

    | VerAck -> 0

// TODO: is there a way to compute this faster?
let private computeChecksum (data:ReadOnlySpan<byte>) =
       SHA256.HashData(SHA256.HashData data).AsSpan().Slice(0,4).ToArray()

let private encodeHeader (span:Span<byte>) header =
    let mutable pos = 0
    pos <- header.magic.AsMemory()
           |> writeBytes (span.Slice(pos, 4))
           |> (+) pos

    header.command.AsMemory()
    |> writeBytes (span.Slice(pos, 12))
    |> ignore

    pos <- pos + 12

    pos <- header.payloadLength
           |> writeUInt32 (span.Slice(pos, 4))
           |> (+) pos

    header.checksum.AsMemory()
    |> writeBytes (span.Slice(pos, 4))
    |> (+) pos


let private encodeVersionPayload (span:Span<byte>) payload =
    let mutable pos = 0
    pos <- payload.version
           |> writeUInt32 (span.Slice(pos, 4))
           |> (+) pos

    let svc = LanguagePrimitives.EnumToValue payload.services
    pos <- svc
           |> writeUInt64 (span.Slice(pos, 8))
           |> (+) pos

    pos <- uint64 payload.timestamp
           |> writeUInt64 (span.Slice(pos, 8))
           |> (+) pos

    let rcvSvc = LanguagePrimitives.EnumToValue payload.receiverServices
    pos <- rcvSvc
           |> writeUInt64 (span.Slice(pos, 8))
           |> (+) pos

    pos <- payload.receiverAddress.MapToIPv6().GetAddressBytes()
           |> getReadOnlyMemory
           |> writeBytes (span.Slice(pos, 16))
           |> (+) pos

    pos <- payload.receiverPort
           |> writeUInt16BigEndian (span.Slice(pos, 2))
           |> (+) pos

    let sndSvc = LanguagePrimitives.EnumToValue payload.senderServices
    pos <- sndSvc
           |> writeUInt64 (span.Slice(pos, 8))
           |> (+) pos

    pos <- payload.senderAddress.MapToIPv6().GetAddressBytes()
           |> getReadOnlyMemory
           |> writeBytes (span.Slice(pos, 16))
           |> (+) pos

    pos <- payload.senderPort
           |> writeUInt16BigEndian (span.Slice(pos, 2))
           |> (+) pos

    pos <- payload.nonce
           |> writeUInt64 (span.Slice(pos, 8))
           |> (+) pos

    let agentBytes = Encoding.UTF8.GetBytes(payload.serverAgent)
    let varIntLen = getVariableIntLength (int64 agentBytes.Length)
    pos <- writeVariableInt (span.Slice(pos, varIntLen)) (int64 agentBytes.Length)
           |> (+) pos

    pos <- agentBytes
           |> getReadOnlyMemory
           |> writeBytes (span.Slice(pos, agentBytes.Length))
           |> (+) pos

    payload.blockHeight
    |> writeUInt32 (span.Slice(pos, 4))
    |> (+) pos


/// takes a domain model and writes a byte array ready to be sent over the wire
/// the result of this encoding *includes* a header, which is generated from the
/// provided domain model
let encode msg =
    let payloadSize = calculatePayloadSize msg
    let buf = headerSize + payloadSize
                  |> Array.zeroCreate<byte>
    let headerSpan = buf.AsSpan(0, headerSize)
    let payloadSpan = buf.AsSpan(headerSize)

    match msg with
    | Version versionPayload ->
        let actualPayloadSize = encodeVersionPayload payloadSpan versionPayload
        if actualPayloadSize <> payloadSize then
            failwithf "payload size mismatch; expected %d, actual %d" payloadSize actualPayloadSize

        let payloadChecksum =
              computeChecksum (Span.op_Implicit (payloadSpan.Slice(0,payloadSize)))

        // be aware of implicit conversions here
        let header = { magic = currentNetworkMagic //|> ByteMemoryRef4.op_Implicit
                       command = Commands.version.bytes //|> ByteMemoryRef12Max.op_Implicit
                       payloadLength = uint32 payloadSize
                       checksum = payloadChecksum }

        encodeHeader headerSpan header |> ignore
        buf


    | VerAck ->
        // be aware of implicit conversions here
        let header = { magic = currentNetworkMagic
                       command = Commands.verack.bytes
                       payloadLength = 0u
                       checksum = EMPTY_PAYLOAD_CHECKSUM }

        encodeHeader headerSpan header |> ignore
        buf

let decodeHeader (data:ReadOnlySpan<byte>) =
       if data.Length <> headerSize then
           failwithf "header size mismatch; expected %d, actual %d" headerSize data.Length

       // be aware of implicit conversions here
       { magic = data.Slice(0,4).ToArray()
         command = data.Slice(4,12).ToArray()
         payloadLength = BitConverter.ToUInt32 (data.Slice(16,4))
         checksum = data.Slice(20,4).ToArray() }

let decode (header:MessageHeader) (payload:ReadOnlySpan<byte>) =
       // check length
       let expectedPayloadLen = Convert.ToInt32 header.payloadLength
       if payload.Length <> expectedPayloadLen then
           failwithf "payload length mismatch; expected %d, actual %d" header.payloadLength payload.Length

       // check checksum
       let checksum = computeChecksum payload
       if not <| areEqualSpans checksum (header.checksum.AsSpan()) then
           failwithf "payload checksum mismatch; expected %A, actual %A" header.checksum checksum

       // decode
       match header.command with
       // | versionCmd when areEqualSpans (versionCmd.AsSpan()) Commands.version.bytes ->
       //        // do the things
       //        Ok ()
       | verackCmd when areEqualSpans (verackCmd.AsSpan()) (Commands.verack.bytes.AsSpan()) ->
              // do the things
              Ok VerAck
       | _ -> Error "unknown command"