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
   
try{

     var wallet = new PwrWallet(sdk, "5051f367aa1dc81294b711a716cc08c096ad61783c89636081a8ea92828a0f58");
       uint nonce = await wallet.GetNonce();
        var r = await wallet.TransferPWR("0x8953f1c3B53Bd9739F78dc8B0CD5DB9686C40b09", 1000000000, nonce);
        Console.WriteLine("Transfer PWR success: " + r.Success);
        Console.WriteLine("Transfer PWR txn hash: " + r.TxnHash);
        Console.WriteLine("Transfer PWR error: " + r.Error);
        Console.WriteLine();
        ++nonce;
        r = await wallet.Delegate("0x61Bd8fc1e30526Aaf1C4706Ada595d6d236d9883", 1000000000, nonce);
        Console.WriteLine("Delegate success: " + r.Success);
        Console.WriteLine("Delegate txn hash: " + r.TxnHash);
        Console.WriteLine("Delegate error: " + r.Error);
        Console.WriteLine();

        ++nonce;
        r = await wallet.ClaimVmId(1, nonce);
        Console.WriteLine("Claim VM ID success: " + r.Success);
        Console.WriteLine("Claim VM ID txn hash: " + r.TxnHash);
        Console.WriteLine("Claim VM ID error: " + r.Error);

        ++nonce;
        byte[] dataBytes = System.Text.Encoding.UTF8.GetBytes("Hello World");
        r = await wallet.SendVmDataTxn(1, dataBytes, nonce);
        Console.WriteLine("Send VM Data success: " + r.Success);
        Console.WriteLine("Send VM Data txn hash: " + r.TxnHash);
        Console.WriteLine("Send VM Data error: " + r.Error);

        while (await sdk.GetDelegatedPWR(wallet.PublicAddress, "0x61Bd8fc1e30526Aaf1C4706Ada595d6d236d9883") == 0)
        {
            await Task.Delay(1000);
        }

        // TODO: send this after the delegation has been completed
        ++nonce;
        r = await wallet.WithDraw("0x61Bd8fc1e30526Aaf1C4706Ada595d6d236d9883", 10, nonce);
        Console.WriteLine("Withdraw success: " + r.Success);
        Console.WriteLine("Withdraw txn hash: " + r.TxnHash);
        Console.WriteLine("Withdraw error: " + r.Error);
        Console.WriteLine();

        // TODO: send this after the delegation has been completed
        ++nonce;
        r = await wallet.WithDrawPWR("0x61Bd8fc1e30526Aaf1C4706Ada595d6d236d9883", 10000000, nonce);
        Console.WriteLine("Withdraw PWR success: " + r.Success);
        Console.WriteLine("Withdraw PWR txn hash: " + r.TxnHash);
        Console.WriteLine("Withdraw PWR error: " + r.Error);
   }catch(Exception e){
       
        Console.WriteLine(e.ToString());
}

