
open System
open System.IO
open System.Security.Cryptography
open System.Text

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

let _encTable = [| '1'; '2'; '3'; '4'; '5'; '6'; '7'; '8'; '9'; 'A'; // 0-9
                   'B'; 'C'; 'D'; 'E'; 'F'; 'G'; 'H'; 'J'; 'K'; 'L'; // 10-19
                   'M'; 'N'; 'P'; 'Q'; 'R'; 'S'; 'T'; 'U'; 'V'; 'W'; // 20-29
                   'X'; 'Y'; 'Z'; 'a'; 'b'; 'c'; 'd'; 'e'; 'f'; 'g'; // 30-39
                   'h'; 'i'; 'j'; 'k'; 'm'; 'n'; 'o'; 'p'; 'q'; 'r'; // 40-49
                   's'; 't'; 'u'; 'v'; 'w'; 'x'; 'y'; 'z' |]

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