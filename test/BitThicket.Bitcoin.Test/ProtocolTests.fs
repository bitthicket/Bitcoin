module BitThicket.Bitcoin.ProtocolTests

open System

open Microsoft.Extensions.Logging
open Xunit
open Xunit.Abstractions
open Swensen.Unquote

open BitThicket.Bitcoin


type ProtocolTests(testOutputHelper:ITestOutputHelper) =

    let bitcoinNodeUri = "tcp://localhost:18333"

    [<Fact;
        Trait("Category", "Protocol");
        Trait("Category", "Integration")>]
    let ``version check handshake`` () =
        async {
            let! result = Peer.connect bitcoinNodeUri 60000u 70015u Protocol.Protocol.NodeServices.Unspecified

            test <@ Result.isOk result @>
        } |> Async.StartAsTask