namespace BitThicket.Bitcoin

module Address =
    open System
    open System.ComponentModel
    open System.Security.Cryptography
    open System.Text

    type VersionPrefix =
    | [<Description("Pay-to-pubkey Hash")>]
      P2PKH = 0x00
    | [<Description("Pay-to-script Hash")>]
      P2SH = 0x05
    | [<Description("Private key")>]
      WIF = 0x80
    | [<Description("Private key - compressed")>]
      WIFCompressed = 0x80
    | [<Description("Testnet address")>]
      TestnetAddress = 0x6f
    | [<Description("Testnet script hash")>]
      TestnetScriptHash = 0xc4
    | BIP38EncryptedPrivateKey = 0x0142         // TODO: need description
    | [<Description("Testnet private key")>] 
      TestnetWIF = 0xef
    | [<Description("Testnet private key")>]
      TestnetWIFCompressed = 0xef
    | [<Description("Testnet BIP32 pubkey")>]
      TestnetBIP32ExtendedPublicKey = 0x043587cf
    | [<Description("Testnet BIP32 private key")>]
      TestnetBIP32ExtendedPrivateKey = 0x04358394
    | [<Description("BIP32 pubkey")>]
      BIP32ExtendedPublicKey = 0x0488b21e 
    | [<Description("BIP32 private key")>]
      BIP32ExtendedPrivateKey = 0x0488ade4

    let format (prefix:VersionPrefix) (bytes:byte array) =
        // TODO: use Span when it becomes available
        let _doubleHash (data:byte array) = 
            use sha256 = SHA256.Create()
            sha256.ComputeHash(sha256.ComputeHash(data)).[0..3]

        let _formatIntermediate _prefix _bytes =
            Array.concat [
                BitConverter.GetBytes(int _prefix);
                _bytes
            ]

        let intermediate = _formatIntermediate prefix bytes
        let checksum = (_doubleHash intermediate)
        let raw = Array.concat [intermediate; checksum]

        // ???: might have to reverse the array for bigendian/littleendian conversion
        Base58.encode raw