namespace BitThicket.Bitcoin.Protocol
open System
open System.Buffers.Binary
open System.Linq

[<AutoOpen>]
module MemoryUtils =

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

    let inline areEqualSpans (span1:ReadOnlySpan<byte>) span2 =
        span1.SequenceEqual(span2)