using System.Numerics;
using Nethereum.Signer;
using Nethereum.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PWRCS;
using PWRCS.Models;

var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
   
try{
     var wallet = new PwrWallet(sdk,"6c44cce020953500c6ea6c15ca812a99762e989c8f61908165a1064e1f95bf95");
     var response = await wallet.TransferPWR("0xf6fe6a14b3aac06c2c102cf5f028df35157f9770",1000000000);
     Console.WriteLine(response.Success);
     Console.WriteLine(response.Error);
   }catch(Exception e){
       
        Console.WriteLine(e.ToString());
}

