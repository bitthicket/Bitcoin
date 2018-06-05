#r "../../packages/Inferno/lib/net462/SecurityDriven.Inferno.dll"

open System
open System.Numerics
open System.Security.Cryptography
open SecurityDriven.Inferno

let oid = Oid("1.3.132.0.10")

printfn "%s" oid.FriendlyName

let curve = ECCurve.CreateFromOid(oid)
let generator = curve.G

let getRandomData length =
    use rng = new RNGCryptoServiceProvider()
    let data = Array.create length 0uy
    rng.GetBytes(data)
    BigInteger(data)

