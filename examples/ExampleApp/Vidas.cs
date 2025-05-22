using System;
using System.Text;
using Newtonsoft.Json.Linq;
using PWR;
using PWR.Models;
using PWR.Utils;

namespace ExampleApp;

class Vidas
{
    public static async Task Run()
    {
        var rpc = new RPC("https://pwrrpc.pwrlabs.io/");
        ulong vidaId = 1; // Replace with your VIDA's ID

        // Since our VIDA is global chat room and we don't care about historical messages,
        // we will start reading transactions startng from the latest PWR Chain block
        ulong startingBlock = await rpc.GetLatestBlockNumber();

        VidaTransactionSubscription subscription = rpc.SubscribeToVidaTransactions(vidaId, startingBlock, (transaction) => {
            // Get the address of the transaction sender
            string sender = transaction.Sender;
            // Get the data sent in the transaction (In Hex Format)
            string data = transaction.Data;

            // Convert data string to bytes
            if (data.StartsWith("0x")) data = data.Substring(2);
            byte[] dataBytes = PWR.Utils.Extensions.HexStringToByteArray(data);
        
            var jsonObject = JObject.Parse(Encoding.UTF8.GetString(dataBytes));
            string? action = jsonObject["action"]?.ToString();
            
            // Check the action and execute the necessary code
            if (string.Equals(action, "send-message-v1", StringComparison.OrdinalIgnoreCase)) {
                string? message = jsonObject["message"]?.ToString();
                if (message != null)
                {
                    Console.WriteLine($"Message from 0x{sender}: {message}");
                }
            }
        });

        subscription.Pause();
        subscription.Resume();
        // subscription.Stop();

        Console.WriteLine($"Latest checked blocked: {subscription.GetLatestCheckedBlock()}");
    }
}