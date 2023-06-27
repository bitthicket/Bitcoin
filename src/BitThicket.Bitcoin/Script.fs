namespace BitThicket.Bitcoin

module Script =

    // make all the following names be all caps separated by underscores
    module Operations =
        // Push value onto stack

        /// An empty array is pushed onto the stack
        let OP_0            = 0x00uy
        /// An empty array is pushed onto the stack
        let OP_FALSE        = 0x00uy
        /// The next byte contains the number of bytes to be pushed onto the stack
        let OP_PUSHDATA1    = 0x4cuy
        /// The next two bytes contain the number of bytes to be pushed onto the stack
        let OP_PUSHDATA2    = 0x4duy
        /// The next four bytes contain the number of bytes to be pushed onto the stack
        let OP_PUSHDATA4    = 0x4euy
        /// The number -1 is pushed onto the stack
        let OP_1NEGATE      = 0x4fuy
        /// Halt - invalid transaction unless found in an unexecuted OP_IF branch
        let OP_RESERVED     = 0x50uy
        /// The number 1 is pushed onto the stack
        let OP_1            = 0x51uy
        /// The number 1 is pushed onto the stack
        let OP_TRUE         = 0x51uy
        /// Push 2 onto the stack
        let OP_2            = 0x52uy
        /// Push 3 onto the stack
        let OP_3            = 0x53uy
        /// Push 4 onto the stack
        let OP_4            = 0x54uy
        /// Push 5 onto the stack
        let OP_5            = 0x55uy
        /// Push 6 onto the stack
        let OP_6            = 0x56uy
        /// Push 7 onto the stack
        let OP_7            = 0x57uy
        /// Push 8 onto the stack
        let OP_8            = 0x58uy
        /// Push 9 onto the stack
        let OP_9            = 0x59uy
        /// Push 10 onto the stack
        let OP_10           = 0x5auy
        /// Push 11 onto the stack
        let OP_11           = 0x5buy
        /// Push 12 onto the stack
        let OP_12           = 0x5cuy
        /// Push 13 onto the stack
        let OP_13           = 0x5duy
        /// Push 14 onto the stack
        let OP_14           = 0x5euy
        /// Push 15 onto the stack
        let OP_15           = 0x5fuy
        /// Push 16 onto the stack
        let OP_16           = 0x60uy


        // Control flow

        /// Does nothing
        let OP_NOP          = 0x61uy
        /// Halt - Invalid transaction unless found in an unexecuted OP_IF branch
        let OP_VER          = 0x62uy
        /// If the top stack value is not 0, the statements are executed. The top stack value is removed
        let OP_IF           = 0x63uy
        /// If the top stack value is 0, the statements are executed. The top stack value is removed
        let OP_NOTIF        = 0x64uy
        /// Halt - Invalid transaction
        let OP_VERIF        = 0x65uy
        /// Halt - Invalid transaction
        let OP_VERNOTIF     = 0x66uy
        /// Execute only if the previous statements were not executed
        let OP_ELSE         = 0x67uy
        /// Ends an if/else block
        let OP_ENDIF        = 0x68uy
        /// Marks transaction as invalid if top stack value is not true. True is removed, but false is not
        let OP_VERIFY       = 0x69uy
        /// Marks transaction as invalid
        let OP_RETURN       = 0x6auy

        // Stack operations

        /// Puts the input onto the top of the alt stack. Removes it from the main stack
        let OP_TOALTSTACK   = 0x6buy
        /// Puts the input onto the top of the main stack. Removes it from the alt stack
        let OP_FROMALTSTACK = 0x6cuy
        /// Pop two stack items
        let OP_2DROP        = 0x6duy
        /// Duplicate the top two stack items
        let OP_2DUP         = 0x6euy
        /// Duplicate the top three stack items
        let OP_3DUP         = 0x6fuy
        /// Copy the third and fourth stack items to the top
        let OP_2OVER        = 0x70uy
        /// Move the fifth and sixth stack items to the top
        let OP_2ROT         = 0x71uy
        /// Swap the top two pairs of items
        let OP_2SWAP        = 0x72uy
        /// If the top stack value is not 0, duplicate it
        let OP_IFDUP        = 0x73uy
        /// Puts the number of stack items onto the stack
        let OP_DEPTH        = 0x74uy
        /// Removes the top stack item
        let OP_DROP         = 0x75uy
        /// Duplicates the top stack item
        let OP_DUP          = 0x76uy
        /// Removes the second-to-top stack item
        let OP_NIP          = 0x77uy
        /// Copies the second-to-top stack item to the top
        let OP_OVER         = 0x78uy
        /// The item n back in the stack is copied to the top
        let OP_PICK         = 0x79uy
        /// The item n back in the stack is moved to the top
        let OP_ROLL         = 0x7auy
        /// The top three items on the stack are rotated to the left
        let OP_ROT          = 0x7buy
        /// The top three items on the stack are swapped
        let OP_SWAP         = 0x7cuy
        /// The item at the top of the stack is copied and inserted before the second-to-top item
        let OP_TUCK         = 0x7duy

        // String splice operations

        /// Concatenates two strings (disabled)
        let OP_CAT          = 0x7euy
        /// Returns a section of a string (disabled)
        let OP_SUBSTR       = 0x7fuy
        /// Keeps only characters left of the specified point in a string (disabled)
        let OP_LEFT         = 0x80uy
        /// Keeps only characters right of the specified point in a string (disabled)
        let OP_RIGHT        = 0x81uy
        /// Pushes the string length of the top element of the stack
        let OP_SIZE         = 0x82uy

        // Binary arithmetic and conditionals

        /// Flip the bits of the top item (disabled)
        let OP_INVERT       = 0x83uy
        /// Boolean and of the top two items (disabled)
        let OP_AND          = 0x84uy
        /// Boolean or of the top two items (disabled)
        let OP_OR           = 0x85uy
        /// Boolean xor of the top two items (disabled)
        let OP_XOR          = 0x86uy
        /// push true if the top two items are equal, else push false
        let OP_EQUAL        = 0x87uy
        /// Same as OP_EQUAL, but runs OP_VERIFY after to halt if not true
        let OP_EQUALVERIFY  = 0x88uy
        /// Halt - invalid transaction unless found in an unexecuted OP_IF branch
        let OP_RESERVED1    = 0x89uy
        /// Halt - invalid transaction unless found in an unexecuted OP_IF branch
        let OP_RESERVED2    = 0x8auy

        // Numeric

        /// 1 is added to the top element of the stack
        let OP_1ADD                 = 0x8buy
        /// 1 is subtracted from the top element of the stack
        let OP_1SUB                 = 0x8cuy
        /// multiply top item by 2 (disabled)
        let OP_2MUL                 = 0x8duy
        /// divide top item by 2 (disabled)
        let OP_2DIV                 = 0x8euy
        /// Negate the top item of the stack
        let OP_NEGATE               = 0x8fuy
        /// The sign of the top item is made positive
        let OP_ABS                  = 0x90uy
        /// If the top item is 0 or 1, it is flipped. Otherwise the output will be 0
        let OP_NOT                  = 0x91uy
        /// If top item is 0 return 0, otherwise return 1
        let OP_0NOTEQUAL            = 0x92uy
        /// Pop top two items, add them, and push the result
        let OP_ADD                  = 0x93uy
        /// Pop top two items, subtract the first from the second, and push the result
        let OP_SUB                  = 0x94uy
        /// Pop top two items, multiply them, and push the result (disabled)
        let OP_MUL                  = 0x95uy
        /// Pop top two items, divide the second by the first, and push the result (disabled)
        let OP_DIV                  = 0x96uy
        /// Pop top two items, divide the second by the first, and push the remainder (disabled)
        let OP_MOD                  = 0x97uy
        /// shift second item left by first item number of bits (disabled)
        let OP_LSHIFT               = 0x98uy
        /// shift second item right by first item number of bits (disabled)
        let OP_RSHIFT               = 0x99uy
        /// boolean AND of top two items
        let OP_BOOLAND              = 0x9auy
        /// boolean OR of top two items
        let OP_BOOLOR               = 0x9buy
        /// Returns 1 if the numbers are equal, 0 otherwise
        let OP_NUMEQUAL             = 0x9cuy
        /// Same as OP_NUMEQUAL, but runs OP_VERIFY after to halt if not true
        let OP_NUMEQUALVERIFY       = 0x9duy
        /// Returns 1 if the numbers are not equal, 0 otherwise
        let OP_NUMNOTEQUAL          = 0x9euy
        /// Returns 1 if the second item is less than the top item, 0 otherwise
        let OP_LESSTHAN             = 0x9fuy
        /// Returns 1 if the second item is greater than the top item, 0 otherwise
        let OP_GREATERTHAN          = 0xa0uy
        /// Returns 1 if the second item is less than or equal to the top item, 0 otherwise
        let OP_LESSTHANOREQUAL      = 0xa1uy
        /// Returns 1 if the second item is greater than or equal to the top item, 0 otherwise
        let OP_GREATERTHANOREQUAL   = 0xa2uy
        /// Returns the smaller of the two top items
        let OP_MIN                  = 0xa3uy
        /// Returns the larger of the two top items
        let OP_MAX                  = 0xa4uy
        /// Returns 1 if the third item is between the second and first items, 0 otherwise
        let OP_WITHIN               = 0xa5uy

        // Cryptographic and Hashing

        /// The input is hashed using RIPEMD-160
        let OP_RIPEMD160            = 0xa6uy
        /// The input is hashed using SHA-1
        let OP_SHA1                 = 0xa7uy
        /// The input is hashed using SHA-256
        let OP_SHA256               = 0xa8uy
        /// The input is hashed twice: first with SHA-256 and then with RIPEMD-160
        let OP_HASH160              = 0xa9uy
        /// The input is hashed two times with SHA-256
        let OP_HASH256              = 0xaauy
        /// Mark the beginning of a signature script
        let OP_CODESEPARATOR        = 0xabuy
        /// Pop a public key and signature and validate the signature for the transaction's hash; return true if matching
        let OP_CHECKSIG             = 0xacuy
        /// Same as OP_CHECKSIG, but runs OP_VERIFY after to halt if not true
        let OP_CHECKSIGVERIFY       = 0xaduy
        /// Pop a public key, hash it, and compare to the signature hash; return true if matching
        /// bug in the original Bitcoin implementation means one less than intended item is popped off the stack
        /// https://bitcointalk.org/index.php?topic=260595.0
        /// Prefix with OP_NOP to work around
        let OP_CHECKMULTISIG        = 0xaeuy
        /// Same as OP_CHECKMULTISIG, but runs OP_VERIFY after to halt if not true
        let OP_CHECKMULTISIGVERIFY  = 0xafuy

        // Nonoperators

        /// Does nothing
        let OP_NOP1                 = 0xb0uy
        /// Does nothing
        let OP_NOP2                 = 0xb1uy
        /// Does nothing
        let OP_NOP3                 = 0xb2uy
        /// Does nothing
        let OP_NOP4                 = 0xb3uy
        /// Does nothing
        let OP_NOP5                 = 0xb4uy
        /// Does nothing
        let OP_NOP6                 = 0xb5uy
        /// Does nothing
        let OP_NOP7                 = 0xb6uy
        /// Does nothing
        let OP_NOP8                 = 0xb7uy
        /// Does nothing
        let OP_NOP9                 = 0xb8uy
        /// Does nothing
        let OP_NOP10                = 0xb9uy

        // Reserved OP codes for internal use by the parser

        let OP_SMALLDATA            = 0xf9uy
        let OP_SMALLINTEGER         = 0xfauy
        let OP_PUBKEYS              = 0xfbuy
        let OP_PUBKEYHASH           = 0xfcuy
        let OP_PUBKEY               = 0xfduy
        let OP_INVALIDOPCODE        = 0xfeuy






