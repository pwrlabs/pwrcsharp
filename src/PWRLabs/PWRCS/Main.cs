using System.Numerics;
using System.Text;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using Nethereum.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PWRCS;
using PWRCS.Models;
var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
await sdk.GetChainId();
try{
TxnForGuardianApproval r = await sdk.IsTransactionValidForGuardianApproval("0x98e1bccd239a09ca56f395d4ddd9a8335fbaa58d04069e51813c42ec03ae1f06");
Console.WriteLine("Transfer PWR success: " + r.Valid);
        Console.WriteLine("Transfer PWR txn hash: " + r.GuardianAddress);
        Console.WriteLine("Transfer PWR error: " + r.ErrorMessage);
}catch(Exception e){
     Console.WriteLine(e.ToString());
}
