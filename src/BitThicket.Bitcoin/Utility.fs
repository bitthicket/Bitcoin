namespace BitThicket.Bitcoin

module internal Bits =
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

module internal Base58 = 
    open System
    open System.IO
    open System.Security.Cryptography
    open System.Text

    type Error =
    | InvalidBase58String of string

    let private _encTable = [| '1'; '2'; '3'; '4'; '5'; '6'; '7'; '8'; '9'; 'A'; // 0-9
                               'B'; 'C'; 'D'; 'E'; 'F'; 'G'; 'H'; 'J'; 'K'; 'L'; // 10-19
                               'M'; 'N'; 'P'; 'Q'; 'R'; 'S'; 'T'; 'U'; 'V'; 'W'; // 20-29
                               'X'; 'Y'; 'Z'; 'a'; 'b'; 'c'; 'd'; 'e'; 'f'; 'g'; // 30-39
                               'h'; 'i'; 'j'; 'k'; 'm'; 'n'; 'o'; 'p'; 'q'; 'r'; // 40-49
                               's'; 't'; 'u'; 'v'; 'w'; 'x'; 'y'; 'z' |] // 50-57

    type ValidString = { value:string }
    let validate raw =
      if String.forall (fun c -> Array.exists (fun e -> c = e) _encTable) raw
      then { value = raw } |> Ok
      else InvalidBase58String raw |> Error

    // this could definitely benefit from Span work
    /// expects input array to be in big-endian order (higher-order bytes precede lower-order ones 
    /// - i.e., data[0] is the MSB)
    let encode (data:byte array) =
      let rec _encode (acc:MemoryStream) (n:bigint) =
          if n = 0I then 
            Array.foldBack (fun b (sb:StringBuilder) -> sb.Append(_encTable.[int b])) (acc.ToArray()) (StringBuilder())
            |> (fun sb -> sb.ToString())
          else
              let rem : bigint ref = ref 0I
              let next = bigint.DivRem(n, 58I, rem)
              acc.WriteByte(!rem |> byte)
              _encode acc next
      
      use ms = new MemoryStream()
      let n = data |> Array.append [|0uy|] |> Array.rev |> bigint
      _encode ms n

    let encodeCheckBinary (data:byte array) =
      use sha = SHA256.Create()
      let hash = data |> sha.ComputeHash |> sha.ComputeHash
      let checksum = hash.[..3]

      Array.concat [data; checksum] |> encode

    let encodeCheck (data:string) = 
      Encoding.UTF8.GetBytes(data) |> encodeCheckBinary

    let decode (data:string) =
      let rec _decode (s:string) (acc:bigint) =
        if s.Length = 0 then Bits.Converter.GetBytesBE(acc)
        else
          Array.findIndex (fun c -> c = s.[0]) _encTable
          |> (fun index -> 58I * acc + bigint index)
          |> _decode (s.Substring(1))

      match validate data with
      | Error err -> Error err
      | Ok validString -> _decode validString.value 0I 
                          // this is tricky because when BigInteger.ToByteArray() is called, it will insert a leading zero byte if necessary
                          // to ensure a positive value when the array is round-tripped.
                          |> if data.[0] = '1' then Bits.ensureZeroMsByte else Bits.removeZeroMsBytes
                          |> Ok