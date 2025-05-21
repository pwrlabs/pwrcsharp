using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pqc.Crypto.Falcon;
using Org.BouncyCastle.Security;

namespace PWR.Utils;

public class Falcon
{
    private static readonly FalconParameters Params512 = FalconParameters.falcon_512;
    private static readonly FalconParameters Params1024 = FalconParameters.falcon_1024;

    static Falcon()
    {
        // No need to add provider in C# version
    }

    /// <summary>
    /// Generate a deterministic Falcon-512 key pair from a seed
    /// </summary>
    /// <param name="seed">The seed bytes to derive the key pair from</param>
    /// <returns>The generated key pair</returns>
    public static AsymmetricCipherKeyPair GenerateKeyPair512FromSeed(byte[] seed)
    {
        // Create a deterministic pseudo-random generator from the seed
        var deterministicRandom = new DeterministicSecureRandom(seed);
        byte[] bytes = new byte[48];
        deterministicRandom.NextBytes(bytes);

        deterministicRandom = new DeterministicSecureRandom(seed);

        var keyPairGen = new FalconKeyPairGenerator();
        var keyGenParams = new FalconKeyGenerationParameters(deterministicRandom, Params512);
        keyPairGen.Init(keyGenParams);
        return keyPairGen.GenerateKeyPair();
    }

    public static AsymmetricCipherKeyPair GenerateKeyPair512()
    {
        var keyPairGen = new FalconKeyPairGenerator();
        var keyGenParams = new FalconKeyGenerationParameters(new SecureRandom(), Params512);
        keyPairGen.Init(keyGenParams);
        return keyPairGen.GenerateKeyPair();
    }

    

    public static AsymmetricCipherKeyPair GenerateKeyPair1024()
    {
        var keyPairGen = new FalconKeyPairGenerator();
        var keyGenParams = new FalconKeyGenerationParameters(new SecureRandom(), Params1024);
        keyPairGen.Init(keyGenParams);
        return keyPairGen.GenerateKeyPair();
    }

    public static byte[] Sign(byte[] message, AsymmetricCipherKeyPair keyPair)
    {
        var signer = new FalconSigner();
        var privateKey = (FalconPrivateKeyParameters)keyPair.Private;
        signer.Init(true, privateKey);
        return signer.GenerateSignature(message);
    }

    public static bool Verify512(byte[] message, byte[] signature, byte[] publicKey)
    {
        try
        {
            // Handle the case where the public key includes the 0x09 prefix
            byte[] processedKey = publicKey;
            if (publicKey.Length == 897 && publicKey[0] == 0x09)
            {
                processedKey = new byte[publicKey.Length - 1];
                Array.Copy(publicKey, 1, processedKey, 0, processedKey.Length);
            }

            var publicKeyParams = new FalconPublicKeyParameters(Params512, processedKey);
            var signer = new FalconSigner();
            signer.Init(false, publicKeyParams);
            return signer.VerifySignature(message, signature);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
