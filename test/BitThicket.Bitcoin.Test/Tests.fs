module Tests

#nowarn "0025"

open System.Text
open Expecto
open Expecto.Flip
open BitThicket.Bitcoin

//#region test1 data
let test1_k = [|0x1euy; 0x99uy; 0x42uy; 0x3auy; 0x4euy; 0xd2uy; 0x76uy; 0x08uy; 0xa1uy; 0x5auy; 0x26uy; 0x16uy; 0xa2uy; 0xb0uy; 
               0xe9uy; 0xe5uy; 0x2cuy; 0xeduy; 0x33uy; 0x0auy; 0xc5uy; 0x30uy; 0xeduy; 0xccuy; 0x32uy; 0xc8uy; 0xffuy; 0xc6uy;
               0xa5uy; 0x26uy; 0xaeuy; 0xdduy|]
let test1_x = [|0xF0uy; 0x28uy; 0x89uy; 0x2Buy; 0xADuy; 0x7Euy; 0xD5uy; 0x7Duy; 0x2Fuy; 0xB5uy; 0x7Buy; 0xF3uy; 0x30uy; 0x81uy;
               0xD5uy; 0xCFuy; 0xCFuy; 0x6Fuy; 0x9Euy; 0xD3uy; 0xD3uy; 0xD7uy; 0xF1uy; 0x59uy; 0xC2uy; 0xE2uy; 0xFFuy; 0xF5uy;
               0x79uy; 0xDCuy; 0x34uy; 0x1Auy|]
let test1_y = [|0x07uy; 0xCFuy; 0x33uy; 0xDAuy; 0x18uy; 0xBDuy; 0x73uy; 0x4Cuy; 0x60uy; 0x0Buy; 0x96uy; 0xA7uy; 0x2Buy; 0xBCuy;
               0x47uy; 0x49uy; 0xD5uy; 0x14uy; 0x1Cuy; 0x90uy; 0xECuy; 0x8Auy; 0xC3uy; 0x28uy; 0xAEuy; 0x52uy; 0xDDuy; 0xFEuy;
               0x2Euy; 0x50uy; 0x5Buy; 0xDBuy|]
let test1_wif = "5J3mBbAH58CpQ3Y5RNJpUKPE62SQ5tfcvU2JpbnkeyhfsYB1Jcn"
let test1_wifCompressed = "KxFC1jmwwCoACiCAWZ3eXa96mBM6tb3TYzGmf6YwgdGWZgawvrtJ"
//#endregion

//#region test2 data
let test2_k = [|0x3Auy; 0xBAuy; 0x41uy; 0x62uy; 0xC7uy; 0x25uy; 0x1Cuy; 0x89uy; 0x12uy; 0x07uy; 0xB7uy; 0x47uy; 0x84uy; 0x05uy;
                0x51uy; 0xA7uy; 0x19uy; 0x39uy; 0xB0uy; 0xDEuy; 0x08uy; 0x1Fuy; 0x85uy; 0xC4uy; 0xE4uy; 0x4Cuy; 0xF7uy; 0xC1uy;
                0x3Euy; 0x41uy; 0xDAuy; 0xA6uy|]

let test2_x = "41637322786646325214887832269588396900663353932545912953362782457239403430124"
              |> bigint.Parse
              |> (fun b -> b.ToByteArray())
              |> Array.rev

let test2_y = "16388935128781238405526710466724741593761085120864331449066658622400339362166"
              |> bigint.Parse
              |> (fun b -> b.ToByteArray())
              |> Array.rev

let test2_wif = "5JG9hT3beGTJuUAmCQEmNaxAuMacCTfXuw1R3FCXig23RQHMr4K"
let test2_wifCompressed = "KyBsPXxTuVD82av65KZkrGrWi5qLMah5SdNq6uftawDbgKa2wv6S"
//#endregion

