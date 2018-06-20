namespace BitThicket.Bitcoin


open System.IO

module Base58 = 
    open System

    let private _encTable = [| '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', // 0-9
                               'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', // 10-19
                               'M', 'N', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', // 20-29
                               'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 'g', // 30-39
                               'h', 'i', 'j', 'k', 'm', 'n', 'o', 'p', 'q', 'r', // 40-49
                               's', 't', 'u', 'v', 'w', 'x', 'y', 'z' |] // 50-57

    // this could definitely benefit from Span work
    /// expects input array to be in big-endian order (higher-order bytes precede lower-order ones 
    /// - i.e., data[0] is the MSB)
    let internal encode (data:byte array) =
      let rec _encode (acc:MemoryStream) (n:bigint) =
          if n = 0I then acc.ToArray()
          else
              let rem : bigint ref = ref 0I
              let next = bigint.DivRem(n, 58I, rem)
              acc.WriteByte(!rem |> byte)
              _encode acc next
      
      use ms = new MemoryStream()
      _encode ms (Array.Reverse(data) |> bigint)

    let decode string =
        Array.zeroCreate<byte> 0