module Tests

open System
open Swensen.Unquote
open Xunit

open BitThicket.Bitcoin.Client

let bitcoinNodeUri = "tcp://localhost:18333"

[<Fact>]
let ``simple tcp connect test to testnet node`` () = async {
    use client = new BitcoinClient(bitcoinNodeUri)
    let! result = client.Connect()
    do! client.Disconnect()

    test <@ Result.isOk result  @>
}
