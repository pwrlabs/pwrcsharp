using System.Text;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using PWRCS.Models;

namespace PWRCS.Tests;

public class PwrSdkTests
{

    [Fact]
    public void TestGetLatestBlockNumber()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        long r = sdk.GetLatestBlockNumber();
        Assert.NotEqual(0,r);
    }
    [Fact]
    public async Task TestGetVmDataTxns()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        List<VmDataTxn> vmDataTxns = await sdk.GetVmDataTxns(1, 800, 10023);
        Assert.NotEmpty(vmDataTxns);
    }

     [Fact]
    public async Task TestGetVmDataTxnsFilterByPerBytePrefix()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        List<VmDataTxn> vmDataTxns = await sdk.GetVmDataTxnsFilterByPerBytePrefix(1, 800, 10023,new byte[] {1,7});
        Assert.NotEmpty(vmDataTxns);
    }

    
    
    [Fact]
    public async Task TestGetActiveVotingPower()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        long r = await sdk.GetActiveVotingPower();
        Assert.NotEqual(0,r);
    }
    [Fact]
    public async Task TestGetTotalDelegatorsCount()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        int r = await sdk.GetTotalDelegatorsCount();
        Assert.Equal(0,r);
    }

    [Fact]
    public async Task TestGetBlockChainVersion()
    {
        
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        short r = await sdk.GetBlockChainVersion();
        Assert.Equal(0,r);
    }


     [Fact]
    public async Task TestGetChainId()
    {
        
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetChainId();
        Console.WriteLine("helo");
        Assert.Equal(0, ((sbyte)r));
    }


    
     [Fact]
    public async Task  TestGetFeePerByte()
    {
        
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        long r = await sdk.GetFeePerByte();
        Assert.Equal(100,r);
    }

   
    
    [Fact]
    public async Task TestGetBalance()
    {
        var wallet = new PwrWallet(new PwrApiSdk("https://pwrrpc.pwrlabs.io/"));
        
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetBalanceOfAddress(wallet.PublicAddress);
        Assert.Equal(0, r.Data);
    }
    
    [Fact]
    public async Task TestGetBlocksCount()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetBlocksCount();
        Assert.True(r.Data > 0);
    }
    
    [Fact]
    public async Task TestGetBlockByNumber()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetBlockByNumber(10);
        Assert.Equal(10, r.Number);
    }    
    
    [Fact]
    public async Task TestGetTotalValidatorsCount()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetTotalValidatorsCount();
        Assert.True(r.Data > 0);
    }    
    
    [Fact]
    public async Task TestGetStandbyValidatorsCount()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetStandbyValidatorsCount();
         Assert.NotEqual(0,r.Data);
    }    
    
    [Fact]
    public async Task TestGetActiveValidatorsCount()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetActiveValidatorsCount();
        Assert.NotEqual(0,r.Data);
    }    
    
    [Fact]
    public async Task TestGetAllValidators()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetAllValidators();
        Assert.True(r.Any());
    }    
    
    [Fact]
    public async Task TestGetStandbyValidators()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetStandbyValidators();
        Assert.True(r.Any());
    }   
    
    [Fact]
    public async Task TestGetActiveValidators()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetActiveValidators();
        Assert.True(r.Any());
    }  
    
    [Fact]
    public async Task TestGetOwnerOfVm()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetOwnerOfVm(0);
        Assert.True(!string.IsNullOrWhiteSpace(r));
    }    
    
    [Fact]
    public async Task TestUpdateFeePerByte()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.UpdateFeePerByte();
        Assert.True(r > 0);
    }    
    
   
}

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
        var wallet = new PwrWallet(new PwrApiSdk("https://pwrrpc.pwrlabs.io/"), "040928b5e48d6761b8ab2b657b5e7735f16cc5365c153c82a6016a4541f16ef9");
        
        var wallet2 = new PwrWallet(new PwrApiSdk("https://pwrrpc.pwrlabs.io/"));
        var r = await wallet.TransferPwr(wallet2.PublicAddress, 100);
        Assert.True(r.Success);
    }
    
    [Fact]
    public async Task TestNonce()
    {
        var wallet = new PwrWallet(new PwrApiSdk("https://pwrrpc.pwrlabs.io/"));
        var nonce = await wallet.GetNonce();
        Assert.Equal(0, nonce);
    }  
    
    [Fact]
    public async Task TestGetBalance()
    {
        var wallet = new PwrWallet(new PwrApiSdk("https://pwrrpc.pwrlabs.io/"));
        var nonce = await wallet.GetBalance();
        Assert.Equal(0, nonce);
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
        var wallet = new PwrWallet(new PwrApiSdk("https://pwrrpc.pwrlabs.io/"));
        var r = await wallet.Withdraw(wallet.PublicAddress, 10);
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