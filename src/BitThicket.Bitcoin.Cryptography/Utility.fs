namespace BitThicket.Bitcoin.Cryptography

open System.Runtime.InteropServices
module Utility = 
    open System
    open System.Globalization
    open System.Text

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

    let BCRYPT_ECDSA_PRIVATE_P256_MAGIC = 0x32534345
    [<Struct; CLIMutable; StructLayout(LayoutKind.Sequential)>]
    type EccPrivateKeyBlob = {
        magic : int;
        cbkey : int;
        [<MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)>]
        x : byte[];
        [<MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)>]
        y : byte[];
        [<MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)>]
        d : byte[];
    }

    let bytesToBlob (bytes:byte[]) =
        let handle = GCHandle.Alloc(bytes, GCHandleType.Pinned)
        try
            try
                Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof<EccPrivateKeyBlob>) :?> EccPrivateKeyBlob
                |> Ok
            with
            | exn -> Error exn.Message
        finally
            handle.Free()
        

