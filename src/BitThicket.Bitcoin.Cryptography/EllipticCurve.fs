namespace BitThicket.Bitcoin.Cryptography

module EllipticCurve =
    open System.Numerics
    open System.Security.Cryptography

    let getKey (data:byte[]) = 
        BigInteger(data)

    let secp256k1 = ECCurve.CreateFromOid(Oid("1.3.132.0.10"))

    let multiply (k:BigInteger) (curve:ECCurve, p:ECPoint) =
        (curve, ECPoint())

    [<AutoOpen>]
    module Operators =
        let (*) k p = multiply k p
