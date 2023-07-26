namespace BitThicket.Bitcoin.Protocol

module Protocol =

    open System
    open System.Net
    open System.Text
    open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols

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

            member self.AsReadOnlyMemory() =
                match self with ByteMemoryRef b -> b

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
            member self.AsReadOnlyMemory() =
                match self with ByteMemoryRef b -> b

    module Commands =
        let version = "version" |> Encoding.UTF8.GetBytes
        let verack = "verack" |> Encoding.UTF8.GetBytes

    type MessageHeader =
        { magic: ByteMemoryRef4
          command: ByteMemoryRef12Max
          payloadLength: uint32
          checksum: ByteMemoryRef4 }

    [<Flags>]
    type NodeServices =
        | Unspecified   = 0x00000000UL
        | Network       = 0x00000001UL
        | GetUTXO       = 0x00000002UL
        | Bloom         = 0x00000004UL
        | Witness       = 0x00000008UL
        | Xthin         = 0x00000010UL
        | Limited       = 0x00000400UL

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

    type MessagePayload =
        | Version of VersionMessage
        | VerAck


