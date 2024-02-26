using System.Numerics;
using Nethereum.Signer;
using Nethereum.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PWRCS;
using PWRCS.Models;

var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
   
try{
     var list = await sdk.GetBlockByNumber(5);
     Console.WriteLine(list.ToString());
     var wallet = new PwrWallet(sdk,"d5cebdc74ba9c0746da66e4ee13a2bae73c3e24218959afdb6e2a4f964599b66");
     
        var r = await wallet.TransferPWR("0xf6fe6a14b3aac06c2c102cf5f028df35157f9770", 1000000);
        Console.WriteLine(r.Success);
        Console.WriteLine(r.Error);
        Console.WriteLine(r.TxnHash);
     
   }catch(Exception e){
       
        Console.WriteLine(e.ToString());
}

