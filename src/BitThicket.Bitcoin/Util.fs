namespace BitThicket.Bitcoin

module internal Bits =
  open System

  [<AbstractClass; Sealed>]
  type Converter () =
    static member GetBytesBE(value : bool) =
      BitConverter.GetBytes(value)
      |> if BitConverter.IsLittleEndian then Array.rev else id
    static member GetBytesBE(value : char) =
      BitConverter.GetBytes(value)
      |> if BitConverter.IsLittleEndian then Array.rev else id
    static member GetBytesBE(value : float) =
      BitConverter.GetBytes(value)
      |> if BitConverter.IsLittleEndian then Array.rev else id
    static member GetBytesBE(value : int16) =
      BitConverter.GetBytes(value)
      |> if BitConverter.IsLittleEndian then Array.rev else id
    static member GetBytesBE(value : int32) =
      BitConverter.GetBytes(value)
      |> if BitConverter.IsLittleEndian then Array.rev else id
    static member GetBytesBE(value : int64) =
      BitConverter.GetBytes(value)
      |> if BitConverter.IsLittleEndian then Array.rev else id
    static member GetBytesBE(value : float32) =
      BitConverter.GetBytes(value)
      |> if BitConverter.IsLittleEndian then Array.rev else id
    static member GetBytesBE(value : uint16) =
      BitConverter.GetBytes(value)
      |> if BitConverter.IsLittleEndian then Array.rev else id
    static member GetBytesBE(value : uint32) =
      BitConverter.GetBytes(value)
      |> if BitConverter.IsLittleEndian then Array.rev else id
    static member GetBytesBE(value : uint64) =
      BitConverter.GetBytes(value)
      |> if BitConverter.IsLittleEndian then Array.rev else id
    static member GetBytesBE(value : bigint) =
      value.ToByteArray()
      |> if BitConverter.IsLittleEndian then Array.rev else id

  /// takes a byte array in big-endian order and ensures
  /// that the most significant bit is set to zero by prepending a 0-byte 
  /// if the msb is 1
  let ensureZeroMsBit (bytes:byte array) =
    match bytes.[0] &&& 0x80uy with
    | 0x80uy -> Array.append [|0uy|] bytes
    | _ -> bytes

  /// takes a byte array in big-endian order and ensures that the leading
  /// byte is 0
  let ensureZeroMsByte (bytes:byte array) =
    if bytes.[0] = 0uy then bytes
    else Array.append[|0uy|] bytes

  /// takes a byte array in big-endian order and removes leading 0 bytes
  let removeZeroMsBytes (bytes:byte array) =
    let nonZeroIndex = Array.findIndex (fun b -> b > 0uy) bytes
    bytes.[nonZeroIndex..]