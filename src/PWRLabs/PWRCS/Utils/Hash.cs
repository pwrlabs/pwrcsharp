using System;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;

namespace PWRCS
{
    public static class Hash
    {
        public static byte[] HashMessage(byte[] input, string algorithm)
        {
            try
            {
                using (var digest = HashAlgorithm.Create(algorithm.ToUpper()))
                {
                    if (digest == null)
                        throw new NotSupportedException($"Algorithm {algorithm} is not supported.");

                    return digest.ComputeHash(input);
                }
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException($"Couldn't compute hash with algorithm {algorithm}.", ex);
            }
        }

        public static byte[] Sha3(byte[] input, int offset, int length)
        {
            var kecc = new KeccakDigest(256);
            kecc.BlockUpdate(input, offset, length);
            byte[] output = new byte[kecc.GetDigestSize()];
            kecc.DoFinal(output, 0);
            return output;
        }

        public static byte[] Sha3(byte[] input)
        {
            return Sha3(input, 0, input.Length);
        }

        public static byte[] Sha256(byte[] input)
        {
            using (var digest = SHA256.Create())
            {
                return digest.ComputeHash(input);
            }
        }

        public static byte[] HmacSha512(byte[] key, byte[] input)
        {
            var hmac = new HMac(new Sha512Digest());
            hmac.Init(new KeyParameter(key));
            hmac.BlockUpdate(input, 0, input.Length);
            byte[] output = new byte[hmac.GetMacSize()];
            hmac.DoFinal(output, 0);
            return output;
        }

        public static byte[] Sha256Hash160(byte[] input)
        {
            byte[] sha256 = Sha256(input);
            var digest = new RipeMD160Digest();
            digest.BlockUpdate(sha256, 0, sha256.Length);
            byte[] output = new byte[digest.GetDigestSize()];
            digest.DoFinal(output, 0);
            return output;
        }

        public static byte[] Blake2b256(byte[] input)
        {
            var blake2b = new Blake2bDigest(256);
            blake2b.BlockUpdate(input, 0, input.Length);
            byte[] output = new byte[blake2b.GetDigestSize()];
            blake2b.DoFinal(output, 0);
            return output;
        }
    }
}
