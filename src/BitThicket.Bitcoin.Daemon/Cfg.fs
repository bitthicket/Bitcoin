module BitThicket.Bitcoin.Daemon.Cfg
open System
open System.Net
open Argu
open Hopac
open Logary
open Logary.Configuration
open Logary.Message
open Logary.Targets

type BitcoinNetwork = | Mainnet | Testnet | Regtest

type Arguments =
    | [<Mandatory>] Network of BitcoinNetwork
    | Log_Level of LogLevel
    | MaxPeers of uint8
with 
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Network _ -> "Specify which blockchain to use"
            | Log_Level _ -> "Set log level"
            | MaxPeers _ -> "Node will connect to no more peers than this"

let private defaultLogLevel = Info

let private buildLogary() = 
    Config.create "BitThicket.Bitcoin.Daemon" (Dns.GetHostName())
    |> Config.target (LiterateConsole.create LiterateConsole.empty "console")
    |> Config.ilogger (ILogger.Console Info)
    |> Config.build
    |> run

let mutable private logary = buildLogary()

let private log = logary.getLogger "Configuration.Agent"
let mutable private parsedArgs : ParseResults<Arguments> option = None

let errorHandler = ProcessExiter(colorizer = function 
    | ErrorCode.HelpText -> None 
    | _ -> Some ConsoleColor.Red)

let private parser = ArgumentParser.Create<Arguments>(programName = "BitcoinDaemon", errorHandler = errorHandler)

let setArgs args =
    match parsedArgs with
    | Some _ -> 
        log.error (eventX "Attempt to set args after initialization")
        Result.Error()
    | None ->
        let pa = parser.Parse args
        let logLevel = pa.GetResult(Log_Level, Info)
        
        if (logLevel <> defaultLogLevel) then 
            logary.shutdown() |> ignore
            logary <- buildLogary()

        parsedArgs <- pa |> Some
        Result.Ok()

let getArgs() = 
    match parsedArgs with
    | Some args -> Result.Ok args
    | None ->
        log.error (eventX ("Attempt to retrieve args before initialization"))
        Result.Error()

let getLogger (name:string) =
    logary.getLogger name

let getNetwork () = 
    let pa = Option.get parsedArgs
    pa.GetResult Network

let getMaxPeers () =
    let pa = Option.get parsedArgs
    pa.GetResult (MaxPeers, defaultValue = uint8 8)