using System.Numerics;
using Nethereum.Signer;
using Nethereum.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PWRCS;
using PWRCS.Models;

var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");

try{
     Block block = await  sdk.GetBlockByNumber(5);
     Console.WriteLine(block.ToString());
     BigInteger privateKey = BigInteger.Parse("19025338099182849188500822369817708178555441129124871592504836170414925188857");
     EthECKey key = PwrWallet.ConvertToEthECKey(privateKey);
      PwrWallet wallet = new PwrWallet(sdk,key);
     uint nonce = await wallet.GetNonce();
     Console.WriteLine("hello test");
     Console.WriteLine(nonce);
     Console.WriteLine(wallet.PublicAddress);
     var response = await sdk.GetBalanceOfAddress("0xb182f3Fa01FD09E2A17eA858114FD6aa735ac925");
     Console.WriteLine(response.Data);

     BigDecimal de = await sdk.GetShareValue("0xf6fe6a14b3aac06c2c102cf5f028df35157f9770");
     Console.WriteLine(de.ToString());
     Console.WriteLine(BigDecimal.Parse(""));
     
     
}catch(Exception e){
        Console.WriteLine(e.Message);
}

