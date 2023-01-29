namespace BitThicket.Bitcoin

module Address =
    open System.ComponentModel
    open System.Linq
    open System.Security.Cryptography

    type AddressType =
    | [<Description("Pay-to-pubkey Hash")>]
      P2PKH
    | [<Description("Pay-to-script Hash")>]
      P2SH
    | [<Description("Private Key")>]
      WIF
    | [<Description("Private Key - Compressed")>]
      WIFCompressed
    | [<Description("Testnet Address")>]
      TestnetAddress
    | [<Description("Testnet Script Hash")>]
      TestnetScriptHash
    | [<Description("Testnet Private Key")>]
      TestnetWIF
    | [<Description("Testnet private Key - Compressed")>]
      TestnetWIFCompressed
    | [<Description("Testnet BIT32 Public Key")>]
      TestnetBIP32ExtPubKey
    | [<Description("Testnet BIP32 Private Key")>]
      TestnetBIP32ExtPrivKey
    | [<Description("BIP32 Public Key")>]
      BIP32ExtPubKey
    | [<Description("BIP32 Private Key")>]
      BIP32ExtPrivKey

    type AddressError =
    | UnsupportedAddressFormat of string
    | InvalidAddressFormat of AddressType * string
    | IncorrectChecksum of string
    | Base58Error of string

    let inline private versionBytes version =
        match version with                                                  // addr prefix
        | P2PKH -> [|0x00uy|]                                               // 1
        | P2SH -> [|0x05uy|]                                                // 3
        | WIF -> [|0x80uy|]                                                 // 5
        | WIFCompressed -> [|0x80uy|]                                       // K or L
        | TestnetAddress -> [|0x6fuy|]                                      // m or n
        | TestnetScriptHash -> [|0xc4uy|]                                   // 2
        | TestnetWIF -> [|0xefuy|]                                          // 9
        | TestnetWIFCompressed -> [|0xefuy|]                                // c
        | TestnetBIP32ExtPubKey -> [|0x04uy; 0x35uy; 0x87uy; 0xcfuy|]       // tpub
        | TestnetBIP32ExtPrivKey -> [|0x04uy; 0x35uy; 0x83uy; 0x94uy|]      // tprv
        | BIP32ExtPubKey -> [|0x04uy; 0x88uy; 0xb2uy; 0x1euy|]              // xpub
        | BIP32ExtPrivKey -> [|0x04uy; 0x88uy; 0xaduy; 0xe4uy|]             // xprv

    // TODO: should these all take inputs of Base58String?

    let (|PrefixP2PKH|_|) (addr:string) =
        if addr[0] = '1' then Some addr else None

    let (|PrefixWIF|_|) (addr:string) =
        if addr[0] = '5' then Some addr else None

    let (|PrefixWIFCompressed|_|) (addr:string) =
        let firstChar = addr[0]
        if firstChar = 'K' || firstChar = 'L' then Some addr else None

    let (|PrefixTesnetAddress|_|) (addr:string) =
        let firstChar = addr[0]
        if firstChar = 'm' || firstChar = 'n' then Some addr else None

    let (|PrefixBIP32ExtPubKey|_|) (addr:string) =
        if addr.StartsWith("xpub") then Some addr else None

    type ValidatedAddress = { addressType:AddressType; base58Check:Base58Check.Base58CheckString }

    let inline private doubleHash (prefixAndPayload:byte[]) =
        use sha256 = SHA256.Create()
        sha256.ComputeHash(prefixAndPayload) |> sha256.ComputeHash

    /// Expects data to be in big-endian byte order
    let encode (prefix:AddressType) (bytes:byte array) =
        let _formatIntermediate _prefix payload =
            Array.concat [
                _prefix |> versionBytes
                payload
            ]

        let intermediate = match prefix with
                           | WIF ->_formatIntermediate prefix bytes
                           | WIFCompressed -> Array.append bytes [|0x01uy|] |> _formatIntermediate prefix
                           | _ -> failwith "unsupported key format"
        let checksum = (doubleHash intermediate).[..3]
        let raw = Array.concat [intermediate; checksum]

        Base58.encode raw
        |> Ok


    let internal validateChecksum addr =
      match Base58.decode addr with
      | Error err -> sprintf "%A" err |> Base58Error |> Error
      | Ok bytes ->
        let check = bytes.[(bytes.Length-4)..]
        let data = bytes.[..(bytes.Length-5)]

        if Enumerable.SequenceEqual((doubleHash data).[..3], check) then Ok ()
        else IncorrectChecksum addr |> Error