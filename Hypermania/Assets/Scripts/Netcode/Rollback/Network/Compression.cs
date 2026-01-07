using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Netcode.Rollback.Network
{
    public static class Compression
    {
        private const int MAX_SCRATCH_BYTES = 256 * 1024;

        [ThreadStatic]
        private static byte[] _scratch;
        private static byte[] Scratch
        {
            get
            {
                if (_scratch == null)
                    _scratch = new byte[MAX_SCRATCH_BYTES];
                return _scratch;
            }
        }

        public static byte[] Encode(in InputBytes refInput, IEnumerable<InputBytes> pendingInput)
        {
            if (pendingInput == null)
                throw new ArgumentNullException(nameof(pendingInput));
            Assert.AreNotEqual(refInput.Bytes.Length, 0, "reference input cannot be empty");

            byte[] scratch = Scratch;
            int outPtr = 0;

            bool hasRun = false;
            byte runValue = 0;
            int runCount = 0;

            void FlushRun()
            {
                if (!hasRun)
                    return;

                if (outPtr + 2 > scratch.Length)
                    throw new InvalidOperationException($"Compression scratch overflow (>{MAX_SCRATCH_BYTES} bytes).");

                scratch[outPtr++] = (byte)runCount;
                scratch[outPtr++] = runValue;

                hasRun = false;
                runCount = 0;
                runValue = 0;
            }

            foreach (InputBytes input in pendingInput)
            {
                Assert.AreEqual(
                    refInput.Bytes.Length,
                    input.Bytes.Length,
                    $"input ({input.Bytes.Length} bytes) must be same length as the reference input ({refInput.Bytes.Length} bytes)"
                );

                for (int i = 0; i < refInput.Bytes.Length; i++)
                {
                    byte delta = (byte)(refInput.Bytes[i] ^ input.Bytes[i]);

                    if (!hasRun)
                    {
                        hasRun = true;
                        runValue = delta;
                        runCount = 1;
                    }
                    else if (delta == runValue && runCount < 255)
                    {
                        runCount++;
                    }
                    else
                    {
                        FlushRun();
                        hasRun = true;
                        runValue = delta;
                        runCount = 1;
                    }
                }
            }

            FlushRun();

            byte[] res = new byte[outPtr];
            Buffer.BlockCopy(scratch, 0, res, 0, outPtr);
            return res;
        }

        public static byte[][] Decode(in InputBytes refInput, ReadOnlySpan<byte> data)
        {
            Assert.AreNotEqual(refInput.Bytes.Length, 0, "reference input cannot be empty");

            byte[] scratch = Scratch;
            int decodedLen = RleDecodeToScratch(data, scratch);

            Assert.AreEqual(
                decodedLen % refInput.Bytes.Length,
                0,
                "decoded data length must be a multiple of reference length"
            );

            int count = decodedLen / refInput.Bytes.Length;
            byte[][] res = new byte[count][];

            int srcBase = 0;
            for (int inp = 0; inp < count; inp++)
            {
                byte[] outInp = new byte[refInput.Bytes.Length];
                for (int i = 0; i < refInput.Bytes.Length; i++)
                {
                    outInp[i] = (byte)(refInput.Bytes[i] ^ scratch[srcBase + i]);
                }

                res[inp] = outInp;
                srcBase += refInput.Bytes.Length;
            }

            return res;
        }

        private static int RleDecodeToScratch(ReadOnlySpan<byte> rle, byte[] scratch)
        {
            Assert.AreEqual(rle.Length % 2, 0, "RLE data length must be even (count,value pairs)");

            int outPtr = 0;

            for (int i = 0; i < rle.Length; i += 2)
            {
                byte count = rle[i];
                byte value = rle[i + 1];

                Assert.IsTrue(count != 0, "RLE run count cannot be 0");

                if (outPtr + count > scratch.Length)
                    throw new InvalidOperationException(
                        $"Decompression scratch overflow (>{MAX_SCRATCH_BYTES} bytes)."
                    );

                for (int k = 0; k < count; k++)
                    scratch[outPtr++] = value;
            }

            return outPtr;
        }
    }
}
