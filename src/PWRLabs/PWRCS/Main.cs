using PWRCS;
using PWRCS.Models;

var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
try{
        List<VmDataTxn> vmDataTxns = await sdk.GetVmDataTxns(1, 10, 10023);
       foreach(var t in vmDataTxns){
        Console.WriteLine(t.Hash);
       }
}catch(Exception e){
        Console.WriteLine(e.Message);
}