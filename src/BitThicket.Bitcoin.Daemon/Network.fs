module BitThicket.Bitcoin.Daemon.Network
open System.Net
open System.Net.Sockets
open Hopac
open Hopac.Infixes
open Logary
open Logary.Message
open Hopac


module private Socket =
    let _log = Cfg.getLogger "Network.Socket"

    type SocketExn(error:SocketError) =
        inherit exn()
        member __.SocketError
            with get() = error

    // TODO: could possibly implement an object pool of SocketAsyncEventArgs
    // if it becomes expensive to new these on every connect.
    // https://docs.microsoft.com/en-us/dotnet/standard/collections/thread-safe/how-to-create-an-object-pool
    // I'm not sure how hot this path will be though.

    let private connectHandler cont econt (saea:SocketAsyncEventArgs) =
        let ipe = saea.RemoteEndPoint
        match saea.SocketError with
        | SocketError.Success ->
            _log.debug (eventX <| sprintf "socket connected to %A" ipe)
            cont saea.ConnectSocket
        | socketError -> 
            _log.debug (eventX <| sprintf "failed to connect socket to %A: %A" ipe socketError)
            SocketExn(socketError) |> econt

    let private makeConnectSaea (addr:IPAddress) (port:uint16) cont econt =
        let saea = new SocketAsyncEventArgs(RemoteEndPoint = IPEndPoint(addr, int port))
        saea.Completed.Add(connectHandler cont econt)
        saea

    let connect (s:Socket) (addr:IPAddress) (port:uint16) = 
        Async.FromContinuations (fun (cont, econt, ccont) ->
            try
                let saea = makeConnectSaea addr port cont econt
                if (not (s.ConnectAsync(saea)))
                then
                    _log.debug (eventX <| sprintf "socket connected synchronously to %A" saea.RemoteEndPoint)
                    cont s
                else ()
            with
            | exn -> 
                _log.error (eventX <| sprintf "Socket operation failed. %A" exn)
                econt exn
        ) |> Alt.fromAsync

module Peers =
    let _log = Cfg.getLogger "Network.PeerLookup"

    type PeerDescriptor =
        { address : string }

    let lookup (hostname:string) =
        _log.debug (eventX <| sprintf "looking up %s" hostname)
        Alt.fromTask (fun ct -> Dns.GetHostEntryAsync(hostname))

    let sendHost (ch:Ch<IPHostEntry>) (iphe:IPHostEntry) = 
        _log.debug (eventX <| sprintf "got IPHostEntry, sending")
        ch *<- iphe

    let getSeedAddresses ch = 
        _log.debug (eventX <| sprintf "doing seed lookup")
        Cfg.getDnsSeeds()
        |> Array.map (fun seed -> lookup seed ^=> sendHost ch)
        |> Job.seqCollect

    let startDiscoveryServer () = job {
        let ch = Ch<IPHostEntry>()
        let rec server () = job {
            _log.debug (eventX <| sprintf "waiting on IPHostEntry")
            let! iphe = Ch.take ch
            _log.debug (eventX <| sprintf "got IPHostEntry with %d addresses" iphe.AddressList.Length)
            return! server()
        }
        do! Job.start (server())
        return ch
    }
(*
    let takeInt ch = job {
        printfn "taking int"
        let! x = Ch.take ch
        printfn "int: %d" x
    }

    let intServer () = job {
        let c = Ch<uint64>()
        let rec server () = job {
            let! i = Ch.take c
            printfn "int: %d" i
            return! server ()
        }
        do! Job.start (server ())
        return c
    }

    let giveInt ch x = job {
        return! Ch.give ch x
    }
*)