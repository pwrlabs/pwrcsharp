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
        var wallet = new PwrWallet(new PwrApiSdk("https://pwrrpc.pwrlabs.io/"), "d5cebdc74ba9c0746da66e4ee13a2bae73c3e24218959afdb6e2a4f964599b66");
        
        var wallet2 = new PwrWallet(new PwrApiSdk("https://pwrrpc.pwrlabs.io/"));
        var r = await wallet.TransferPWR("0xf6fe6a14b3aac06c2c102cf5f028df35157f9770", 1);
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
        var nonce = await wallet.GetBalance();
        Assert.Equal(0UL, nonce);
    }    
    
    [Fact]
    public async Task TestSendVmDataTxn()
    {
        var wallet = new PwrWallet(new PwrApiSdk("https://pwrrpc.pwrlabs.io/"));
        var r = await wallet.SendVmDataTxn(0, new byte[]{ 1, 2,3});
        Assert.True(r.Success);
    }   
    
    [Fact]
    public async Task TestDelegate()
    {
        var wallet = new PwrWallet(new PwrApiSdk("https://pwrrpc.pwrlabs.io/"));
        var r = await wallet.Delegate(wallet.PublicAddress, 10);
        Assert.True(r.Success);
    }    
    
    [Fact]
    public async Task TestWithdraw()
    {
        var wallet = new PwrWallet(new PwrApiSdk("https://pwrrpc.pwrlabs.io/"),"d5cebdc74ba9c0746da66e4ee13a2bae73c3e24218959afdb6e2a4f964599b66");
        var r = await wallet.WithDrawPWR(wallet.PublicAddress, 1);
        Assert.True(r.Success);
    }
    
    [Fact]
    public async Task TestClaimVmId()
    {
        var wallet = new PwrWallet(new PwrApiSdk("https://pwrrpc.pwrlabs.io/"));
        var r = await wallet.ClaimVmId(0);
        Assert.True(r.Success);
    }

    
}