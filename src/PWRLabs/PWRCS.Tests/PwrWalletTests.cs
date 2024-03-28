using System.Text;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using PWRCS.Models;

namespace PWRCS.Tests;

public class PwrWalletTests
{
    [Fact]
    public void TestSignature()
    {
        var key = new EthECKey("ebf3a1c0654c8dd7700070b5a280d34d7ea3a1dc9d781dfc3af49443ffd1cc65");
        
        var s1 = Signer.SignMessage(key, "Hello!");
        Assert.Equal("de956f8dc5b68e8195929e84c885f542a41cb635b3d2eb6dca92f1d983af70ef00f3250f7f012edf8d99ce6da4c03a4b06557b5df5e07bbd085260f4536a13ee1c", s1);
        
        var s2 = Signer.SignMessage(key, "Hello World!!");
        Assert.Equal("34899d6189e03146312f1a330de7c588202b9f515ac09425aa345d4fc6fc86e11c9d6fe7ae128e7534d0b17b38e81f37a3f0e45880fb44758eb459a96a064b4a1b", s2);
    }
    
    [Fact]
    public async Task TestTransfer()
    {
        var wallet = new PwrWallet(new PwrApiSdk("https://pwrrpc.pwrlabs.io/"), "5051f367aa1dc81294b711a716cc08c096ad61783c89636081a8ea92828a0f58");

        var r = await wallet.TransferPWR("0x8953f1c3B53Bd9739F78dc8B0CD5DB9686C40b09", 1);
        Assert.True(r.Success);
    }
    
    [Fact]
    public async Task TestNonce()
    {
        var wallet = new PwrWallet(new PwrApiSdk("https://pwrrpc.pwrlabs.io/"));
        var nonce = await wallet.GetNonce();
        Assert.Equal(0UL, nonce);
    }  
    
    [Fact]
    public async Task TestGetBalance()
    {
        var wallet = new PwrWallet(new PwrApiSdk("https://pwrrpc.pwrlabs.io/"));
        var bal = await wallet.GetBalance();
        Assert.Equal(0UL, bal);
    }    
    
    [Fact]
    public async Task TestSendVmDataTxn()
    {
        var wallet = new PwrWallet(new PwrApiSdk("https://pwrrpc.pwrlabs.io/"), "5051f367aa1dc81294b711a716cc08c096ad61783c89636081a8ea92828a0f58");
        var r = await wallet.SendVmDataTxn(0, new byte[]{ 1, 2,3});
        Assert.True(r.Success);
    }   
    
    [Fact]
    public async Task TestDelegate()
    {
        var wallet = new PwrWallet(new PwrApiSdk("https://pwrrpc.pwrlabs.io/"), "5051f367aa1dc81294b711a716cc08c096ad61783c89636081a8ea92828a0f58");
        var r = await wallet.Delegate("0x61Bd8fc1e30526Aaf1C4706Ada595d6d236d9883", 1000000000);
        Assert.True(r.Success);
    }    
    
    [Fact]
    public async Task TestWithdraw()
    {
       
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var wallet = new PwrWallet(sdk,"5051f367aa1dc81294b711a716cc08c096ad61783c89636081a8ea92828a0f58");
        while (await sdk.GetDelegatedPWR(wallet.PublicAddress, "0x61Bd8fc1e30526Aaf1C4706Ada595d6d236d9883") == 0)
        {
            await Task.Delay(1000);
        }
        var r = await wallet.WithDraw("0x61Bd8fc1e30526Aaf1C4706Ada595d6d236d9883", 10);
        Assert.True(r.Success);
    }

     [Fact]
    public async Task TestWithdrawPWR()
    {

        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var wallet = new PwrWallet(sdk,"5051f367aa1dc81294b711a716cc08c096ad61783c89636081a8ea92828a0f58");
        while (await sdk.GetDelegatedPWR(wallet.PublicAddress, "0x61Bd8fc1e30526Aaf1C4706Ada595d6d236d9883") == 0)
        {
            await Task.Delay(1000);
        }

        var r = await wallet.WithDrawPWR("0x61Bd8fc1e30526Aaf1C4706Ada595d6d236d9883", 10000000);
        Assert.True(r.Success);
    }
    
    [Fact]
    public async Task TestClaimVmId()
    {
        var wallet = new PwrWallet(new PwrApiSdk("https://pwrrpc.pwrlabs.io/"), "5051f367aa1dc81294b711a716cc08c096ad61783c89636081a8ea92828a0f58");
        var r = await wallet.ClaimVmId(1);
        Assert.True(r.Success);
    }

     [Fact]
    public async Task TestSendPayableVmDataTxn()
    {   
        ulong vmId = 897435;
        var wallet = new PwrWallet(new PwrApiSdk("https://pwrrpc.pwrlabs.io/"), "5051f367aa1dc81294b711a716cc08c096ad61783c89636081a8ea92828a0f58");
        var r = await wallet.SendPayableVmDataTxn(vmId, 10, Encoding.UTF8.GetBytes("Hello World"), await wallet.GetNonce());
        Assert.True(r.Success);
    }

    [Fact]
    public async Task TestSetGuardian()
    {
        var wallet = new PwrWallet(new PwrApiSdk("https://pwrrpc.pwrlabs.io/"), "5051f367aa1dc81294b711a716cc08c096ad61783c89636081a8ea92828a0f58");
       var r = await wallet.SetGuardian("0x61Bd8fc1e30526Aaf1C4706Ada595d6d236d9883", (ulong)DateTimeOffset.Now.ToUnixTimeSeconds() + 100000);
        Assert.True(r.Success);
    }

     [Fact]
    public async Task TestSendGuardianWrappedTransaction()
    {
        PwrApiSdk sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var wallet = new PwrWallet(sdk, "5051f367aa1dc81294b711a716cc08c096ad61783c89636081a8ea92828a0f58");
        byte[] transferTxn = await wallet.GetSignedTransferPWRTxn("0x61Bd8fc1e30526Aaf1C4706Ada595d6d236d9883", 1000,await wallet.GetNonce());
        PwrWallet guardian = new PwrWallet(sdk,"03a5240936d67dc18dca348e793010a14c5eba86a73d0c9e45764681295a73df");
        var r = await guardian.SendGuardianWrappedTransaction(transferTxn);
        Assert.True(r.Success);

        byte[] removeGuardianTxn = await  wallet.GetSignedRemoveGuardianTxn();
        var response = await guardian.SendGuardianWrappedTransaction(removeGuardianTxn);
        
        Assert.True(response.Success);
    }
    
}