using System;

namespace DaftAppleGames.Editor
{
    /// <summary>
    ///     Computes the MD4 digest Unity uses to derive MonoScript local IDs for managed assembly types.
    /// </summary>
    internal static class ReferenceAssetImporterMd4
    {
        public static byte[] ComputeHash(byte[] input)
        {
            int paddedLength = ((input.Length + 8) / 64 + 1) * 64;
            byte[] paddedInput = new byte[paddedLength];
            Buffer.BlockCopy(input, 0, paddedInput, 0, input.Length);
            paddedInput[input.Length] = 0x80;
            ulong bitLength = (ulong)input.Length * 8;
            for (int byteIndex = 0; byteIndex < 8; byteIndex++)
                paddedInput[paddedLength - 8 + byteIndex] = (byte)(bitLength >> byteIndex * 8);

            uint a = 0x67452301;
            uint b = 0xefcdab89;
            uint c = 0x98badcfe;
            uint d = 0x10325476;
            uint[] words = new uint[16];

            for (int blockOffset = 0; blockOffset < paddedInput.Length; blockOffset += 64)
            {
                ReadWords(paddedInput, blockOffset, words);

                uint originalA = a;
                uint originalB = b;
                uint originalC = c;
                uint originalD = d;

                ApplyRounds(ref a, ref b, ref c, ref d, words);

                unchecked
                {
                    a += originalA;
                    b += originalB;
                    c += originalC;
                    d += originalD;
                }
            }

            byte[] hash = new byte[16];
            WriteUInt32(hash, 0, a);
            WriteUInt32(hash, 4, b);
            WriteUInt32(hash, 8, c);
            WriteUInt32(hash, 12, d);
            return hash;
        }

        private static void ApplyRounds(ref uint a, ref uint b, ref uint c, ref uint d, uint[] words)
        {
            Round1(ref a, b, c, d, words[0], 3);
            Round1(ref d, a, b, c, words[1], 7);
            Round1(ref c, d, a, b, words[2], 11);
            Round1(ref b, c, d, a, words[3], 19);
            Round1(ref a, b, c, d, words[4], 3);
            Round1(ref d, a, b, c, words[5], 7);
            Round1(ref c, d, a, b, words[6], 11);
            Round1(ref b, c, d, a, words[7], 19);
            Round1(ref a, b, c, d, words[8], 3);
            Round1(ref d, a, b, c, words[9], 7);
            Round1(ref c, d, a, b, words[10], 11);
            Round1(ref b, c, d, a, words[11], 19);
            Round1(ref a, b, c, d, words[12], 3);
            Round1(ref d, a, b, c, words[13], 7);
            Round1(ref c, d, a, b, words[14], 11);
            Round1(ref b, c, d, a, words[15], 19);

            Round2(ref a, b, c, d, words[0], 3);
            Round2(ref d, a, b, c, words[4], 5);
            Round2(ref c, d, a, b, words[8], 9);
            Round2(ref b, c, d, a, words[12], 13);
            Round2(ref a, b, c, d, words[1], 3);
            Round2(ref d, a, b, c, words[5], 5);
            Round2(ref c, d, a, b, words[9], 9);
            Round2(ref b, c, d, a, words[13], 13);
            Round2(ref a, b, c, d, words[2], 3);
            Round2(ref d, a, b, c, words[6], 5);
            Round2(ref c, d, a, b, words[10], 9);
            Round2(ref b, c, d, a, words[14], 13);
            Round2(ref a, b, c, d, words[3], 3);
            Round2(ref d, a, b, c, words[7], 5);
            Round2(ref c, d, a, b, words[11], 9);
            Round2(ref b, c, d, a, words[15], 13);

            Round3(ref a, b, c, d, words[0], 3);
            Round3(ref d, a, b, c, words[8], 9);
            Round3(ref c, d, a, b, words[4], 11);
            Round3(ref b, c, d, a, words[12], 15);
            Round3(ref a, b, c, d, words[2], 3);
            Round3(ref d, a, b, c, words[10], 9);
            Round3(ref c, d, a, b, words[6], 11);
            Round3(ref b, c, d, a, words[14], 15);
            Round3(ref a, b, c, d, words[1], 3);
            Round3(ref d, a, b, c, words[9], 9);
            Round3(ref c, d, a, b, words[5], 11);
            Round3(ref b, c, d, a, words[13], 15);
            Round3(ref a, b, c, d, words[3], 3);
            Round3(ref d, a, b, c, words[11], 9);
            Round3(ref c, d, a, b, words[7], 11);
            Round3(ref b, c, d, a, words[15], 15);
        }

        private static void ReadWords(byte[] input, int blockOffset, uint[] words)
        {
            for (int wordIndex = 0; wordIndex < words.Length; wordIndex++)
            {
                int offset = blockOffset + wordIndex * 4;
                words[wordIndex] = (uint)(input[offset] |
                                          input[offset + 1] << 8 |
                                          input[offset + 2] << 16 |
                                          input[offset + 3] << 24);
            }
        }

        private static void Round1(ref uint value, uint b, uint c, uint d, uint word, int shift)
        {
            value = RotateLeft(unchecked(value + ((b & c) | (~b & d)) + word), shift);
        }

        private static void Round2(ref uint value, uint b, uint c, uint d, uint word, int shift)
        {
            value = RotateLeft(
                unchecked(value + ((b & c) | (b & d) | (c & d)) + word + 0x5a827999), shift);
        }

        private static void Round3(ref uint value, uint b, uint c, uint d, uint word, int shift)
        {
            value = RotateLeft(unchecked(value + (b ^ c ^ d) + word + 0x6ed9eba1), shift);
        }

        private static uint RotateLeft(uint value, int shift)
        {
            return value << shift | value >> 32 - shift;
        }

        private static void WriteUInt32(byte[] destination, int offset, uint value)
        {
            destination[offset] = (byte)value;
            destination[offset + 1] = (byte)(value >> 8);
            destination[offset + 2] = (byte)(value >> 16);
            destination[offset + 3] = (byte)(value >> 24);
        }
    }
}
