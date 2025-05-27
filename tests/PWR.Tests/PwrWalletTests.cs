using System.Text;
using PWR;
using PWR.Models;
using PWR.Utils;

namespace PWR.Tests;

public class PwrWalletTests
{
    [Fact]
    public void TestWalletAddress()
    {
        var wallet = new Wallet("demand april length soap cash concert shuffle result force mention fringe slim");
        Assert.Equal("0xe68191b7913e72e6f1759531fbfaa089ff02308a", wallet.GetAddress());
    }

    [Fact]
    public void TestWalletSignAndVerify()
    {
        var wallet = new Wallet("demand april length soap cash concert shuffle result force mention fringe slim");
        var message = Encoding.UTF8.GetBytes("Hello World!!");
        var signature = wallet.Sign(message);
        Assert.True(wallet.Verify(message, signature));
    }
    [Fact]
    public async Task TestTransfer()
    {
        var wallet = new Wallet("demand april length soap cash concert shuffle result force mention fringe slim");

        var r = await wallet.TransferPWR("0x8953f1c3B53Bd9739F78dc8B0CD5DB9686C40b09", 1);
        Assert.True(r.Success);
    }
    
    [Fact]
    public async Task TestNonce()
    {
        var wallet = new Wallet(12);
        var nonce = await wallet.GetNonce();
        Assert.Equal(0UL, nonce);
    }
    
    [Fact]
    public async Task TestGetBalance()
    {
        var wallet = new Wallet(12);
        var bal = await wallet.GetBalance();
        Assert.Equal(0UL, bal);
    }
    
    [Fact]
    public async Task TestSendVidaData()
    {
        var wallet = new Wallet("demand april length soap cash concert shuffle result force mention fringe slim");
        var r = await wallet.SendVidaData(0, new byte[]{1, 2,3});
        Assert.True(r.Success);
    }
    
    [Fact]
    public async Task TestDelegate()
    {
        var wallet = new Wallet("demand april length soap cash concert shuffle result force mention fringe slim");
        var r = await wallet.Delegate("0x023C98B9E4B6C2E94DB8A724D1131F8E33F8D8EB", 1000000000);
        Assert.True(r.Success);
    }

    [Fact]
    public async Task TestSendPayableVidaData()
    {   
        ulong vidaId = 897435;
        var wallet = new Wallet("demand april length soap cash concert shuffle result force mention fringe slim");
        var r = await wallet.SendPayableVidaData(vidaId, Encoding.UTF8.GetBytes("Hello World"), 10);
        Assert.True(r.Success);
    }

    // [Fact]
    // public async Task TestSetGuardian()
    // {
    //     var wallet = new Wallet(new RPC("https://pwrrpc.pwrlabs.io/"), "0xfefe6247b79a3a0dedcd8269f7e4ed4794231654c8e017815eadee1d2084d1c0");
    //    var r = await wallet.SetGuardian("0x61Bd8fc1e30526Aaf1C4706Ada595d6d236d9883", (ulong)DateTimeOffset.Now.ToUnixTimeSeconds() + 100000);
    //     Assert.True(r.Success);
    // }

    // [Fact]
    // public async Task TestClaimVidaIdTxn()
    // {
    //     var wallet = new Wallet("demand april length soap cash concert shuffle result force mention fringe slim");
    //     var r = await wallet.ClaimVidaId(123400);
    //     Assert.True(r.Success);
    // }
}