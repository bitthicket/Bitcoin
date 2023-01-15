namespace BitThicket.Bitcoin

open System
open System.Collections
open System.Security.Cryptography

module Key =

    type Mnemonic = Bip39Mnemonic of string array

    /// length is in bytes
    let private getEntropy length =
        if length <> 16 
           && length <> 20 
           && length <> 24 
           && length <> 28 
           && length <> 32
        then Error("invalid entropy length")
        else RandomNumberGenerator.GetBytes(length) |> Ok
    
    let private getChecksum (entropy:ReadOnlySpan<byte>) =
        // this will always be an whole number of bytes
        let csumLen = entropy.Length / 4
        let hash = SHA256.HashData(entropy)
        let result = hash[0..csumLen]
        result

    /// operator for indexing 11-bit words from extended entropy
    let inline private (@^) (index:int) (bitarray:ReadOnlySpan<byte>) =
        let mutable results = 0
        
        let _0bit = 11 * index
        let _1 = _0bit / 8
        let _1_shift = _0bit % 8
        let _1_bits = bitarray[_1] &&& (0xffuy <<< _1_shift) |> int
        results <- results ||| (_1_bits >>> _1_shift)

        let _2 = _1 + 1
        // 8 - (11 - 8 + _1_shift) = 8 - 11 + 8 - _1_shift = 16 - 11 - _1_shift = 5 - _1_shift
        let _2_shift = 5 - _1_shift
        let _2_mask = if _2_shift >= 8 then 0xffuy else 0xffuy >>> _2_shift
        let _2_bits = bitarray[_2] &&& _2_mask |> int
        results <- results &&& (_2_bits <<< (8-_1_shift))

        if _2_shift > 8 
        then 
            let _3 = _2+1
            let _3_shift = _2_shift % 8
            let _3_mask = 0xffuy >>> _3_shift
            let _3_bits = bitarray[_3] &&& _3_mask |> int
            // shift enough to leave room for the first two chunks
            // _2_bits must necessarily be all 8 bits if we are in this block
            results <- results &&& (_3_bits <<< (16 - _1_shift))

        results
    
    let getMnemonic length = 
        if length <> 12
           && length <> 15
           && length <> 18
           && length <> 21
           && length <> 24
        then Error("invalid mnemonic length")
        else
            let entropyLength = 12*11 - (12*11/32)
            match getEntropy entropyLength with 
            | Error(err) -> Error(err)
            | Ok entropy ->
                let csum = getChecksum entropy
                let extendedEntropy = Array.concat [entropy; csum]


        

