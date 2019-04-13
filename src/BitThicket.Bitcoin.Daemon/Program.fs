module BitThicket.Bitcoin.Daemon.Program
open System
open Argu
open Hopac
open Logary
open Logary.Configuration
open Logary.Message
open Logary.Targets
open System.Net

type BitcoinNetwork = | Mainnet | Testnet

type Arguments =
    | Network of BitcoinNetwork
    | Log_Level of LogLevel
with 
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Network _ -> "Specify which blockchain to use"
            | Log_Level _ -> "Set log level"

[<EntryPoint>]
let main argv =
    use mre = new System.Threading.ManualResetEventSlim(false);
    use sub = Console.CancelKeyPress.Subscribe(fun _ -> mre.Set())

    let logary = 
        Config.create "BitThicket.Bitcoin.Daemon" (Dns.GetHostName())
        |> Config.target (LiterateConsole.create LiterateConsole.empty "console")
        |> Config.ilogger (ILogger.Console Debug)
        |> Config.build
        |> run

    let parser = ArgumentParser.Create<Arguments>(programName = "BitcoinDaemon.exe")
    parser.PrintUsage() |> printfn "%s"

    let _log = logary.getLogger "Program.main"
    _log.info (eventX "Starting up Bit Thicket Bitcoin Daemon")

    mre.Wait()
    0 // return an integer exit code
