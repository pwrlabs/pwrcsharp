
using PWRCS;
using PWRCS.Models;
namespace PWRCS.Tests;
public class PwrSdkTests
{

    [Fact]
    public void TestGetLatestBlockNumber()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        ulong r = sdk.GetLatestBlockNumber();
        Assert.NotEqual(0UL,r);
    }
    [Fact]
    public async Task TestGetVmDataTxns()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        List<VmDataTxn> vmDataTxns = await sdk.GetVmDataTxns(1, 13, 10023);
        Assert.NotEmpty(vmDataTxns);
    }

     [Fact]
    public async Task TestGetVmDataTxnsFilterByPerBytePrefix()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        List<VmDataTxn> vmDataTxns = await sdk.GetVmDataTxnsFilterByPerBytePrefix(1, 800, 10023,new byte[] {1,7});
        Assert.IsType<List<VmDataTxn>>(vmDataTxns);
    }

    
    
    [Fact]
    public async Task TestGetActiveVotingPower()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        ulong r = await sdk.GetActiveVotingPower();
        Assert.NotEqual(0UL, r);
    }
    [Fact]
    public async Task TestGetTotalDelegatorsCount()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        uint r = await sdk.GetTotalDelegatorsCount();
         Assert.Equal(0UL, r);
    }

    [Fact]
    public async Task TestGetBlockChainVersion()
    {
        
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        short r = await sdk.GetBlockChainVersion();
        Assert.NotEqual(0,r);
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
        ulong r = await sdk.GetFeePerByte();
        Assert.Equal(100UL,r);
    }

   
    
    [Fact]
    public async Task TestGetBalance()
    {
        var wallet = new PwrWallet(new PwrApiSdk("https://pwrrpc.pwrlabs.io/"));
        
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetBalanceOfAddress(wallet.PublicAddress);
        ulong data = r.Data;
         Assert.Equal(0UL, data);
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
        Assert.Equal(10UL, r.Number);
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
         Assert.NotEqual(0UL,r.Data);
    }    
    
    [Fact]
    public async Task TestGetActiveValidatorsCount()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetActiveValidatorsCount();
        Assert.NotEqual(0UL,r.Data);
    }    
    
    [Fact]
    public async Task TestGetAllValidators()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetAllValidators();
        Assert.NotEmpty(r);
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
        ulong before = sdk.FeePerByte;
        await sdk.UpdateFeePerByte();
        Assert.NotEqual(before,sdk.FeePerByte);
    } 

    [Fact]
    public async Task TestGetNonceOfAddress()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetNonceOfAddress("0xf6fe6a14b3aac06c2c102cf5f028df35157f9770");
        Assert.True(r.Data >= 0);


    }       
    
    [Fact]
    public async Task TestGetBalanceOfAddress()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetBalanceOfAddress("0xf6fe6a14b3aac06c2c102cf5f028df35157f9770");
        Assert.True(r.Data >= 0);
    }  

    [Fact]
    public async Task TestGetGuardianOfAddress()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetGuardianOfAddress("0xf6fe6a14b3aac06c2c102cf5f028df35157f9770");
        Assert.NotEqual(" ",r);
    }  

    [Fact]
    public async Task TestGetDelegatedPWR()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetDelegatedPWR("0xf6fe6a14b3aac06c2c102cf5f028df35157f9770","0xf6fe6a14b3aac06c2c102cf5f028df35157f9770");
        Assert.True(r >= 0);
    }  

    [Fact]
    public async Task TestGetShareValue()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetShareValue("0xf6fe6a14b3aac06c2c102cf5f028df35157f9770");
        Assert.True(r >= 0);    
    }  
      
}
