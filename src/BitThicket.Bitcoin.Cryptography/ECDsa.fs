namespace BitThicket.Bitcoin.Cryptography

module ECDsa =
    open System
    open System.Security.Cryptography
    open System.Text

    let BCRYPT_ECDSA_PRIVATE_P256_MAGIC =   0x32534345
    let BCRYPT_ECDSA_PUBLIC_P256_MAGIC =    0x31534345

    type PublicBlob256 =
        { magic : int;
          keysize : int;
          cngData : byte array }

        member this.Key =  this.cngData.[8..]

    type PrivateBlob256 =
        { magic : int;
          keysize : int;
          cngData : byte array }

        member this.PublicKey = this.cngData.[8..71]
        member this.PrivateKey = this.cngData.[72..]

    let secp256k1 = ECCurve.CreateFromOid(Oid("1.3.132.0.10"))

    let generateKeyPair (curve:ECCurve) =
        let cng = new ECDsaCng(curve)
        cng.Key

    let inline bytesToPublicEccBlob (bytes:byte[]) =
        { magic = BCRYPT_ECDSA_PUBLIC_P256_MAGIC;
          keysize = 256;
          cngData = bytes } |> Ok

    let inline bytesToPrivateEccBlob (bytes:byte[]) =
        { magic = BCRYPT_ECDSA_PRIVATE_P256_MAGIC;
          keysize = 256;
          cngData = bytes } |> Ok

    /// params k, x, and y correspond to CngKeyBlobFormat.EccPrivateBlob params d, x, and y respectively
    let cngKeyFromParams k x y =
        let blob = Array.concat [BitConverter.GetBytes(BCRYPT_ECDSA_PRIVATE_P256_MAGIC);
                                 BitConverter.GetBytes(256); x; y; k]
        CngKey.Import(blob, CngKeyBlobFormat.EccPrivateBlob)

    let exportPublicKey (cngKey:CngKey) =
        cngKey.Export(CngKeyBlobFormat.EccPublicBlob) |> bytesToPublicEccBlob

    let exportPrivateKey (cngKey:CngKey) =
        cngKey.Export(CngKeyBlobFormat.EccPrivateBlob) |> bytesToPrivateEccBlob

    let formatPublicKey (cngKey:CngKey) =
        cngKey.Export(CngKeyBlobFormat.EccPublicBlob).[8..]
        |> Array.fold Utility.hexFold (StringBuilder("04"))
        |> (fun buf -> buf.ToString())

    let formatPublicKeyCompressed (cngKey:CngKey) =
        let xbytes = (cngKey.Export(CngKeyBlobFormat.EccPublicBlob)).[8..39]
        let initialState = match (xbytes.[31] &&& 1uy) with
                            | 1uy -> StringBuilder("03")
                            | _ -> StringBuilder("02")

        Array.fold Utility.hexFold initialState xbytes
        |> (fun buf -> buf.ToString())