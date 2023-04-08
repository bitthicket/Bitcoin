module Tests

#nowarn "0025"

open System.Text
open Xunit
open Swensen.Unquote
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

//#region Base58 decode test data
let decode_test_data = seq {
    // 123
    yield [|
        "38" :> obj
        [| 0x7buy |]
    |]

    // test
    yield [|
        "3yZe7d" :> obj
        [| 0x74uy; 0x65uy; 0x73uy; 0x74uy |]
    |]

    // "hello"
    yield [|
        "Cn8eVZg" :> obj
        [| 0x68uy; 0x65uy; 0x6cuy; 0x6cuy; 0x6fuy |]
    |]

    // "fishy"
    yield [|
        "CZ9Z8UY" :> obj
        [| 0x66uy; 0x69uy; 0x73uy; 0x68uy; 0x79uy |]
    |]

    // "hockey"
    yield [|
        "u1KxT8tp" :> obj
        [| 0x68uy; 0x6fuy; 0x63uy; 0x6buy; 0x65uy; 0x79uy |]
    |]

    // "Collins"
    yield [|
        "3ZDgUv5ihG" :> obj
        [| 0x43uy; 0x6fuy; 0x6cuy; 0x6cuy; 0x69uy; 0x6euy; 0x73uy |]
    |]

    // "Hello, World!"
    yield [|
        "72k1xXWG59fYdzSNoA" :> obj
        [| 0x48uy; 0x65uy; 0x6cuy; 0x6cuy; 0x6fuy; 0x2cuy; 0x20uy;
           0x57uy; 0x6fuy; 0x72uy; 0x6cuy; 0x64uy; 0x21uy |]
    |]

    yield [|
        "5J3mBbAH58CpQ3Y5RNJpUKPE62SQ5tfcvU2JpbnkeyhfsYB1Jcn" :> obj
        [|// version byte
            0x80uy;
            // payload
            0x1euy; 0x99uy; 0x42uy; 0x3auy; 0x4euy; 0xd2uy; 0x76uy; 0x08uy;
            0xa1uy; 0x5auy; 0x26uy; 0x16uy; 0xa2uy; 0xb0uy; 0xe9uy; 0xe5uy;
            0x2cuy; 0xeduy; 0x33uy; 0x0auy; 0xc5uy; 0x30uy; 0xeduy; 0xccuy;
            0x32uy; 0xc8uy; 0xffuy; 0xc6uy; 0xa5uy; 0x26uy; 0xaeuy; 0xdduy;
            // checksum
            0xc4uy; 0x7euy; 0x83uy; 0xffuy|] :> obj
    |]
}
//#endregion

[<Fact>]
[<Trait("Category", "Base58")>]
let encode_hello () =
    let input = Encoding.UTF8.GetBytes("hello")

    test <@ Base58.encode input = "Cn8eVZg" @>

[<Fact>]
[<Trait("Category", "Base58")>]
let decode_hello () =
    test <@ Encoding.UTF8.GetBytes("hello") |> Ok = Base58.decode "Cn8eVZg" @>

[<Theory; MemberData(nameof decode_test_data)>]
[<Trait("Category", "Base58")>]
let decode_bytes input decoded_bytes =
    let expected : Result<byte[],EncodingError> = Ok decoded_bytes
    test <@ Base58.decode input = expected @>

[<Fact>]
[<Trait("Category", "Base58")>]
let ``decode base58check-encoded hello`` () =
    let input = "12L5B5yqsf7vwb"
    let expected = [|0x00uy; 0x68uy; 0x65uy; 0x6cuy; 0x6cuy; 0x6fuy; 0x9cuy; 0x3cuy; 0x23uy; 0x62uy|]
                   |> Ok

    test <@ Base58.decode input = expected @>


let ``validate base58check checksum`` () =
    let input = "12L5B5yqsf7vwb" // "hello" base58check-encoded

    let result = Base58.decode input
                 |> Result.bind (Base58Check.validateChecksum >> (Result.mapError Base58CheckError))

    test <@ Result.isOk result @>

[<Fact>]
[<Trait("Category", "Base58")>]
let ``decode base58check string`` () =
    let input = "12L5B5yqsf7vwb" // "hello" base58check-encoded
    let result = Base58Check.decode input

    test <@ Result.isOk result @>

[<Fact>]
[<Trait("Category","Base58")>]
let ``fail checksum validation`` () =
    let testB58BadCheck = "3L5B5yqsVG8Vt"
    let result = Base58.validate testB58BadCheck

    test <@ Result.isOk result @>