using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PWRCS;
using PWRCS.Models;

var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");

try{
     
       string data = await sdk.TestRequest("https://pwrrpc.pwrlabs.io/chainId/");
       JObject responseData = JsonConvert.DeserializeObject<JObject>(data);

       var chainId = responseData["chainId"]?.Value<byte>() ?? unchecked((byte)-1);
       Console.WriteLine("Chaind id : " + chainId);
}catch(Exception e){
        Console.WriteLine(e.Message);
}

