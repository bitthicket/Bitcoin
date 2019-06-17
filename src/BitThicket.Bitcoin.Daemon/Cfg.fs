module BitThicket.Bitcoin.Daemon.Cfg
open System
open System.Net
open Argu
open Hopac
open Logary
open Logary.Configuration
open Logary.Message
open Logary.Targets

// BitThicket
open Util

let rec private _moduleType = getModuleType <@ _moduleType @>

type BitcoinNetwork = | Mainnet | Testnet | Regtest

let private mainDnsSeeds = 
        [| "dnsseed.bluematt.me";
           "seed.bitcoin.sipa.be";
           "dnsseed.bitcoin.dashjr.org";
           "seed.bitcoinstats.com";
           "seed.bitcoin.jonasschnelli.ch";
           "seed.btc.petertodd.org" |]

let private testDnsSeeds = 
        [| "seed.tbtc.petertodd.org" |]

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
    let console = LiterateConsole.create LiterateConsole.empty "console"
    let logfileName = File.Naming ("{service}-{date}", "log")
    let logfile = File.create (File.FileConf.create Environment.CurrentDirectory logfileName) "file"
    Config.create "BitThicket.Bitcoin.Daemon" (Dns.GetHostName())
    |> Config.targets [console; logfile] 
    |> Config.ilogger (ILogger.Console Info)
    |> Config.build
    |> run

let private logary = buildLogary()
let getLogger (name:string) =
    logary.getLogger (PointName.parse name)

let private log = logary.getLogger _moduleType.FullName
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
        
        log.info (eventX <| sprintf "Setting log level: %A" logLevel)
        logary.switchLoggerLevel (".*", logLevel)

        parsedArgs <- pa |> Some
        Result.Ok()

let getArgs() = 
    match parsedArgs with
    | Some args -> Result.Ok args
    | None ->
        log.error (eventX ("Attempt to retrieve args before initialization"))
        Result.Error()

let getNetwork () = 
    let pa = Option.get parsedArgs
    pa.GetResult Network

let getPort () =
    match getNetwork() with
    | Mainnet -> 8333
    | Testnet -> 18333
    | Regtest -> failwith "unsupported network"

let getDnsSeeds () =
    match getNetwork() with
    | Mainnet -> mainDnsSeeds
    | Testnet -> testDnsSeeds
    | Regtest -> failwith "unsupported network"

let getMaxPeers () =
    let pa = Option.get parsedArgs
    pa.GetResult (MaxPeers, defaultValue = uint8 8)