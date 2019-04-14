module BitThicket.Bitcoin.Daemon.Program
open System
open Argu
open Hopac
open Logary
open Logary.Configuration
open Logary.Message
open Logary.Targets
open System.Net

open BitThicket.Bitcoin.Daemon.Configuration


[<EntryPoint>]
let main argv =
    use mre = new System.Threading.ManualResetEventSlim(false);
    use sub = Console.CancelKeyPress.Subscribe(fun _ -> mre.Set())

    let parser = ArgumentParser.Create<Arguments>(programName = "BitcoinDaemon.exe")
    let parsedArgs = parser.Parse argv



    // let _log = logary.getLogger "Program.main"
    // _log.info (eventX "Starting up Bit Thicket Bitcoin Daemon")

    mre.Wait()
    0 // return an integer exit code
