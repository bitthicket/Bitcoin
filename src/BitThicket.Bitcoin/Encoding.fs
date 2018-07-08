namespace BitThicket.Bitcoin.Encoding

module internal Base58 = 
    open System
    open System.IO
    open System.Text
    open BitThicket.Bitcoin

    type Error =
    | InvalidBase58String of string

    type Base58String = Base58String of string

    let private _encTable = [| '1'; '2'; '3'; '4'; '5'; '6'; '7'; '8'; '9'; 'A'; // 0-9
                               'B'; 'C'; 'D'; 'E'; 'F'; 'G'; 'H'; 'J'; 'K'; 'L'; // 10-19
                               'M'; 'N'; 'P'; 'Q'; 'R'; 'S'; 'T'; 'U'; 'V'; 'W'; // 20-29
                               'X'; 'Y'; 'Z'; 'a'; 'b'; 'c'; 'd'; 'e'; 'f'; 'g'; // 30-39
                               'h'; 'i'; 'j'; 'k'; 'm'; 'n'; 'o'; 'p'; 'q'; 'r'; // 40-49
                               's'; 't'; 'u'; 'v'; 'w'; 'x'; 'y'; 'z' |] // 50-57

    let validate raw =
        if String.forall (fun c -> Array.exists (fun e -> c = e) _encTable) raw
        then Base58String raw |> Ok
        else InvalidBase58String raw |> Error

    let private divrem n d =
        let rem : bigint ref = ref 0I
        let next = bigint.DivRem(n, d, rem)
        (next, !rem)

    let rec private _encode (acc:MemoryStream) n =
        if n = 0I then
            Array.foldBack (fun b (sb:StringBuilder) -> sb.Append(_encTable.[int b])) (acc.ToArray()) (StringBuilder())
            |> (fun sb -> sb.ToString())
        else
            let rem : bigint ref = ref 0I
            let next = bigint.DivRem(n, 58I, rem)
            acc.WriteByte(!rem |> byte)
            _encode acc next

    // this could definitely benefit from Span work
    /// expects input array to be in big-endian order (higher-order bytes precede lower-order ones 
    /// - i.e., data[0] is the MSB)
    let encode (data:byte array) =
        use ms = new MemoryStream()
        data |> Array.append [|0uy|] |> Array.rev |> bigint |> _encode ms

    let rec private _decode (s:string) (acc:bigint) =
        if s.Length = 0 then Bits.Converter.GetBytesBE(acc)
        else Array.findIndex (fun c -> c = s.[0]) _encTable 
             |> (fun idx -> 58I * acc + bigint idx) |> _decode (s.Substring(1))

    let decode (data:string) =
        match validate data with
        | Error err -> Error err
        | Ok (Base58String encoding) -> 
            // this is tricky because when BigInteger.ToByteArray() is called, it will insert a leading zero byte if necessary
            // to ensure a positive value when the array is round-tripped.
            _decode encoding 0I 
            |> if data.[0] = '1' then Bits.ensureZeroMsByte else Bits.removeZeroMsBytes
            |> Ok

module internal Base58Check =
    open System.Linq
    open System.Security.Cryptography

    type Version =
    | P2PKH
    | P2SH
    | WIF
    | WIFCompressed
    | Testnet
    | TestnetScriptHash
    | TestnetWIF
    | TestnetWIFCompressed

    // Base58Check encodings
    type Encoding = 
    | BitcoinAddress of string * byte array
    | P2SHAddress of string * byte array
    | WIFKey of string * byte array
    | WIFCompressedKey of string * byte array
    | TestnetAddress of string * byte array
    | TestnetScriptHashAddress of string * byte array
    | TestnetWIFKey of string * byte array
    | TestnetWIFCompressedKey of string * byte array

    type Error =
    | UnsupportedAddressFormat of string
    | InvalidAddressFormat of Version*string
    | IncorrectChecksum of byte array
    | Base58Error of Base58.Error

    let inline private doubleHash (prefixAndPayload:byte array) = 
      use sha256 = SHA256.Create()
      sha256.ComputeHash(prefixAndPayload) |> sha256.ComputeHash

    let inline private toBytePrefix version = 
      match version with
       | P2PKH -> [|0x00uy|]  // 1
       | P2SH -> [|0x05uy|]   // 3
       | WIF -> [|0x80uy|]    // 5
       | WIFCompressed -> [|0x80uy|]  // K or L
       | Testnet -> [|0x6Fuy|] // m or on
       | TestnetScriptHash -> [|0xC4uy|]  // 2
       | TestnetWIF -> [|0xEFuy|] // 9
       | TestnetWIFCompressed -> [|0xEFuy|] // c
    //    | TestnetBIP32ExtendedPublicKey -> [|0x04uy; 0x35uy; 0x87uy; 0xCFuy|]  // tpub
    //    | TestnetBIP32ExtendedPrivateKey -> [|0x04uy; 0x35uy; 0x83uy; 0x94uy|] // tprv
    //    | BIP32ExtendedPublicKey -> [|0x04uy; 0x88uy; 0xb2uy; 0x1Euy|] // xpub
    //    | BIP32ExtendedPrivateKey -> [|0x04uy; 0x88uy; 0xADuy; 0xE4uy|]  // xprv

    let private _validateChecksum (data:byte array) =
        let check = data.[(data.Length-4)..]
        let data = data.[..(data.Length-5)]

        if Enumerable.SequenceEqual((doubleHash data).[..3], check) then Ok ()
        else IncorrectChecksum data |> Error

    let private encodeUnchecked version payload = 
        match version with
        | WIF -> Array.concat [ toBytePrefix version; payload ]
        | _ -> failwith "unsupported encoding version"

    let private encodeChecked unchecked =
        doubleHash unchecked |> (fun cs -> cs.[..3]) |> Array.append unchecked

    /// Expects data to be in big-endian byte order
    let encode version payload = 
        encodeUnchecked version payload |> encodeChecked |> Base58.encode

    let toEncoding encodedString =
        match Base58.decode encodedString with
        | Error err -> Base58Error err |> Error
        | Ok data -> 
            match _validateChecksum data with
            | Error err -> Error err
            | Ok _ -> 
                match encodedString with
                | (enc:string) when enc.[0] = '1' -> BitcoinAddress (enc,data.[1..20]) |> Ok
                | enc when enc.[0] = '5' -> WIFKey (enc,data.[1..32]) |> Ok
                | enc when enc.[0] = 'K' || enc.[0] = 'L' -> WIFCompressedKey (enc,data.[1..33]) |> Ok
                | enc when enc.[0] = 'm' || enc.[0] = 'n' -> TestnetAddress (enc,data) |> Ok
                | enc when enc.[0] = '2' -> TestnetScriptHashAddress (enc,data) |> Ok
                | enc when enc.[0] = '9' -> TestnetWIFKey (enc,data) |> Ok
                | enc when enc.[0] = 'c' -> TestnetWIFCompressedKey (enc,data) |> Ok
                | _ -> UnsupportedAddressFormat encodedString |> Error

    let decode encodedString =
        match toEncoding encodedString with
        | Error err -> Error err
        | Ok encoding -> toPayload encoding |> Ok
