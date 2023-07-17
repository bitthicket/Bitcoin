open FSharp.Core
// encodings

type Base58String = Base58String of string
type Base58CheckString = Base58CheckString of string

type EncodedString =
    | Base58 of Base58String
    | Base58Check of Base58CheckString

// Keys & Addresses

type PrivateKey =
    | Binary of byte[]
    | Hex of string
    | Wif of Base58CheckString
    | WifCompressed of Base58CheckString

type PublicKey =
    | Bip32ExtendedPubKey of Base58CheckString

type Address =
    | BitcoinAddress of Base58CheckString
    | P2shAddress of Base58CheckString
    | TestnetAddress of Base58CheckString
    | Bip38EncryptedPrivKey of Base58CheckString

type Mnemonic =
    | Mnemonic12 of string[]
    | Mnemonic24 of string[]

// always 512 bits / 64 bytes
type Seed = Seed of byte[]

type KeyIdentifier = KeyIdentifier of int list

// Transactions

[<Measure>] type bitcoin
[<Measure>] type satoshi


module Signature =
    module Flags =
        let SIGHASH_ALL             = 0x01uy
        let SIGHASH_NONE            = 0x02uy
        let SIGHASH_SINGLE          = 0x03uy
        let SIGHASH_ANYONECANPAY    = 0x80uy

module Transaction =
    type TransactionOutput =
        { Value: uint64<satoshi>
          LockScript: byte[] }   // scriptPubKey

    type TransactionInput =
        { TxId: byte[]
          OutputIndex: uint32
          UnlockScript: byte[]  // scriptSig
          Sequence: uint32 }

    module NSequenceFlags =
        let NO_LOCKTIME               = 1 <<< 31
        let LOCKTIME_TYPE_SECONDS     = 1 <<< 22
        let LOCKTIME_VALUE_MASK       = 0x0000FFFF

