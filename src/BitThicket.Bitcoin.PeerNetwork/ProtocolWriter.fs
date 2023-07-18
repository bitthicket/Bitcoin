namespace BitThicket.Bitcoin
#nowarn "9"

open System
open System.Buffers.Binary
open System.IO
open System.Text
open FSharp.NativeInterop


type internal ProtocolWriter(stream:Stream, encoding:Encoding, ?bigEndian:bool) =
    inherit BinaryWriter(stream, encoding)
    let bigEndian = defaultArg bigEndian true

    member self.Write(o:obj) =
        match o with
        | :? byte as b -> self.Write(b)
        | :? sbyte as b -> self.Write(b)
        | :? char as c -> self.Write(c)
        | :? string as s -> self.Write(s)
        | :? bool as b -> self.Write(b)
        | :? int16 as i -> self.Write(i)
        | :? uint16 as i -> self.Write(i)
        | :? int32 as i -> self.Write(i)
        | :? uint32 as i -> self.Write(i)
        | :? int64 as i -> self.Write(i)
        | :? uint64 as i -> self.Write(i)
        | :? float32 as f -> self.Write(f)
        | :? float as f -> self.Write(f)
        | :? (byte[]) as b -> self.Write(b)
        | :? (sbyte[]) as b -> self.Write(b)
        | :? (char[]) as c -> self.Write(c)
        | _ -> failwith "unsupported type"

    override self.Write(s:float32) =
        let ptr = NativePtr.stackalloc<byte> sizeof<float32>
                  |> NativePtr.toVoidPtr
        let buf = Span<byte>(ptr, sizeof<float32>)

        if bigEndian then
            BinaryPrimitives.WriteSingleBigEndian(buf, s)
        else
            BinaryPrimitives.WriteSingleLittleEndian(buf, s)

        self.OutStream.Write(buf)

    override self.Write(f:float) =
        let ptr = NativePtr.stackalloc<byte> sizeof<float>
                  |> NativePtr.toVoidPtr
        let buf = Span<byte>(ptr, sizeof<float>)

        if bigEndian then
            BinaryPrimitives.WriteDoubleBigEndian(buf, f)
        else
            BinaryPrimitives.WriteDoubleLittleEndian(buf, f)

        self.OutStream.Write(buf)

    override self.Write(i:int16) =
        let ptr = NativePtr.stackalloc<byte> sizeof<int16>
                  |> NativePtr.toVoidPtr
        let buf = Span<byte>(ptr, sizeof<int16>)

        if bigEndian then
            BinaryPrimitives.WriteInt16BigEndian(buf, i)
        else
            BinaryPrimitives.WriteInt16LittleEndian(buf, i)

        self.OutStream.Write(buf)

    override self.Write(i:uint16) =
        let ptr = NativePtr.stackalloc<byte> sizeof<uint16>
                  |> NativePtr.toVoidPtr
        let buf = Span<byte>(ptr, sizeof<uint16>)

        if bigEndian then
            BinaryPrimitives.WriteUInt16BigEndian(buf, i)
        else
            BinaryPrimitives.WriteUInt16LittleEndian(buf, i)

        self.OutStream.Write(buf)

    override self.Write(i:int32) =
        let ptr = NativePtr.stackalloc<byte> sizeof<int32>
                  |> NativePtr.toVoidPtr
        let buf = Span<byte>(ptr, sizeof<int32>)

        if bigEndian then
            BinaryPrimitives.WriteInt32BigEndian(buf, i)
        else
            BinaryPrimitives.WriteInt32LittleEndian(buf, i)

        self.OutStream.Write(buf)

    override self.Write(i:uint32) =
        let ptr = NativePtr.stackalloc<byte> sizeof<uint32>
                  |> NativePtr.toVoidPtr
        let buf = Span<byte>(ptr, sizeof<uint32>)

        if bigEndian then
            BinaryPrimitives.WriteUInt32BigEndian(buf, i)
        else
            BinaryPrimitives.WriteUInt32LittleEndian(buf, i)

        self.OutStream.Write(buf)

    override self.Write(i:int64) =
        let ptr = NativePtr.stackalloc<byte> sizeof<int64>
                  |> NativePtr.toVoidPtr
        let buf = Span<byte>(ptr, sizeof<int64>)

        if bigEndian then
            BinaryPrimitives.WriteInt64BigEndian(buf, i)
        else
            BinaryPrimitives.WriteInt64LittleEndian(buf, i)

        self.OutStream.Write(buf)

    override self.Write(i:uint64) =
        let ptr = NativePtr.stackalloc<byte> sizeof<uint64>
                  |> NativePtr.toVoidPtr
        let buf = Span<byte>(ptr, sizeof<uint64>)

        if bigEndian then
            BinaryPrimitives.WriteUInt64BigEndian(buf, i)
        else
            BinaryPrimitives.WriteUInt64LittleEndian(buf, i)

        self.OutStream.Write(buf)