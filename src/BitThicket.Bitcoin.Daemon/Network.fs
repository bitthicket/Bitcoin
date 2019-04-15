module BitThicket.Bitcoin.Daemon.Network
open System.Collections.Generic

(*
testnet seeds:
testnet-seed.bitcoin.jonasschnelli.ch
seed.tbtc.petertodd.org
*)

module private PeerLookup =
    let dnsSeeds = 
       [| "dnsseed.bluematt.me";
       "seed.bitcoin.sipa.be";
       "dnsseed.bitcoin.dashjr.org";
       "seed.bitcoinstats.com";
       "seed.bitcoin.jonasschnelli.ch";
       "seed.btc.petertodd.org" |]

    type PeerDescriptor =
       { address : string }
       
    let peers = HashSet<PeerDescriptor>()

    

