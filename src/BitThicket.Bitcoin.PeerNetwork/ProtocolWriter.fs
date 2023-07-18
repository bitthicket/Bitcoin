namespace BitThicket.Bitcoin
#nowarn "9"

open System
open System.Buffers.Binary
open System.IO
open System.Net
open System.Text
open FSharp.NativeInterop


type internal ProtocolWriter(stream:Stream, encoding:Encoding) =
    inherit BinaryWriter(stream, encoding)

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
        | :? IPAddress as ip -> self.Write(ip)
        | _ -> failwithf "unsupported type %A" (o.GetType())


    override self.Write(i:uint16) =
        let ptr = NativePtr.stackalloc<byte> sizeof<uint16>
                  |> NativePtr.toVoidPtr
        let buf = Span<byte>(ptr, sizeof<uint16>)

        BinaryPrimitives.WriteUInt16LittleEndian(buf, i)

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

    // override self.Write(i:uint32) =
    //     let ptr = NativePtr.stackalloc<byte> sizeof<uint32>
    //               |> NativePtr.toVoidPtr
    //     let buf = Span<byte>(ptr, sizeof<uint32>)

    //     if bigEndian then
    //         BinaryPrimitives.WriteUInt32BigEndian(buf, i)
    //     else
    //         BinaryPrimitives.WriteUInt32LittleEndian(buf, i)

    //     self.OutStream.Write(buf)

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

    member self.Write(ip:IPAddress) =
        let ptr = NativePtr.stackalloc<byte> sizeof<IPAddress>
                  |> NativePtr.toVoidPtr
        let buf = Span<byte>(ptr, sizeof<IPAddress>)

        let addressData = ip.MapToIPv6().GetAddressBytes()
        self.Write(addressData)