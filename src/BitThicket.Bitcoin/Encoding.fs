namespace BitThicket.Bitcoin

module Encoding =

    type internal Version =
    | P2PKH
    | P2SH
    | WIF
    | WIFCompressed
    | Testnet
    | TestnetScriptHash
    | TestnetWIF
    | TestnetWIFCompressed

    type Encoding = 
        internal
        | BitcoinAddress of string * byte array
        | P2SHAddress of string * byte array
        | WIFKey of string * byte array
        | WIFCompressedKey of string * byte array
        | TestnetAddress of string * byte array
        | TestnetScriptHashAddress of string * byte array
        | TestnetWIFKey of string * byte array
        | TestnetWIFCompressedKey of string * byte array

    let inline internal toBytePrefix version = 
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

    let internal encodeUnchecked version payload = 
        Array.concat [ toBytePrefix version; payload ]

    let inline internal toEncoding (encodedString:string) (payload:byte array) =
        match encodedString with
        | enc when enc.[0] = '1' -> BitcoinAddress (enc, payload.[1..20]) |> Ok
        | enc when enc.[0] = '5' -> WIFKey (enc, payload.[1..32]) |> Ok
        | enc when enc.[0] = 'K' || enc.[0] = 'L' -> WIFCompressedKey (enc, payload.[1..33]) |> Ok
        | enc when enc.[0] = 'm' || enc.[0] = 'n' -> TestnetAddress (enc, payload.[1..20]) |> Ok
        | enc when enc.[0] = '2' -> TestnetScriptHashAddress (enc, payload) |> Ok // TODO: get data length for this
        | enc when enc.[0] = '9' -> TestnetWIFKey (enc, payload.[1..32]) |> Ok 
        | enc when enc.[0] = 'c' -> TestnetWIFCompressedKey (enc, payload.[1..33]) |> Ok
        | _ -> UnsupportedEncoding encodedString |> Error

    let (|BitcoinAddress|_|) =  Some ()
    let (|P2SHAddress|_|) = Some ()
    let (|WIFKey|_|) = Some ()
    let (|WIFCompressedKey|_|) = Some ()
    let (|TestnetAddress|_|) = Some ()
    let (|TestnetScriptHashAddress|_|) = Some ()
    let (|TestnetWIFKey|_|) = Some ()
    let (|TestnetWIFCompressedKey|_|) = Some ()

    let decode encodedString =
        Base58Check.decode encodedString |> Result.bind (toEncoding encodedString)

        // match Base58Check.decode encodedString with
        // | Error (Base58Check.Base58Error err) -> Base58Error err |> Error
        // | Error err -> Base58CheckError err |> Error
        // | Ok data -> 
        //     match encodedString with
        //         | (enc:string) when enc.[0] = '1' -> BitcoinAddress (enc,data.[1..20]) |> Ok
        //         | enc when enc.[0] = '5' -> WIFKey (enc,data.[1..32]) |> Ok
        //         | enc when enc.[0] = 'K' || enc.[0] = 'L' -> WIFCompressedKey (enc,data.[1..33]) |> Ok
        //         | enc when enc.[0] = 'm' || enc.[0] = 'n' -> TestnetAddress (enc,data) |> Ok
        //         | enc when enc.[0] = '2' -> TestnetScriptHashAddress (enc,data) |> Ok
        //         | enc when enc.[0] = '9' -> TestnetWIFKey (enc,data) |> Ok
        //         | enc when enc.[0] = 'c' -> TestnetWIFCompressedKey (enc,data) |> Ok
        //         | _ -> UnsupportedAddressFormat encodedString |> Error