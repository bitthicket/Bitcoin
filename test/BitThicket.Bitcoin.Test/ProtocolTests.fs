module BitThicket.Bitcoin.ProtocolTests

open System
open System.Threading.Tasks

open Microsoft.Extensions.Logging
open Xunit
open Xunit.Abstractions
open Swensen.Unquote

open BitThicket.Bitcoin


type ProtocolTests(testOutputHelper:ITestOutputHelper) =

    let bitcoinNodeUri = "tcp://localhost:18333"
    do LoggerFactory.Create(fun builder ->
            builder.AddXUnit(testOutputHelper) |> ignore)
       |> BitThicket.Bitcoin.Logging.Providers.MicrosoftExtensionsLoggingProvider.setMicrosoftLoggerFactory


    [<Fact;
        Trait("Category", "Protocol");
        Trait("Category", "Integration")>]
    let ``version check handshake`` () =
        async {
            // this is super fugly
            let logger = testOutputHelper.ToLogger()
            let mutable contextOpt : IAsyncDisposable option = None
            try
                try
                    let! result = Peer.connect bitcoinNodeUri 60000u 70015u Protocol.Protocol.NodeServices.Unspecified

                    let context =
                        match result with
                        | Ok ctx ->
                            contextOpt <- Some ctx
                            ctx
                        | Error err -> failwithf "connection failed: %A" err

                    sprintf "peer context: %A" context |> logger.LogInformation
                with
                | e -> failwithf "%A" e
            finally
                match contextOpt with
                | Some context ->
                    context.DisposeAsync().AsTask()
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                    logger.LogInformation("disposed context")
                | _ -> ()
        } |> Async.StartAsTask