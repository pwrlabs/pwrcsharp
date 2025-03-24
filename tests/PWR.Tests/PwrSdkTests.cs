
using PWR;
using PWR.Models;
namespace PWR.Tests;

public class PwrSdkTests
{

    [Fact]
    public async Task TestGetLatestBlockNumber()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        ulong r = await sdk.GetLatestBlockNumber();
        Assert.NotEqual(0UL, r);
    }

    [Fact]
    public async Task TestGetVmDataTransactions()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        List<VmDataTxn> vmDataTxns = await sdk.GetVmDataTransactions(317, 319, 123);
        Assert.NotEmpty(vmDataTxns);
    }

    [Fact]
    public async Task TestGetVmDataTransactionsFilterByPerBytePrefix()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        List<VmDataTxn> vmDataTxns = await sdk.GetVmDataTransactionsFilterByPerBytePrefix(317, 319, 123, new byte[] {1,7});
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
    public async Task TestGetDelegatorsCount()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        uint r = await sdk.GetDelegatorsCount();
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
        Assert.Equal(1000UL, r);
    }


    [Fact]
    public async Task TestGetBalance()
    {
        var wallet = new PwrWallet();
        
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var data = await wallet.GetBalance();
        Assert.Equal(0UL, data);
    }

    [Fact]
    public async Task TestGetNonce()
    {
        var wallet = new PwrWallet();
        
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var data = await wallet.GetNonce();
        Assert.Equal(0UL, data);
    }
    
    [Fact]
    public async Task TestGetBlocksCount()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetBlocksCount();
        Assert.True(r > 0);
    }
    
    [Fact]
    public async Task TestGetBlockByNumber()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetBlockByNumber(10);
        Assert.Equal(10UL, r.Number);
    }
    
    [Fact]
    public async Task TestGetValidatorsCount()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetValidatorsCount();
        Assert.True(r >= 0);
    }

    [Fact]
    public async Task TestGetStandbyValidatorsCount()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetStandbyValidatorsCount();
        Assert.True(r >= 0);
    }

    [Fact]
    public async Task TestGetActiveValidatorsCount()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetActiveValidatorsCount();
        Assert.True(r >= 0);
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
    public async Task TestGetOwnerOfVmIds()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetOwnerOfVmIds(1234);
        Assert.True(!string.IsNullOrWhiteSpace(r));
    }

    [Fact]
    public async Task TestUpdateFeePerByte()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        ulong before = sdk.FeePerByte;
        ulong after = await sdk.GetFeePerByte();
        Assert.NotEqual(before, after);
    }

    [Fact]
    public async Task TestGetNonceOfAddress()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var data = await sdk.GetNonceOfAddress("0xf6fe6a14b3aac06c2c102cf5f028df35157f9770");
        Assert.True(data >= 0);
    }

    [Fact]
    public async Task TestGetBalanceOfAddress()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var data = await sdk.GetBalanceOfAddress("0xf6fe6a14b3aac06c2c102cf5f028df35157f9770");
        Assert.True(data >= 0);
    }

    [Fact]
    public async Task TestGetGuardianOfAddress()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetGuardianOfAddress("0xf6fe6a14b3aac06c2c102cf5f028df35157f9770");
        Assert.NotEqual(" ",r);
    }

    [Fact]
    public async Task TestGetDelegatorsOfPwr()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetDelegatorsOfPwr("0x3B3B69093879E7B6F28366FA3C32762590FF547E", "0xF88AB0D32274BC659534B2AC7E0ECB2A17329FC7");
        Assert.True(r >= 0);
    }

    [Fact]
    public async Task TestGetShareValue()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetShareValue("0xF88AB0D32274BC659534B2AC7E0ECB2A17329FC7");
        Assert.True(r >= 0);
    }

    [Fact]
    public async Task TestGetValidator()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetValidator("0xF88AB0D32274BC659534B2AC7E0ECB2A17329FC7");
        Assert.Equal("F88AB0D32274BC659534B2AC7E0ECB2A17329FC7", r.Address); 
    }

    [Fact]
    public async Task TestGetDelegatees()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetDelegatees("0xD97C25c0842704588dD70A061c09A522699E2B9c");
        Assert.True(r.Any()); 
    }
}
