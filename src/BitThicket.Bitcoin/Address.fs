namespace BitThicket.Bitcoin

module Address =

    let validateAddress address =
      match validateChecksum address with
      | Error err -> Error err
      | Ok _ -> 
        let atype = match address with
                    | PrefixP2PKH _ -> Some P2PKH
                    | PrefixWIF _ -> Some WIF
                    | PrefixWIFCompressed _ -> Some WIFCompressed
                    | PrefixTestnetAddress _ -> Some TestnetAddress
                    | PrefixBIP32ExtendedPublicKey _ -> Some BIP32ExtendedPublicKey
                    | _ -> None
        match atype with
        | None -> UnsupportedAddressFormat address |> Error
        | Some addrType -> { addressType = addrType; base58Check = address } |> Ok

    