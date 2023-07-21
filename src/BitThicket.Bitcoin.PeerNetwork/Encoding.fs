module BitThicket.Bitcoin.PeerNetwork.Encoding

open System
open System.Buffers.Binary
open System.Security.Cryptography
open System.Text
open FSharp.Reflection
open Microsoft.FSharp.NativeInterop

open BitThicket.Bitcoin.PeerNetwork.Protocol

module private MemoryUtils =
    open System.Runtime.InteropServices
    let inline getReadOnlyMemory (arr:'a array) =
        arr.AsMemory() |> Memory<'a>.op_Implicit

module private ProtocolUtils =
    let getVariableIntLength (i:int64) =
        if i < 0xfd then 1
        elif i <= 0xffff then 3
        elif i <= 0xffffffff then 5
        else 9

    let writeVariableInt (buf:Span<byte>) (i:int64) =
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

let private calculatePayloadSize = function
    | Version payload ->
        versionPayloadBaseSize
        + ProtocolUtils.getVariableIntLength (int64 payload.serverAgent.Length)
        + Encoding.UTF8.GetByteCount(payload.serverAgent)

let private encodeHeader span header =
    if header.command.Length > 12 then
        failwith "command must be 12 bytes or less"

    let mutable pos = 0
    BinaryPrimitives.WriteUInt32LittleEndian(span, header.magic)
    pos <- pos + 4
    header.command.Span.CopyTo(span.Slice(pos))
    pos <- pos + 12
    BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(pos), header.payloadLength)
    pos <- pos + 4
    BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(pos), header.checksum)
    pos + 4

let private encodeVersionPayload span payload =
    let mutable pos = 0
    BinaryPrimitives.WriteUInt32LittleEndian(span, payload.version)
    pos <- pos + 4
    BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(pos), payload.services)
    pos <- pos + 8
    BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(pos), payload.timestamp)
    pos <- pos + 8
    BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(pos), payload.receiverServices)
    pos <- pos + 8
    payload.receiverAddress.MapToIPv6().GetAddressBytes().CopyTo(span.Slice(pos))
    pos <- pos + 16
    BinaryPrimitives.WriteUInt16BigEndian(span.Slice(pos), payload.receiverPort)
    pos <- pos + 2
    BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(pos), payload.senderServices)
    pos <- pos + 8
    payload.senderAddress.MapToIPv6().GetAddressBytes().CopyTo(span.Slice(pos))
    pos <- pos + 16
    BinaryPrimitives.WriteUInt16BigEndian(span.Slice(pos), payload.senderPort)
    pos <- pos + 2
    BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(pos), payload.nonce)
    pos <- pos + 8

    let agentBytes = Encoding.UTF8.GetBytes(payload.serverAgent)
    pos <- pos + ProtocolUtils.writeVariableInt (span.Slice(pos)) (int64 agentBytes.Length)
    agentBytes.CopyTo(span.Slice(pos))
    pos <- pos + agentBytes.Length

    BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(pos), payload.blockHeight)
    pos + 4


/// takes a domain model and writes a byte array ready to be sent over the wire
let encodePeerMessage payload =
    let payloadSize = calculatePayloadSize payload
    match payload with
    | Version versionPayload ->
        let buf = headerSize + payloadSize
                  |> Array.zeroCreate<byte>

        let headerSpan = buf.AsSpan()
        let payloadSpan = headerSpan.Slice(headerSize)

        let actualPayloadSize = encodeVersionPayload payloadSpan versionPayload
        if actualPayloadSize <> payloadSize then
            failwithf "payload size mismatch; expected %d, actual %d" payloadSize actualPayloadSize

        // TODO: need to make this faster
        let payloadChecksum = payloadSpan.Slice(0,payloadSize).ToArray()
                              |> SHA256.HashData
                              |> SHA256.HashData
                              |> fun arr -> BitConverter.ToUInt32(arr, 0)

        let header = { magic = currentNetworkMagic
                       command = Commands.version |> MemoryUtils.getReadOnlyMemory
                       payloadLength = uint32 payloadSize
                       checksum = payloadChecksum }

        encodeHeader headerSpan header |> ignore

        buf