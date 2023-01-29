namespace BitThicket.Bitcoin

#nowarn "3391"

type Base58Error =
    | InvalidBase58String of string

type Base58CheckError =
    | IncorrectChecksum of byte array

type EncodingError =
    | UnsupportedEncoding of string
    | InvalidEncoding of string
    | Base58Error of Base58Error
    | Base58CheckError of Base58CheckError

module Base58 =
    open System
    open System.IO
    open System.Text

    type Base58String = Base58String of string

    let private _58I = 58I

    let private _encTable = [| '1'; '2'; '3'; '4'; '5'; '6'; '7'; '8'; '9'; 'A'; // 0-9
                               'B'; 'C'; 'D'; 'E'; 'F'; 'G'; 'H'; 'J'; 'K'; 'L'; // 10-19
                               'M'; 'N'; 'P'; 'Q'; 'R'; 'S'; 'T'; 'U'; 'V'; 'W'; // 20-29
                               'X'; 'Y'; 'Z'; 'a'; 'b'; 'c'; 'd'; 'e'; 'f'; 'g'; // 30-39
                               'h'; 'i'; 'j'; 'k'; 'm'; 'n'; 'o'; 'p'; 'q'; 'r'; // 40-49
                               's'; 't'; 'u'; 'v'; 'w'; 'x'; 'y'; 'z' |] // 50-57

    let private _decTable = ['1',0I; '2',1I; '3',2I; '4',3I; '5',4I; '6',5I; '7',6I; '8',7I; '9',8I; 'A',9I;
                             'B',10I; 'C',11I; 'D',12I; 'E',13I; 'F',14I; 'G',15I; 'H',16I; 'J',17I; 'K',18I; 'L',19I;
                             'M',20I; 'N',21I; 'P',22I; 'Q',23I; 'R',24I; 'S',25I; 'T',26I; 'U',27I; 'V',28I; 'W',29I;
                             'X',30I; 'Y',31I; 'Z',32I; 'a',33I; 'b',34I; 'c',35I; 'd',36I; 'e',37I; 'f',38I; 'g',39I;
                             'h',40I; 'i',41I; 'j',42I; 'k',43I; 'm',44I; 'n',45I; 'o',46I; 'p',47I; 'q',48I; 'r',49I;
                             's',50I; 't',51I; 'u',52I; 'v',53I; 'w',54I; 'x',55I; 'y',56I; 'z',57I] |> Map.ofList

    let private _validate raw =
        if String.forall (fun c -> Map.containsKey c _decTable) raw
        then Base58String raw |> Ok
        else InvalidBase58String raw |> Error

    let validate raw = _validate raw |> Result.mapError Base58Error

    let private divrem n d =
        let rem : bigint ref = ref 0I
        let next = bigint.DivRem(n, d, rem)
        (next, rem.Value)

    let rec private _encode (acc:MemoryStream) n =
        if n = 0I then
            Array.foldBack (fun b (sb:StringBuilder) -> sb.Append(_encTable.[int b])) (acc.ToArray()) (StringBuilder())
            |> (fun sb -> sb.ToString())
        else
            let rem : bigint ref = ref 0I
            let next = bigint.DivRem(n, 58I, rem)
            acc.WriteByte(rem.Value |> byte)
            _encode acc next

    // this could definitely benefit from Span work
    /// expects input array to be in big-endian order (higher-order bytes precede lower-order ones
    /// - i.e., data[0] is the MSB)
    let encode (data:byte array) =
        use ms = new MemoryStream()
        data |> Array.append [|0uy|] |> Array.rev |> bigint |> _encode ms

    let rec private _decode (s:ReadOnlySpan<char>) (acc:bigint) =
        if s.Length = 0 then Bits.Converter.GetBytesBE(acc)
        else _decTable[s[0]]
             |> (fun idx -> _58I * acc + idx)
             |> _decode (s.Slice(1))

    let decode (data:string) =
        validate data
        // this is tricky because when BigInteger.ToByteArray() is called, it will insert a leading zero byte if necessary
        // to ensure a positive value when the array is round-tripped.
        |> Result.bind (fun (Base58String encoding) ->
                            _decode encoding 0I
                            |> if data.[0] = '1' then Bits.ensureZeroMsByte else Bits.removeZeroMsBytes
                            |> Ok)

module Base58Check =
    open System.Linq
    open System.Security.Cryptography

    type Base58CheckString = Base58CheckString of string

    let inline private doubleHash (prefixAndPayload:byte array) =
      use sha256 = SHA256.Create()
      sha256.ComputeHash(prefixAndPayload) |> sha256.ComputeHash

    let internal validateChecksum (data:byte array) =
        let payload = data.[..(data.Length-5)]
        let check = data.[(data.Length-4)..]

        if Enumerable.SequenceEqual((doubleHash payload).[..3], check) then Ok data
        else IncorrectChecksum data |> Error

    let private encodeChecked unchecked =
        doubleHash unchecked |> (fun cs -> cs.[..3]) |> Array.append unchecked

    /// Expects data to be in big-endian byte order
    let encode payload =
        encodeChecked payload |> Base58.encode |> Base58CheckString

    let decode encodedString =
        Base58.decode encodedString |> Result.bind (validateChecksum >> Result.mapError Base58CheckError)