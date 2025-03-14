using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PWRCS;
using PWRCS.Models;

class Program
{
    static async Task Main()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");

        ulong startingBlock = await sdk.GetLatestBlockNumber();
        sdk.SubscribeToIvaTransactions(1234, startingBlock, (transaction) => {
            string sender = transaction.Sender;
            string data = transaction.Data;

            // Process the hex data
            if (data.StartsWith("0x")) data = data.Substring(2);
            byte[] dataBytes = data.HexStringToByteArray();
            
            var jsonObject = JObject.Parse(Encoding.UTF8.GetString(dataBytes));
            string action = jsonObject["action"]?.ToString();
            
            if (string.Equals(action, "send-message-v1", StringComparison.OrdinalIgnoreCase)) {
                string message = jsonObject["message"]?.ToString();
                Console.WriteLine($"Message from {sender}: {message}");
            }
        });
        Console.WriteLine("Listening for transactions...");
        Console.ReadLine();
    }
}