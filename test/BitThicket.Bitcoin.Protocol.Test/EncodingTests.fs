module BitThicket.Bitcoin.PeerNetwork.EncodingTests

open System
open System.Linq
open System.Net
open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols

open FsUnit
open Swensen.Unquote
open Xunit
open BitThicket.Bitcoin.Protocol
open BitThicket.Bitcoin.Protocol.Protocol


[<Fact>]
let ``basic version paylad test`` () =
    // using example from: https://en.bitcoin.it/wiki/Protocol_documentation#version
    let message =
        {
            version = 60002u
            services = NodeServices.Network
            timestamp = 1355839953UL<s>
            receiverServices = NodeServices.Network
            receiverAddress = IPAddress.Parse("10.0.0.1")
            receiverPort = 8333us
            senderServices = NodeServices.Network
            senderAddress = IPAddress.Parse("10.0.0.1")
            senderPort = 8333us
            nonce = 0x3b2eb35d8ce61765UL
            serverAgent = "/Satoshi:0.7.2/"
            blockHeight = 212672u
        }
        |> Version

    let expected = [|
        // header
        // -- magic
        0x0buy; 0x11uy; 0x09uy; 0x07uy
        // -- command "version"
        0x76uy; 0x65uy; 0x72uy; 0x73uy; 0x69uy; 0x6fuy; 0x6euy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy
        // -- payload length: 84 + 1 + 15 = 100 = 0x64
        0x64uy; 0x00uy; 0x00uy; 0x00uy
        // -- checksum (sha256(sha256(payload))
        0x74uy; 0xa7uy; 0xd7uy; 0x65uy

        // payload
        // -- version (24, 4): 60002u
        0x62uy; 0xeauy; 0x00uy; 0x00uy
        // -- services (28, 8): 1UL
        0x01uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy
        // -- timestamp (36, 8): 1355839953UL
        0xd1uy; 0x79uy; 0xd0uy; 0x50uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy
        // -- receiverServices (44, 8): 1UL
        0x01uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy
        // -- receiverAddress (52, 16): ::ffff:10.0.0.1, bigendian
        0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xffuy; 0xffuy; 0x0auy; 0x00uy; 0x00uy; 0x01uy
        // -- receiverPort (68, 2): 8333us, bigendian
        0x20uy; 0x8duy
        // -- senderServices (70, 8): 1UL
        0x01uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy;
        // -- senderAddress (78, 16): ::ffff:10.0.0.1, bigendian
        0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0xffuy; 0xffuy; 0x0auy; 0x00uy; 0x00uy; 0x01uy
        // -- senderPort (94, 2): 8333us, bigendian
        0x20uy; 0x8duy
        // -- nonce (96, 8): 0x3b2eb35d8ce61765UL
        0x65uy; 0x17uy; 0xe6uy; 0x8cuy; 0x5duy; 0xb3uy; 0x2euy; 0x3buy
        // -- serverAgent (104, 1) length 15 = 0x0f
        0x0fuy
        // -- serverAgent (105, 15): "/Satoshi:0.7.2/"
        0x2fuy; 0x53uy; 0x61uy; 0x74uy; 0x6fuy; 0x73uy; 0x68uy; 0x69uy; 0x3auy; 0x30uy; 0x2euy; 0x37uy; 0x2euy; 0x32uy; 0x2fuy
        // -- blockHeight (120, 4): 212672u
        0xc0uy; 0x3euy; 0x03uy; 0x00uy
    |]

    let actual = Encoding.encode message

    let magicExpected = ArraySegment(expected, 0, 4)
    let magicActual = ArraySegment(actual, 0, 4)
    test <@ magicExpected.SequenceEqual(magicActual) @>

    let commandExpected = ArraySegment(expected, 4, 12)
    let commandActual = ArraySegment(actual, 4, 12)
    test <@ commandExpected.SequenceEqual(commandActual) @>

    // payloadLength (16,4) and checksum (20, 4) checked last

    let versionExpected = ArraySegment(expected, 24, 4)
    let versionActual = ArraySegment(actual, 24, 4)
    test <@ versionExpected.SequenceEqual(versionActual) @>

    let servicesExpected = ArraySegment(expected, 28, 8)
    let servicesActual = ArraySegment(actual, 28, 8)
    test <@ servicesExpected.SequenceEqual(servicesActual) @>

    let timestampExpected = ArraySegment(expected, 36, 8)
    let timestampActual = ArraySegment(actual, 36, 8)
    test <@ timestampExpected.SequenceEqual(timestampActual) @>

    let receiverServicesExpected = ArraySegment(expected, 44, 8)
    let receiverServicesActual = ArraySegment(actual, 44, 8)
    test <@ receiverServicesExpected.SequenceEqual(receiverServicesActual) @>

    let receiverAddressExpected = ArraySegment(expected, 52, 16)
    let receiverAddressActual = ArraySegment(actual, 52, 16)
    test <@ receiverAddressExpected.SequenceEqual(receiverAddressActual) @>

    let receiverPortExpected = ArraySegment(expected, 68, 2)
    let receiverPortActual = ArraySegment(actual, 68, 2)
    test <@ receiverPortExpected.SequenceEqual(receiverPortActual) @>

    let senderServicesExpected = ArraySegment(expected, 70, 8)
    let senderServicesActual = ArraySegment(actual, 70, 8)
    test <@ senderServicesExpected.SequenceEqual(senderServicesActual) @>

    let senderAddressExpected = ArraySegment(expected, 78, 16)
    let senderAddressActual = ArraySegment(actual, 78, 16)
    test <@ senderAddressExpected.SequenceEqual(senderAddressActual) @>

    let senderPortExpected = ArraySegment(expected, 94, 2)
    let senderPortActual = ArraySegment(actual, 94, 2)
    test <@ senderPortExpected.SequenceEqual(senderPortActual) @>

    let nonceExpected = ArraySegment(expected, 96, 8)
    let nonceActual = ArraySegment(actual, 96, 8)
    test <@ nonceExpected.SequenceEqual(nonceActual) @>

    let serverAgentLengthExpected = ArraySegment(expected, 104, 1)
    let serverAgentLengthActual = ArraySegment(actual, 104, 1)
    test <@ serverAgentLengthExpected.SequenceEqual(serverAgentLengthActual) @>

    let serverAgentExpected = ArraySegment(expected, 105, 15)
    let serverAgentActual = ArraySegment(actual, 105, 15)
    test <@ serverAgentExpected.SequenceEqual(serverAgentActual) @>

    let blockHeightExpected = ArraySegment(expected, 120, 4)
    let blockHeightActual = ArraySegment(actual, 120, 4)
    test <@ blockHeightExpected.SequenceEqual(blockHeightActual) @>

    // moved this to the end with checksum for similar reasons
    let payloadLengthExpected = ArraySegment(expected, 16, 4)
    let payloadLengthActual = ArraySegment(actual, 16, 4)
    test <@ payloadLengthExpected.SequenceEqual(payloadLengthActual) @>

    // check that we didn't over-allocate
    test <@ actual.Length = expected.Length @>

    // moving this last because it's the most likely to change
    let checksumExpected = ArraySegment(expected, 20, 4)
    let checksumActual = ArraySegment(actual, 20, 4)
    test <@ checksumExpected.SequenceEqual(checksumActual) @>

