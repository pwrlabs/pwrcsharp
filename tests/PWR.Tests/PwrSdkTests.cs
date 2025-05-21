
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

    // [Fact]
    // public async Task TestGetVidaDataTransactions()
    // {
    //     var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
    //     List<VidaDataTxn> vidaDataTxns = await sdk.GetVidaDataTransactions(85411, 85420, 123);
    //     Assert.NotEmpty(vidaDataTxns);
    // }

    [Fact]
    public async Task TestGetActiveVotingPower()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        ulong r = await sdk.GetActiveVotingPower();
        Assert.NotEqual(0UL, r);
    }

    [Fact]
    public async Task TestGetBlockChainVersion()
    {
        
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        short r = await sdk.GetBlockChainVersion();
        Assert.NotEqual(0, r);
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
        var wallet = new PwrWallet(12);
        
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var data = await wallet.GetBalance();
        Assert.Equal(0UL, data);
    }

    [Fact]
    public async Task TestGetNonce()
    {
        var wallet = new PwrWallet(12);
        
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
    
    // [Fact]
    // public async Task TestGetAllValidators()
    // {
    //     var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
    //     var r = await sdk.GetAllValidators();
    //     Assert.True(r.Any());
    // }

    [Fact]
    public async Task TestGetStandbyValidators()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetStandbyValidators();
        Assert.True(r.Any());
    }

    // [Fact]
    // public async Task TestGetActiveValidators()
    // {
    //     var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
    //     var r = await sdk.GetActiveValidators();
    //     Assert.True(r.Any());
    // }

    [Fact]
    public async Task TestGetOwnerOfVidaIds()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetOwnerOfVidaIds(1234);
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
        var r = await sdk.GetDelegatorsOfPwr("0xe68191b7913e72e6f1759531fbfaa089ff02308a", "0x023C98B9E4B6C2E94DB8A724D1131F8E33F8D8EB");
        Assert.True(r >= 0);
    }

    [Fact]
    public async Task TestGetShareValue()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetShareValue("0x023C98B9E4B6C2E94DB8A724D1131F8E33F8D8EB");
        Assert.True(r >= 0);
    }

    [Fact]
    public async Task TestGetValidator()
    {
        var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
        var r = await sdk.GetValidator("0x023C98B9E4B6C2E94DB8A724D1131F8E33F8D8EB");
        Assert.Equal("0x023C98B9E4B6C2E94DB8A724D1131F8E33F8D8EB", r.Address); 
    }

    // [Fact]
    // public async Task TestGetDelegatees()
    // {
    //     var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
    //     var r = await sdk.GetDelegatees("0xe68191b7913e72e6f1759531fbfaa089ff02308a");
    //     Console.WriteLine("RRRR:" + r);
    //     Assert.True(1==1); 
    // }
}
