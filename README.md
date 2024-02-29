# PWRCS

PWRCS is a C# library for uinteracting with the PWR network. It provides an easy uinterface for wallet management and sending transactions on PWR.

## Features

- Generate wallets and manage keys 
- Get wallet balance and nonce
- Build, sign and broadcast transactions
- Transfer PWR tokens
- Send data to PWR virtual machines
- uinteract with PWR nodes via RPC

## Getting Started

### Prerequisites

- .NET 7.0

### Installation

PWRCS is available on The NuGet Gallery. Add this dependency to your `.csproj` file:

```xml
   <Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net7.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="PWRCS" Version="7.0.0" />
  </ItemGroup>

</Project>

```

### Usage

**Import the library:**
```csharp 
using com.github.pwrlabs.pwrcs.*;
```

**Set your RPC node:**
```csharp
var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
```

**Generate a new wallet:** 
```csharp
 var wallet = new PwrWallet(sdk);
```

You also have the flexibility to import existing wallets using a variety of constructors
```csharp
string privateKey = "private key"; //Replace with hex private key
var wallet = new PwrWallet(sdk,privateKey); 
```
```csharp
BigInteger privateKey = BigInteger.Parse("...");
var wallet = new PwrWallet(sdk,privateKey); 
```
```csharp
EthECKey ecKey = ...; //Generate or import ecKey 
var wallet = new PwrWallet(sdk,ecKey); 
```

**Get wallet address:**
```csharp
string address = await wallet.GetAddress();
```

**Get wallet balance:**
```csharp
ulong balance = await wallet.GetBalance();
```

**Transfer PWR tokens:**
```csharp
var response = await wallet.TransferPWR("recipientAddress", amount); 
```

Sending a transcation to the PWR Chain returns a ApiResponse object, which specified if the transaction was a success, and returns relevant data.
If the transaction was a success, you can retrieive the transaction hash, if it failed, you can fetch the error.

```csharp
ApiResponse r = await wallet.TransferPWR("recipientAddress", amount); 

if(r.isSuccess) {
   Console.WriteLine("Transcation Hash: " + r.Message);
} else {
   Console.WriteLine("Error: " + r.Error);
}
```

**Send data to a VM:**
```csharp
uint vmId = 123;
byte[] data = ...;
var r = await wallet.SendVmDataTxn(vmId, data);

if(r.isSuccess) {
    Console.WriteLine("Transcation Hash: " + r.Message);
} else {
    Console.WriteLine("Error: " + r.Error);
}
```
### Other Method Calls

**Update fee per byte:**

Fetches latest fee-per-byte rate from the RPC node and updates the local fee rate.

```csharp
await sdk.UpdateFeePerByte();
``` 

**Get Fee Per Byte:**

Gets the latest fee-per-byte rate.

```csharp
ulong fee = await sdk.GetFeePerByte();
```

**Get Balance Of Address:**

Gets the balance of a specific address.

```csharp
ulong balance = await sdk.GetBalanceOfAddress("0x...");
```

**Get Nonce Of Address:**

Gets the nonce/transaction count of a specific address.

```csharp
uint nonce = await sdk.GetNonceOfAddress("0x..."); 
```

**Broadcast Txn:**

Broadcasts a signed transaction to the network.

```csharp
byte[] signedTransaction = ...;
await sdk.BroadcastTxn(signedTransaction);
```

## Contributing

Pull requests are welcome! 

For major changes, please open an issue first to discuss what you would like to change.

## License

[MIT](https://choosealicense.com/licenses/mit/)
