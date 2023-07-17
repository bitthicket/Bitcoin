namespace BitThicket.Bitcoin.PeerNetwork

module Protocol =

    open System
    open System.Net
    open System.Text

    let NETWORK_MAGIC_MAINNET = 0xd9b4bef9u
    let NETWORK_MAGIC_TESTNET = 0x0b110907u
    let NETWORK_MAGIC_REGTEST = 0xfabfb5dau

    let EMPTY_PAYLOAD_CHECKSUM = 0x5df6e0e2u

    module Commands =
        let version = "version" |> Encoding.UTF8.GetBytes

    type MessageHeader =
        { magic: uint32
          command: ReadOnlyMemory<byte>
          payloadLength: uint32
          checksum: uint32 }

    [<Flags>]
    type NodeServices =
        | Unspecified   = 0x00000000u
        | Network       = 0x00000001u
        | GetUTXO       = 0x00000002u
        | Bloom         = 0x00000004u
        | Witness       = 0x00000008u
        | Xthin         = 0x00000010u
        | Limited       = 0x00000400u

    type VersionMessage =
        {
          /// the highest protocol version understood by the transmitting node
          version: uint32                      // nVersion

          /// bitfield of features to be enabled for this connection
          services: uint64                  // nServices

          /// standard UNIX timestamp in seconds according to the transmitting node's clock
          timestamp: uint64                 // nTime, unix

          /// Services supported by the receiving node as perceived by the transmitting node
          receiverServices: uint64          // addr_recv services

          /// IP address of the receiving node
          receiverAddress: IPAddress        // addrYou

          /// port number of the receiving node
          receiverPort: uint16

          /// Services supported by the transmitting node; should match services
          senderServices: uint64

          /// IP address of the transmitting node
          senderAddress: IPAddress

          /// port number of the transmitting node
          senderPort: uint16

          nonce: uint64

          serverAgent: string

          blockHeight: int }

    type MessagePayload =
        | Version of VersionMessage

    type Message =
        { header: MessageHeader
          payload: MessagePayload }
