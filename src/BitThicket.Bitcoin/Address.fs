namespace BitThicket.Bitcoin

module Address =
    open System.ComponentModel
    open System.Linq
    open System.Security.Cryptography

    type Type =
    | [<Description("Pay-to-pubkey Hash")>]
      P2PKH
    | [<Description("Pay-to-script Hash")>]
      P2SH
    | [<Description("Private key")>]
      WIF
    | [<Description("Private key - compressed")>]
      WIFCompressed
    | [<Description("Testnet address")>]
      TestnetAddress
    | [<Description("Testnet script hash")>]
      TestnetScriptHash
    | BIP38EncryptedPrivateKey        // TODO: need description
    | [<Description("Testnet private key")>] 
      TestnetWIF
    | [<Description("Testnet private key")>]
      TestnetWIFCompressed
    | [<Description("Testnet BIP32 pubkey")>]
      TestnetBIP32ExtendedPublicKey
    | [<Description("Testnet BIP32 private key")>]
      TestnetBIP32ExtendedPrivateKey
    | [<Description("BIP32 pubkey")>]
      BIP32ExtendedPublicKey
    | [<Description("BIP32 private key")>]
      BIP32ExtendedPrivateKey

    type Error =
    | UnsupportedAddressFormat of string
    | InvalidAddressFormat of Type*string
    | IncorrectChecksum of string
    | Base58Error of string

    let inline private versionBytes version = 
      match version with
       | P2PKH -> [|0x00uy|]
       | P2SH -> [|0x05uy|]
       | WIF -> [|0x80uy|]
       | WIFCompressed -> [|0x80uy|]
       | TestnetAddress -> [|0x6Fuy|]
       | TestnetScriptHash -> [|0xC4uy|]
       | BIP38EncryptedPrivateKey -> [|0x01uy; 0x42uy|]
       | TestnetWIF -> [|0xEFuy|]
       | TestnetWIFCompressed -> [|0xEFuy|]
       | TestnetBIP32ExtendedPublicKey -> [|0x04uy; 0x35uy; 0x87uy; 0xCFuy|]
       | TestnetBIP32ExtendedPrivateKey -> [|0x04uy; 0x35uy; 0x83uy; 0x94uy|]
       | BIP32ExtendedPublicKey -> [|0x04uy; 0x88uy; 0xb2uy; 0x1Euy|]
       | BIP32ExtendedPrivateKey -> [|0x04uy; 0x88uy; 0xADuy; 0xE4uy|]

    let (|PrefixP2PKH|_|) (addr:string) =
      if addr.StartsWith("1") then Some addr else None

    let (|PrefixWIF|_|) (addr:string) =
      if addr.[0] = '5' then Some addr else None

    let (|PrefixWIFCompressed|_|) (addr:string) =
      if addr.[0] = 'K' || addr.[0] = 'L' then Some addr else None

    let (|PrefixTestnetAddress|_|) (addr:string) =
      if addr.[0] = 'm' || addr.[0] = 'n' then Some addr else None

    let (|PrefixBIP38EncryptedPrivateKey|_|) (addr:string) =
      if addr.StartsWith("6P") then Some addr else None
    
    let (|PrefixBIP32ExtendedPublicKey|_|) (addr:string) =
      if addr.StartsWith("xpub") then Some addr else None

    type ValidatedAddress = { addressType:Type; base58Check:string; }

    let inline private doubleHash (prefixAndPayload:byte array) = 
      use sha256 = SHA256.Create()
      sha256.ComputeHash(prefixAndPayload) |> sha256.ComputeHash

    /// Expects data to be in big-endian byte order
    let encode (prefix:Type) (bytes:byte array) =
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

    let private validateWif (addr:string) =
      if addr.Length <> 52 then InvalidAddressFormat (WIF, addr) |> Error
      else Ok ()

    let private validateAddress address =
      validateChecksum address
      // match address with
      // | WIF addr -> validateWif addr
      // | WIFCompressed addr ->  Ok { addressType = }
      // | addr -> UnsupportedAddressFormat addr |> Error

    let decode base58Check =
       

      (Type.WIF, Array.zeroCreate<byte> 0)
      |> Ok