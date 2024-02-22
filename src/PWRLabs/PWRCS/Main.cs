using PWRCS;
using PWRCS.Models;

var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
try{
        List<Validator> vmDataTxns = await sdk.GetAllValidators();
        
       foreach(var t in vmDataTxns){
        Console.WriteLine(t.Address);
       }
}catch(Exception e){
        Console.WriteLine(e.Message);
}