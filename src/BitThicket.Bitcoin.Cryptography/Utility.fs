namespace BitThicket.Bitcoin.Cryptography

#nowarn "9"

module Utility = 
    open System
    open System.Globalization
    open System.Runtime.InteropServices
    open System.Text
    open Microsoft.FSharp.NativeInterop

    let hexFold (builder:StringBuilder) (byte:byte) =
        builder.Append(sprintf "%02x" byte)

    let stringToBytes hexString =
        let (|Invalid|Empty|NextByte|) = function
                                         | (s:string) when s.Length % 2 = 1 -> Invalid s
                                         | (s:string) when s.Length = 0 -> Empty s
                                         | (s:string) -> NextByte s.[s.Length-2..s.Length-1]

        let rec s2b acc s =
            match s with
            | Empty _ -> Ok acc
            | Invalid _ -> Error "invalid string"
            | NextByte hex -> 
                try
                    s2b (Array.concat [[|Byte.Parse(hex, NumberStyles.HexNumber)|]; acc]) s.[0..s.Length-3]
                with
                | :? System.FormatException as fex -> Error fex.Message
                | ex -> Error ex.Message

        s2b (Array.zeroCreate<byte> 0) hexString

    //type EccPrivateBlob256 = Unsafe.EccPrivateBlob256
    //type EccPublicBlob256 = Unsafe.EccPublicBlob256

    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type EccPrivateBlob256 = {
        magic : int;
        keysize : int;
        // these are const sized for now because we only ever deal with secp256k1
        [<MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)>]
        x : byte[] ;
        [<MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)>]
        y : byte[];
        [<MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)>]
        d : byte[];
    }

    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type EccPublicBlob256 = {
        magic : int;
        keysize : int;
        [<MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)>]
        x : byte[];
        [<MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)>]
        y : byte[];
    }

    let inline private marshalTo<'dst> ptr = Marshal.PtrToStructure(ptr, typeof<'dst>) :?> 'dst

    let inline bytesToEccBlob<'a> (bytes:byte[]) =
        use ptr = fixed bytes
        try
            marshalTo<'a> (NativePtr.toNativeInt ptr) |> Ok
        with
        | exn -> Error exn.Message