module Tests

open System
open System.Security.Cryptography
open Expecto

open BitThicket.Bitcoin.Cryptography
open System


let getCurve (prime : int) (a : int) (b : int) (generator : (int*int) option) (order : int) (cofactor : int) =
  let mutable curve = ECCurve(A = BitConverter.GetBytes(a),
                              B = BitConverter.GetBytes(b),
                              Cofactor = BitConverter.GetBytes(cofactor),
                              CurveType = ECCurve.ECCurveType.PrimeShortWeierstrass,
                              Order = BitConverter.GetBytes(order),
                              Prime = BitConverter.GetBytes(prime))

  match generator with
  | Some (gx, gy) -> curve.G <- ECPoint(X = BitConverter.GetBytes(gx), Y = BitConverter.GetBytes(gy))
  | None -> ()
  
  curve

let getPoint(x:int) (y:int) =  ECPoint(X = BitConverter.GetBytes(x), Y = BitConverter.GetBytes(y))

[<Tests>]
let tests =
  testList "EllipticCurve tests" [
    // y^2 = x^3 - 7x + 10 mod 127 == (127, -7, 10, G, 133, 1)
    // this is using an entire curve instead of a subgroup
    testCase "(1,2) ⊕ (3,4) ∈ (127, -7, 10, G, 133, 1)" <| fun _ ->
      let curve = getCurve 127 -7 10 None 133 1
      let p1 = getPoint 1 2
      let p2 = getPoint 3 4
      let rX = 32
      let rY = 114

      let (_, result) = ECDsa.add curve p1 p2
      Expect.isNotNull result.X "result.X was null, supposed to be byte[]"
      Expect.isNotNull result.Y "result.Y was null, supposed to be byte[]"
      
      let X = BitConverter.ToInt32(ReadOnlySpan(result.X))
      let Y = BitConverter.ToInt32(ReadOnlySpan(result.Y))
      Expect.equal -rX X "-R.X = result.X"

    testCase "should fail" <| fun _ ->
      let subject = false
      Expect.isTrue subject "I should fail because the subject is false."
  ]