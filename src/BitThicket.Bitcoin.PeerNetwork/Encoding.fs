module BitThicket.Bitcoin.PeerNetwork.Encoding

open System
open System.Buffers.Binary
open System.Security.Cryptography
open System.Text
open FSharp.Reflection
open Microsoft.FSharp.NativeInterop

open BitThicket.Bitcoin.PeerNetwork.Protocol

module private MemoryUtils =
    let inline getReadOnlyMemory (arr:'a array) =
        arr.AsMemory() |> Memory<'a>.op_Implicit

    let inline writeBytes dest (src:ReadOnlyMemory<byte>) =
        src.Span.CopyTo(dest)
        src.Length

    let inline writeUInt16 dest i =
        BinaryPrimitives.WriteUInt16LittleEndian(dest, i)
        2

    let inline writeUInt16BigEndian dest i =
        BinaryPrimitives.WriteUInt16BigEndian(dest, i)
        2

    let inline writeInt32 dest i =
        BinaryPrimitives.WriteInt32LittleEndian(dest, i)
        4

    let inline writeUInt32 dest i =
        BinaryPrimitives.WriteUInt32LittleEndian(dest, i)
        4

    let inline writeInt64 dest i =
        BinaryPrimitives.WriteInt64LittleEndian(dest, i)
        8

    let inline writeUInt64 dest i =
        BinaryPrimitives.WriteUInt64LittleEndian(dest, i)
        8

    let getVariableIntLength (i:int64) =
        if i < 0xfd then 1
        elif i <= 0xffff then 3
        elif i <= 0xffffffff then 5
        else 9

    let inline writeVariableInt (buf:Span<byte>) (i:int64) =
        if i < 0xfd then
            buf[0] <- byte i
            1
        elif i <= 0xffff then
            buf[0] <- 0xfduy
            buf[1] <- byte i
            buf[2] <- byte (i >>> 8)
            3
        elif i <= 0xffffffff then
            buf[0] <- 0xfeuy
            buf[1] <- byte i
            buf[2] <- byte (i >>> 8)
            buf[3] <- byte (i >>> 16)
            buf[4] <- byte (i >>> 24)
            5
        else
            buf[0] <- 0xffuy
            buf[1] <- byte i
            buf[2] <- byte (i >>> 8)
            buf[3] <- byte (i >>> 16)
            buf[4] <- byte (i >>> 24)
            buf[5] <- byte (i >>> 32)
            buf[6] <- byte (i >>> 40)
            buf[7] <- byte (i >>> 48)
            buf[8] <- byte (i >>> 56)
            9

// TODO: this needs to be configurable, injectable
let private currentNetworkMagic = NETWORK_MAGIC_TESTNET

// TODO: one day this file should be generated from the domain model; the compiler knows pretty much
// everything prior to encoding time other than length of variable-length strings.
// We should be able to pre-compute all the serialization offsets and lengths

let private headerSize = 24

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

open MemoryUtils

let private calculatePayloadSize = function
    | Version payload ->
        versionPayloadBaseSize
        + getVariableIntLength (int64 payload.serverAgent.Length)
        + Encoding.UTF8.GetByteCount(payload.serverAgent)
    | VerAck -> 0

let private encodeHeader (span:Span<byte>) header =
    let mutable pos = 0
    pos <- header.magic.AsReadOnlyMemory()
           |> writeBytes (span.Slice(pos, 4))
           |> (+) pos

    header.command.AsReadOnlyMemory()
    |> writeBytes (span.Slice(pos, 12))
    |> ignore

    pos <- pos + 12

    pos <- header.payloadLength
           |> writeUInt32 (span.Slice(pos, 4))
           |> (+) pos

    header.checksum.AsReadOnlyMemory()
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
let encodePeerMessage payload =
    let payloadSize = calculatePayloadSize payload
    match payload with
    | Version versionPayload ->
        let buf = headerSize + payloadSize
                  |> Array.zeroCreate<byte>

        let headerSpan = buf.AsSpan(0, headerSize)
        let payloadSpan = buf.AsSpan(headerSize)

        let actualPayloadSize = encodeVersionPayload payloadSpan versionPayload
        if actualPayloadSize <> payloadSize then
            failwithf "payload size mismatch; expected %d, actual %d" payloadSize actualPayloadSize

        // TODO: need to make this faster
        let payloadChecksum = payloadSpan.Slice(0,payloadSize).ToArray()
                              |> SHA256.HashData
                              |> SHA256.HashData
                              |> Array.take 4
                              |> getReadOnlyMemory

        let header = { magic = currentNetworkMagic |> ByteMemoryRef4.op_Implicit
                       command = Commands.version |> ByteMemoryRef12Max.op_Implicit
                       payloadLength = uint32 payloadSize
                       checksum = payloadChecksum |> ByteMemoryRef4.op_Implicit }

        encodeHeader headerSpan header |> ignore
        buf

    | VerAck ->
        let buf = headerSize |> Array.zeroCreate<byte>
        let headerSpan = buf.AsSpan()
        let header = { magic = currentNetworkMagic |> ByteMemoryRef4.op_Implicit
                       command = Commands.verack |> ByteMemoryRef12Max.op_Implicit
                       payloadLength = 0u
                       checksum = EMPTY_PAYLOAD_CHECKSUM |> ByteMemoryRef4.op_Implicit }

        encodeHeader headerSpan header |> ignore
        buf