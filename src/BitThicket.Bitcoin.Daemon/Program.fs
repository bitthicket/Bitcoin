module BitThicket.Bitcoin.Daemon.Program
open System
open Argu
open Logary
open Logary.Message
open System.Net

open BitThicket.Bitcoin.Daemon.Configuration


[<EntryPoint>]
let main argv =
    use mre = new System.Threading.ManualResetEventSlim(false);
    use sub = Console.CancelKeyPress.Subscribe(fun _ -> mre.Set())

    let log = getLogger "main"

    match setArgs argv with
    | Ok _ -> log.info (eventX "Initializing Bitcoin Daemon")
    | Result.Error _ -> 
        log.error (eventX "Failed to initialize Bitcoin Daemon")
        failwith "unexpected failure in initialization"

    mre.Wait()
    0 // return an integer exit code
