using System;
using System.Runtime.InteropServices;

namespace BitThicket.Bitcoin.Cryptography
{
    internal class Ecc {
        public const int BCRYPT_ECDSA_PRIVATE_P256_MAGIC =  0x32534345;
        public const int BCRYPT_ECDSA_PUBLIC_P256_MAGIC =   0x31534345;
    }
}

namespace BitThicket.Bitcoin.Cryptography.Unsafe
{
    // see https://msdn.microsoft.com/en-us/library/windows/desktop/aa375520(v=vs.85).aspx
    // for documented blob structures

    internal unsafe struct EccPrivateBlob256
    {
        public int magic;
        public int keysize;
        public fixed byte x[32];
        public fixed byte y[32];
        public fixed byte d[32];
    }

    internal unsafe struct EccPublicBlob256
    {
        public int magic;
        public int keysize;
        public fixed byte _x[32];
        public fixed byte _y[32];
    }
}
