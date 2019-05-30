module BitThicket.Bitcoin.Daemon.Network
open System
open System.Collections.Generic
open System.Net
open System.Net.Sockets
open Hopac
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

module private Peers =
    let _log = Cfg.getLogger "Network.PeerLookup"

    type PeerDescriptor =
        { address : string }

    let private mainDnsSeeds = 
        [| "dnsseed.bluematt.me";
           "seed.bitcoin.sipa.be";
           "dnsseed.bitcoin.dashjr.org";
           "seed.bitcoinstats.com";
           "seed.bitcoin.jonasschnelli.ch";
           "seed.btc.petertodd.org" |]

    let private testDnsSeeds = 
        [| "seed.tbtc.petertodd.org" |]

    let shuffle arr =
        let rand = new Random(int DateTime.Now.Ticks)
        Array.sortBy (fun _ -> rand.Next()) arr

    let peerDirectoryJob = job {
        let peers = HashSet<PeerDescriptor>()

        // wait on requests for peers
        ()
    }

    let private findPeersJob = job {
        let dnsSeeds = match Cfg.getNetwork() with
                        | Cfg.Mainnet -> shuffle mainDnsSeeds
                        | Cfg.Testnet -> shuffle testDnsSeeds
                        | Cfg.Regtest -> failwith "unsupported network"
        let port = match Cfg.getNetwork() with
                   | Cfg.Mainnet -> 8333
                   | Cfg.Testnet -> 18333
                   | Cfg.Regtest -> failwith "unsupported network"

        let maxPeers = Cfg.getMaxPeers()

        let rec getPeerCandidates candidates seed = job {
            let! addrs = Dns.GetHostAddressesAsync(seed)
            let connectAlts = 
                addrs 
                |> Array.map (fun ip -> 
                    let s = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                    Socket.connect s ip port)
        }

        ()
    }

