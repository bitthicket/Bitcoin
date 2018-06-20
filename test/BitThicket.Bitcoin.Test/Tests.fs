module Tests

open System.Text
open Expecto
open Expecto.Flip
open BitThicket.Bitcoin


[<Tests>]
let tests =
  testList "base58 tests" [
    testCase "encode 'hello'" <| fun _ ->
      let input = Encoding.UTF8.GetBytes("hello")
      let expected = "Cn8eVZg"
      
      Base58.encode input
      |> Expect.sequenceEqual "encoder produced incorrect output" expected
  ]