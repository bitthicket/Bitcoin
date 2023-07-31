namespace BitThicket.Bitcoin

module internal BinaryUtil =
  open System

  [<AbstractClass; Sealed>]
  type Converter () =
    static member GetBytesBE(value : bool) =
      BitConverter.GetBytes(value)
      |> if BitConverter.IsLittleEndian then Array.rev else id
    static member GetBytesBE(value : char) =
      BitConverter.GetBytes(value)
      |> if BitConverter.IsLittleEndian then Array.rev else id
    static member GetBytesBE(value : float) =
      BitConverter.GetBytes(value)
      |> if BitConverter.IsLittleEndian then Array.rev else id
    static member GetBytesBE(value : int16) =
      BitConverter.GetBytes(value)
      |> if BitConverter.IsLittleEndian then Array.rev else id
    static member GetBytesBE(value : int32) =
      BitConverter.GetBytes(value)
      |> if BitConverter.IsLittleEndian then Array.rev else id
    static member GetBytesBE(value : int64) =
      BitConverter.GetBytes(value)
      |> if BitConverter.IsLittleEndian then Array.rev else id
    static member GetBytesBE(value : float32) =
      BitConverter.GetBytes(value)
      |> if BitConverter.IsLittleEndian then Array.rev else id
    static member GetBytesBE(value : uint16) =
      BitConverter.GetBytes(value)
      |> if BitConverter.IsLittleEndian then Array.rev else id
    static member GetBytesBE(value : uint32) =
      BitConverter.GetBytes(value)
      |> if BitConverter.IsLittleEndian then Array.rev else id
    static member GetBytesBE(value : uint64) =
      BitConverter.GetBytes(value)
      |> if BitConverter.IsLittleEndian then Array.rev else id
    static member GetBytesBE(value : bigint) =
      value.ToByteArray()
      |> if BitConverter.IsLittleEndian then Array.rev else id

  /// takes a byte array in big-endian order and ensures
  /// that the most significant bit is set to zero by prepending a 0-byte
  /// if the msb is 1
  let ensureZeroMsBit (bytes:byte array) =
    match bytes.[0] &&& 0x80uy with
    | 0x80uy -> Array.append [|0uy|] bytes
    | _ -> bytes

  /// takes a byte array in big-endian order and ensures that the leading
  /// byte is 0
  let ensureZeroMsByte (bytes:byte array) =
    if bytes.[0] = 0uy then bytes
    else Array.append[|0uy|] bytes

  /// takes a byte array in big-endian order and removes leading 0 bytes
  let removeZeroMsBytes (bytes:byte array) =
    let nonZeroIndex = Array.findIndex (fun b -> b > 0uy) bytes
    bytes.[nonZeroIndex..]

module internal StringUtil =
  open System
  open System.Globalization
  open System.Text

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

  let byteArrayFolder sb (b:byte) = Printf.kbprintf (fun _ -> sb) sb "%02x" b