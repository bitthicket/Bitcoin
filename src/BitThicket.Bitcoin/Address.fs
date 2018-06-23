namespace BitThicket.Bitcoin

module Address =
    open System
    open System.ComponentModel
    open System.Security.Cryptography
    open System.Text

    type VersionPrefix =
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

    /// Expects data to be in big-endian byte order
    let format (prefix:VersionPrefix) (bytes:byte array) =
        // TODO: use Span when it becomes available
        let _doubleHash (prefixAndPayload:byte array) = 
            use sha256 = SHA256.Create()
            sha256.ComputeHash(sha256.ComputeHash(prefixAndPayload))

        let _formatIntermediate _prefix payload =
            Array.concat [
                versionBytes prefix
                payload
            ]

        let intermediate = _formatIntermediate prefix bytes
        let checksum = (_doubleHash intermediate).[0..3]
        let raw = Array.concat [intermediate; checksum]

        Base58.encode raw