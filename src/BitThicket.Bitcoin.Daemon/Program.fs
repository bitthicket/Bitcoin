module BitThicket.Bitcoin.Daemon.Program
open System
open Argu
open Hopac
open Hopac.Infixes
open Logary
open Logary.Message
open System.Net

open BitThicket.Bitcoin.Daemon.Cfg
open BitThicket.Bitcoin.Daemon.Network


[<EntryPoint>]
let main argv =
    use mre = new System.Threading.ManualResetEventSlim(false);
    use sub = Console.CancelKeyPress.Subscribe(fun _ -> mre.Set())

    let log = getLogger "main"

    match setArgs argv with
    | Ok _ -> 
        let initStr = getNetwork() |> sprintf "Initializing Bitcoin Daemon (%A)"
        log.info (eventX initStr)

        let dnsCh = Peers.startDiscoveryServer() |> run

        log.debug (eventX <| sprintf "discovery server started")
        Peers.getSeedAddresses dnsCh |> run |> ignore

    | Result.Error _ -> 
        log.error (eventX "Failed to initialize Bitcoin Daemon")
        failwith "unexpected failure in initialization"

    mre.Wait()
    0 // return an integer exit code
