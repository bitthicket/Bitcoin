namespace BitThicket.Bitcoin.Protocol

[<AutoOpen>]
module Protocol =

    open System
    open System.Net
    open System.Text
    open Microsoft.FSharp.Core.Printf
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols


    (*
        NOTES
         - Best summary documentation of protocol:
            - https://en.bitcoin.it/wiki/Protocol_documentation
            - https://developer.bitcoin.org/reference/intro.html
         - version <= 31800 not supported?  source: https://en.bitcoin.it/wiki/Version_Handshake
    *)

    let NETWORK_MAGIC_MAINNET = [| 0xf9uy; 0xbeuy; 0xb4uy; 0xd9uy; |]
    let NETWORK_MAGIC_TESTNET = [| 0x0buy; 0x11uy; 0x09uy; 0x07uy |]
    let NETWORK_MAGIC_REGTEST = [| 0xfauy; 0xbfuy; 0xb5uy; 0xdauy |]

    let EMPTY_PAYLOAD_CHECKSUM = [| 0x5duy; 0xf6uy; 0xe0uy; 0xe2uy |]

    (*
        Things needed for each message type:
        - entry in Commands module
        - a new payload type
        - an entry in the MessagePayload union
        - a block in the match expression of the Encoding.calculatePayloadSize function
        - a private encoding function in the Encoding module
        - a block in the match expression in Encoding.encodePeerMessage
    *)

    type ByteMemoryRef4 = private ByteMemoryRef of ReadOnlyMemory<byte>
        with
            static member op_Implicit (b:byte[]) =
                if b.Length <> 4 then
                    failwith "ByteMemoryRef4: array must be 4 bytes"
                ByteMemoryRef (b.AsMemory() |> Memory<byte>.op_Implicit)
            static member op_Implicit (b:ReadOnlyMemory<byte>) =
                if b.Length <> 4 then
                    failwith "ByteMemoryRef4: array must be 4 bytes"
                ByteMemoryRef b

            member self.AsMemory() =
                match self with ByteMemoryRef b -> b
            member self.AsSpan() =
                match self with ByteMemoryRef b -> b.Span
            override self.ToString() =
                match self with ByteMemoryRef b ->
                    let buf = new StringBuilder()
                    for i in 0..b.Length-1 do
                        if i <> 0 then buf.Append(" ") |> ignore else ()
                        bprintf buf "%02x" b.Span[i]
                    buf.ToString()

    type ByteMemoryRef12Max = private ByteMemoryRef of ReadOnlyMemory<byte>
        with
            static member op_Implicit (b:byte[]) =
                if b.Length > 12 then
                    failwith "ByteMemoryRef12: array must be 12 bytes or less"
                ByteMemoryRef (b.AsMemory() |> Memory<byte>.op_Implicit)
            static member op_Implicit (b:ReadOnlyMemory<byte>) =
                if b.Length > 12 then
                    failwith "ByteMemoryRef12: array must be 12 bytes or less"
                ByteMemoryRef b

            member self.AsMemory() =
                match self with ByteMemoryRef b -> b
            member self.AsSpan() =
                match self with ByteMemoryRef b -> b.Span
            override self.ToString() =
                match self with ByteMemoryRef b ->
                    let buf = new StringBuilder()
                    for i in 0..b.Length-1 do
                        if i <> 0 then buf.Append(" ") |> ignore else ()
                        bprintf buf "%02x" b.Span[i]
                    buf.ToString()

    module rec Commands =
        type Command =
            { text: string
              bytes: ByteMemoryRef12Max }

        let private _version = "version"
        let version = { text = _version
                        bytes = _version |> Encoding.UTF8.GetBytes |> ByteMemoryRef12Max.op_Implicit }

        let private _verack = "verack"
        let verack = { text = _verack
                       bytes = _verack |> Encoding.UTF8.GetBytes |> ByteMemoryRef12Max.op_Implicit }


    type MessageHeader =
        { magic: ByteMemoryRef4
          command: ByteMemoryRef12Max
          payloadLength: uint32
          checksum: ByteMemoryRef4 }

    // TODO: how does BIP 37 fit in here?  What commands must be supported regardless of service bits?
    [<Flags>]
    type NodeServices =
        | Unspecified       = 0x00000000UL
        /// responds to requests for full blocks
        | Network           = 0x00000001UL
        /// supports commands for utxo queries; bip 64
        | GetUTXO           = 0x00000002UL
        /// supports connection bloom filtering; bip 111
        | Bloom             = 0x00000004UL
        /// supports segregated witness; bip 144
        | Witness           = 0x00000008UL
        /// deprecated; only listed here for completeness.  not used by bitcoin core
        | Xthin             = 0x00000010UL
        /// supports compact block filters; bip 157,158
        | CompactFilters    = 0x00000040UL
        /// serves at least the last 288 blocks; bip 159
        | Limited           = 0x00000400UL

    // good reference for versions: https://developer.bitcoin.org/reference/p2p_networking.html#protocol-versions

    type VersionMessage =
        { version: uint32
          services: NodeServices
          timestamp: uint64<s>
          receiverServices: NodeServices
          receiverAddress: IPAddress
          receiverPort: uint16
          senderServices: NodeServices
          senderAddress: IPAddress
          senderPort: uint16
          nonce: uint64
          serverAgent: string
          blockHeight: uint32 }

    type Message =
        | Version of VersionMessage
        | VerAck