[<Tests>]
let tests =
  testList "base58 tests" [
    testCase "encode \"hello\"" <| fun _ ->
      let input = Encoding.UTF8.GetBytes("hello")
      let expected = "Cn8eVZg"
      
      Base58.encode input
      |> Expect.sequenceEqual "encoder produced incorrect output" expected

    testCase "decode \"Cn8veVZg\"" <| fun _ ->
      let input = "Cn8eVZg"
      let expected = Encoding.UTF8.GetBytes("hello")

      let result = Base58.decode input
      Expect.isOk "Base58 decoder failed" result

      match result with
      | Ok actual -> Expect.sequenceEqual "incorrect decoded bytes" expected actual
      | _ -> failwith "unexpected error"

    testCase "decode 5J3mBbAH58CpQ3Y5RNJpUKPE62SQ5tfcvU2JpbnkeyhfsYB1Jcn" <| fun _ ->
      let input = "5J3mBbAH58CpQ3Y5RNJpUKPE62SQ5tfcvU2JpbnkeyhfsYB1Jcn"
      let expected = [|// version byte
                       0x80uy;
                       // payload
                       0x1euy; 0x99uy; 0x42uy; 0x3auy; 0x4euy; 0xd2uy; 0x76uy; 0x08uy;
                       0xa1uy; 0x5auy; 0x26uy; 0x16uy; 0xa2uy; 0xb0uy; 0xe9uy; 0xe5uy; 
                       0x2cuy; 0xeduy; 0x33uy; 0x0auy; 0xc5uy; 0x30uy; 0xeduy; 0xccuy; 
                       0x32uy; 0xc8uy; 0xffuy; 0xc6uy; 0xa5uy; 0x26uy; 0xaeuy; 0xdduy;
                       // checksum
                       0xc4uy; 0x7euy; 0x83uy; 0xffuy|]

      let result = Base58.decode input
      Expect.isOk "Base58 decoder failed" result

      let (Ok actual) = result
      Expect.sequenceEqual "decode result incorrect" expected actual

    testCase "decode base58check-encoded 'hello'" <| fun _ ->
      let input = "12L5B5yqsf7vwb"
      let expected = [|0x00uy; 0x68uy; 0x65uy; 0x6cuy; 0x6cuy; 0x6fuy; 0x9cuy; 0x3cuy; 0x23uy; 0x62uy|]

      let result = Base58.decode input
      Expect.isOk "failed to decode input" result

      let (Ok actual) = result
      Expect.sequenceEqual "decode result incorrect" expected actual

    testCase "validate base58check checksum" <| fun _ ->
      let testB58Check = "12L5B5yqsf7vwb" // "hello" base58check-encoded

      Address.validateChecksum testB58Check
      |> Expect.isOk "Failed to validate checksum"
  ]

[<Tests>]
let addressTests =
  testList "address tests" [
    testCase "test1 k => WIF" <| fun _ ->
      let expected = test1_wif

      let result = Address.encode Address.AddressType.WIF test1_k
      Expect.isOk "WIF encoding failed for test1 private key" result

      match result with 
      | Ok actual -> Expect.equal "incorrect WIF-encoded result for test1 private key" expected actual
      | _ -> failwith "unexpected error"
    
    testCase "test1 k => WIF-compressed" <| fun _ ->
      let expected = test1_wifCompressed

      let result = Address.encode Address.AddressType.WIFCompressed test1_k
      Expect.isOk "WIF-compressed encoding failed for test1 private key" result

      match result with 
      | Ok actual -> Expect.equal "incorrect WIF-compressed-encoded result for test1 private key" expected actual
      | _ -> failwith "unexpected error"

    testCase "test2 k => WIF" <| fun _ ->
      let expected = test2_wif

      let result = Address.encode Address.AddressType.WIF test2_k
      Expect.isOk "WIF encoding failed for test2 private key" result
      
      match result with
      | Ok actual -> Expect.equal "incorrect WIF-encoded result for test2 private key" expected actual
      | _ -> failwith "unexpected error"

    testCase "test2 k => WIF-compressed" <| fun _ ->
      let expected = test2_wifCompressed

      let result = Address.encode Address.AddressType.WIFCompressed test2_k
      Expect.isOk "WIF-compressed encoding failed for test2 private key" result

      match result with
      | Ok actual -> Expect.equal "incorrect WIF-compressed-encoded result for test2 private key" expected actual
      | _ -> failwith "unexpected error"

    testCase "fail checksum validation" <| fun _ ->
      let testB58BadCheck = "3L5B5yqsVG8Vt"

      Address.validateChecksum testB58BadCheck
      |> Expect.isError "Unexpectedly passed checksum validation"

    testCase "validate WIF key" <| fun _ ->
      let testWif = "5J3mBbAH58CpQ3Y5RNJpUKPE62SQ5tfcvU2JpbnkeyhfsYB1Jcn"

      Address.validateAddress testWif
      |> Expect.isOk "Valid address failed validation"
  ]