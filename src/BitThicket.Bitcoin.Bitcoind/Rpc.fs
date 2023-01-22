namespace BitThicket.Bitcoin.Bitcoind.Rpc
open System
open System.Net.Sockets
open Hopac

(*
    Possible additional modeling:
     - blockHash:string may need a validated "BlockHash" type
     - txid:string maybe a validated txid?
*)

type BlockStat =
    | AvgFee
    | AvgFeeRate
    // TODO

type ScanAction =
    | Start
    | Abort
    | Status

type Command =
    // Blockchain
    | GetBestBlockHash
    | GetBlock of blockHash:string * verbosity:int option
    | GetBlockChainInfo
    | GetBlockCount
    | GetBlockFilter of blockHash:string * filterType:string option
    | GetBlockHash of height:int
    | GetBlockHeader of blockHash:string * verbose:bool option
    | GetBlockStats of hashOrHeight:Choice<string,int> * stats:BlockStat array
    | GetChainTips
    | GetChainTxStats of nblocks:int option * blockHash:string option
    | GetDifficulty
    | GetMempoolAncestors of txid:string * verbose:bool option
    | GetMempoolDescendants of txid:string * verbose:bool option
    | GetMempoolEntry of txid:string
    | GetMempoolInfo
    | GetRawMempool of verbose:bool option
    | GetTxOut of txid:string * n:int * includeMempool:bool option
    | GetTxOutProof of txid:string array * blockHash:string option
    | GetTxOutSetInfo
    | PreciousBlock of blockHash:string
    | PruneBlockChain of height:int
    | SaveMempool
    | ScanTxOutSet of action:ScanAction * scanObjects:obj array option // TODO
    | VerifyChain of checkLevel:int option * nblocks:int option
    | VerifyTxOutProof of proof:string

type RpcClient() =
    member internal _.TcpClient = new TcpClient()
    interface IDisposable with
        member this.Dispose() =
            this.TcpClient.Dispose()

module RpcClient =
    let connect (host:string) port : Job<Result<RpcClient,exn>> = 
        let port = defaultValueArg port 8333
        let client = new RpcClient()
        
        job {
            try
                do! client.TcpClient.ConnectAsync(host, port) 
                    |> Job.awaitUnitTask
                return Ok client
            with
            | x -> 
                return Error x
        }