namespace BitThicket.Bitcoin

open System
open System.Collections
open System.Security.Cryptography
open BitThicket.Bitcoin.Bips

#nowarn "3391"

module Key =

    type PrivateKeyFormat =
        | Raw
        | Hex
        | WIF
        | WIFCompressed
        | TestnetWIF
        | TestnetWIFCompressed

    type PublicKeyFormat =
        | Raw
        | Hex
        | Compressed
        | Uncompressed




