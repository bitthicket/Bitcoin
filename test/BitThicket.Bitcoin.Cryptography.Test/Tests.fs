module Tests

open System
open Expecto
open Expecto.Flip
open BitThicket.Bitcoin.Cryptography

let testK1 = [|0x1euy; 0x99uy; 0x42uy; 0x3auy; 0x4euy; 0xd2uy; 0x76uy; 0x08uy; 0xa1uy; 0x5auy; 0x26uy; 0x16uy; 0xa2uy; 0xb0uy; 
               0xe9uy; 0xe5uy; 0x2cuy; 0xeduy; 0x33uy; 0x0auy; 0xc5uy; 0x30uy; 0xeduy; 0xccuy; 0x32uy; 0xc8uy; 0xffuy; 0xc6uy;
               0xa5uy; 0x26uy; 0xaeuy; 0xdduy|]
let testX1 = [|0xF0uy; 0x28uy; 0x89uy; 0x2Buy; 0xADuy; 0x7Euy; 0xD5uy; 0x7Duy; 0x2Fuy; 0xB5uy; 0x7Buy; 0xF3uy; 0x30uy; 0x81uy;
               0xD5uy; 0xCFuy; 0xCFuy; 0x6Fuy; 0x9Euy; 0xD3uy; 0xD3uy; 0xD7uy; 0xF1uy; 0x59uy; 0xC2uy; 0xE2uy; 0xFFuy; 0xF5uy;
               0x79uy; 0xDCuy; 0x34uy; 0x1Auy|]
let testY1 = [|0x07uy; 0xCFuy; 0x33uy; 0xDAuy; 0x18uy; 0xBDuy; 0x73uy; 0x4Cuy; 0x60uy; 0x0Buy; 0x96uy; 0xA7uy; 0x2Buy; 0xBCuy;
               0x47uy; 0x49uy; 0xD5uy; 0x14uy; 0x1Cuy; 0x90uy; 0xECuy; 0x8Auy; 0xC3uy; 0x28uy; 0xAEuy; 0x52uy; 0xDDuy; 0xFEuy;
               0x2Euy; 0x50uy; 0x5Buy; 0xDBuy|]
let testBytes1 = Array.concat [BitConverter.GetBytes(ECDsa.BCRYPT_ECDSA_PRIVATE_P256_MAGIC);
                               BitConverter.GetBytes(256);
                               testX1;testY1;testK1]

[<Tests>]
let ecdsaTests =
  testList "ECDsa tests" [
    testCase "generate secp256k1 key" <| fun _ ->
      use key = ECDsa.generateKeyPair ECDsa.secp256k1

      Expect.isNotNull "key shouldn't be null" key
      Expect.equal "incorrect key size" 256 key.KeySize

    testCase "export public key" <| fun _ ->
      use key = ECDsa.generateKeyPair ECDsa.secp256k1
      let result = ECDsa.exportPublicKey key
      
      Expect.isOk "public key export failed" result

    testCase "export private key" <| fun _ ->
      use key = ECDsa.generateKeyPair ECDsa.secp256k1
      let result = ECDsa.exportPrivateKey key

      Expect.isOk "private key export failed" result

    testCase "import private key" <| fun _ ->
      use key = ECDsa.cngKeyFromParams testK1 testX1 testY1
      key |> ignore
  ]

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

    testCase "bytesToBlob with pp63 inputs" <| fun _ ->
      let bytes = testBytes1;

      let result = ECDsa.bytesToPrivateEccBlob bytes

      Expect.isOk "bytesToBlob failed" result
      let blob = match result with | Ok b -> b | _ -> failwith "unexpected result"
      let testPublicKey = Array.concat [testX1;testY1]
      Expect.sequenceEqual "X data didn't convert successfully" testPublicKey blob.PublicKey

      let testPrivateKey = testK1
      Expect.sequenceEqual "K data didn't convert successfully" testPrivateKey blob.PrivateKey
  ]