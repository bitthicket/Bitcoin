namespace BitThicket.Bitcoin.Cryptography

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