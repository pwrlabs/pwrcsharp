using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PWR;
using PWR.Models;

namespace ExampleApp;

class App
{
    private static readonly PwrApiSdk sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
    private static PwrWallet wallet;
    private const long vidaId = 848796;
    private const ulong startingBlockNumber = 241741;

    public static async Task Run()
    {
        await CreateOrLoadWalletAsync();
        ReadDataFromPwrChain();
        await ReadUserInputAsync();
    }

    static async Task CreateOrLoadWalletAsync()
    {
        const string password = "123";
        const string walletFile = "wallet.dat";

        try
        {
            if (File.Exists(walletFile))
            {
                wallet = PwrWallet.LoadWallet(walletFile, password);
            }
            else
            {
                wallet = new PwrWallet();
                wallet.StoreWallet(walletFile, password);
            }
        }
        catch
        {
            wallet = new PwrWallet();
            wallet.StoreWallet(walletFile, password);
        }

        Console.WriteLine($"Wallet address: {wallet.GetAddress()}");
        Console.WriteLine($"Balance: {await wallet.GetBalance()}");
    }

    static void ReadDataFromPwrChain()
    {
        var subscription = sdk.SubscribeToIvaTransactions(vidaId, startingBlockNumber, transaction =>
        {
            var data = transaction.Data;

            // Remove 0x prefix if present
            if (data.StartsWith("0x"))
                data = data[2..];

            try
            {
                var dataBytes = Convert.FromHexString(data);
                var dataStr = Encoding.UTF8.GetString(dataBytes);
                using var doc = JsonDocument.Parse(dataStr);
                var root = doc.RootElement;

                if (root.TryGetProperty("action", out var actionElement))
                {
                    var action = actionElement.GetString();
                    var sender = transaction.Sender;

                    switch (action?.ToLower())
                    {
                        case "sendmessage":
                            if (root.TryGetProperty("message", out var messageElement))
                            {
                                var message = messageElement.GetString();
                                Console.WriteLine($"Message from {sender}: {message}");
                            }
                            break;
                        case "sendgift":
                            // Handle sendGift logic
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing transaction: {ex.Message}");
            }
        });

        subscription.Pause();

        ulong num = subscription.GetLatestCheckedBlock();
        Console.WriteLine($"Block number: {num}");

        subscription.Resume();
    }

    static async Task ReadUserInputAsync()
    {
        while (true)
        {
            var input = Console.ReadLine();
            if (string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase))
                break;

            var jsonData = new
            {
                action = "sendMessage",
                message = input
            };

            var jsonString = JsonSerializer.Serialize(jsonData);
            var dataBytes = Encoding.UTF8.GetBytes(jsonString);

            var response = await wallet.SendVMData(vidaId, dataBytes);
            if (response.Success)
            {
                Console.WriteLine($"Message sent successfully. Txn hash: {response.TxnHash}");
            }
            else
            {
                Console.WriteLine($"Failed to send message: {response.Error}");
            }
        }
    }
}
