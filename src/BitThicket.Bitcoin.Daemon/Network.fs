module BitThicket.Bitcoin.Daemon.Network
open System
open System.Collections.Generic
open System.Net
open System.Net.Sockets
open Hopac
open Hopac.Infixes
open Logary
open Logary.Message

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

module private Dns =
    let lookup (hostname:string) = job {
        return! Dns.GetHostEntryAsync(hostname)
    }

    let spreadAddr (entry:IPHostEntry) = 
        seq entry.AddressList

module private Peers =
    let _log = Cfg.getLogger "Network.PeerLookup"

    type PeerDescriptor =
        { address : string }

    let getSeedAddresses() = 
        Cfg.getDnsSeeds() 
        |> Seq.map (Dns.lookup >-> Dns.spreadAddr)
        |> Job.conCollect
        |> Job.map Seq.concat

