using System.Text;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using Nethereum.Util;

namespace PWRPY;

public class Signer
{
    public static string SignMessage(EthECKey key, string message)
    {
        return SignMessage(key, Encoding.UTF8.GetBytes(message)).ToHex();
    }
    
    public static byte[] SignMessage(EthECKey key, byte[] message)
    {
        var sha3Keccack = new Sha3Keccack();
        byte[] messageHash = sha3Keccack.CalculateHash(message);
        
        var signer = new EthereumMessageSigner();
        var signatureBytes = signer.SignAndCalculateV(messageHash, key);
        
        var signatureString = signatureBytes.R.ToHex().PadLeft(64, '0') +
                              signatureBytes.S.ToHex().PadLeft(64, '0') +
                              signatureBytes.V.ToHex();
        
        return signatureString.HexToByteArray();
    }
}