[<Fact>]
let ``basic verack payload test`` () =
    let expected = [|
    // header
        // -- magic
        0x0buy; 0x11uy; 0x09uy; 0x07uy
        // -- command "verack"
        0x76uy; 0x65uy; 0x72uy; 0x61uy; 0x63uy; 0x6buy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy
        // -- payload length: 0
        0x00uy; 0x00uy; 0x00uy; 0x00uy
        // -- checksum (sha256(sha256(payload))
        0x5duy; 0xf6uy; 0xe0uy; 0xe2uy;
    |]

    let actual = Encoding.encode VerAck

    let magicExpected = ArraySegment(expected, 0, 4)
    let magicActual = ArraySegment(actual, 0, 4)
    test <@ magicExpected.SequenceEqual(magicActual) @>

    let commandExpected = ArraySegment(expected, 4, 12)
    let commandActual = ArraySegment(actual, 4, 12)
    test <@ commandExpected.SequenceEqual(commandActual) @>

    let payloadLengthExpected = ArraySegment(expected, 16, 4)
    let payloadLengthActual = ArraySegment(actual, 16, 4)
    test <@ payloadLengthExpected.SequenceEqual(payloadLengthActual) @>

    // check that we didn't over-allocate
    test <@ actual.Length = expected.Length @>

    let checksumExpected = ArraySegment(expected, 20, 4)
    let checksumActual = ArraySegment(actual, 20, 4)
    test <@ checksumExpected.SequenceEqual(checksumActual) @>

