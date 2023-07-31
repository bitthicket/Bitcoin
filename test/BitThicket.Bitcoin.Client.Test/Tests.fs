namespace BitThicket.Bitcoin.Client

open System

open Microsoft.Extensions.Logging

open Swensen.Unquote
open Xunit
open Xunit.Abstractions

open BitThicket.Bitcoin.Client
open BitThicket.Bitcoin.Protocol

// type BitcoinClientTests(testOutputHelper:ITestOutputHelper) =

//     let bitcoinNodeUri = "tcp://localhost:18333"

//     [<Fact; Trait("Category","Integration")>]
//     let ``simple tcp connect test to testnet node`` () = task {
//         let logger = testOutputHelper.ToLogger()
//         use client = new BitcoinClient(bitcoinNodeUri, logger = logger)
//         let! result = client.Connect()

//         test <@ Result.isOk result && client.IsConnected @>

//         do! client.Disconnect()
//     }


