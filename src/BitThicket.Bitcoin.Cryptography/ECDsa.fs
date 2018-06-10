namespace BitThicket.Bitcoin.Cryptography

module ECDsa =
    open System
    open System.Numerics
    open System.Security.Cryptography

    let getKey (data:byte[]) = 
        BigInteger(data)

    let secp256k1 = ECCurve.CreateFromOid(Oid("1.3.132.0.10"))

    let add (curve:ECCurve) (p1:ECPoint) (p2:ECPoint) = 
        (curve, ECPoint(X = [|0uy|], Y = [|0uy|]))

    /// implemented usingt the double-and-add algorithm
    let multiply (curve:ECCurve) (k:BigInteger) (p:ECPoint) =
        (ECPoint(), curve)

    [<AutoOpen>]
    module Operators =
        let (*) k p = multiply (snd p) k (fst p)