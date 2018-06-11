module Tests

open Expecto
open Expecto.Flip
open BitThicket.Bitcoin.Cryptography

// [<Tests>]
// let tests =
//   testList "EllipticCurve tests" [
//     // y^2 = x^3 - 7x + 10 mod 127 == (127, -7, 10, G, 133, 1)
//     // this is using an entire curve instead of a subgroup
//     // testCase "validate CngKey" <| fun _ ->
//     //   let k = 
//   ]

[<Tests>]
let utilityTests =
  testList "Utility tests" [
    testCase "simple stringToBytes" <| fun _ ->
      let input = "F028"
      let result = Utility.stringToBytes input
      let expected = [|0xF0uy; 0x28uy|]

      Expect.isOk "failed to convert input" result 
      match result with | Ok b -> b | _ -> failwith "unexpected result"
      |> Expect.equal "Produced incorrect output" expected

    testCase "stringToBytes invalid input" <| fun _ ->
      let input = "F0287"
      Utility.stringToBytes input
      |> Expect.isError "stringToBytes should have failed"

    testCase "stringToBytes empty input" <| fun _ ->
      let input = ""
      let result = Utility.stringToBytes input
      let expected = Array.zeroCreate<byte> 0

      Expect.isOk "stringToBytes should have just returned a zero-length array" result
      match result with | Ok b -> b | _ -> failwith "unexpected result"
      |> Expect.equal "Produced incorrect output" expected

    testCase "stringToBytes not-hex input" <| fun _ ->
      let input = "hello"
      Utility.stringToBytes input
      |> Expect.isError "stringToBytes should have failed"

    testCase "stringToBytes lowercase input" <| fun _ ->
      let input = "f028"
      let result = Utility.stringToBytes input
      let expected = [|0xf0uy; 0x28uy|]

      Expect.isOk "stringtoBytes should have parsed lowercase without error" result 
      match result with | Ok b -> b | _ -> failwith "unexpected result"
      |> Expect.equal "produced incorrect output" expected
  ]