module BitThicket.Bitcoin.Daemon.Configuration
open System.Net
open Argu
open Hopac
open Logary
open Logary.Configuration
open Logary.Message
open Logary.Targets

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

type ConfigurationMessage = 
    | SetArgs of string array
    | GetSetting of string
    | GetLogger of string

type Settings() =
    static let logary = 
        Config.create "BitThicket.Bitcoin.Daemon" (Dns.GetHostName())
        |> Config.target (LiterateConsole.create LiterateConsole.empty "console")
        |> Config.ilogger (ILogger.Console Debug)
        |> Config.build
        |> run
    
    static let log = logary.getLogger "Configuration.Agent"

    static let mutable parsedArgs : ParseResults<Arguments> option = None

    static let parser = ArgumentParser.Create<Arguments>(programName = "BitcoinDaemon")

    static let agent = MailboxProcessor.Start(fun inbox ->
        let rec messageLoop() = async {
            let! msg = inbox.Receive()
            
            match msg with
            | SetArgs args -> 
                match parsedArgs with
                | Some _ -> 
                    log.error (eventX "Attempt to set args after initialization")
                | None -> 
                    parsedArgs <- parser.Parse args |> Some
            | GetSetting setting -> 
                log.debug (eventX (sprintf "requested settting: %s" setting))
            | GetLogger name -> 
                log.debug (eventX (sprintf "request for logger '%s'" name))

            return! messageLoop()
        }
        messageLoop() )