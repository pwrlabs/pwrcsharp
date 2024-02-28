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
    <PackageReference Include="PWRCS.Json" Version="13.0.1" />
  </ItemGroup>

</Project>

```

### Usage

**Import the library:**
```java 
using com.github.pwrlabs.pwrj.*;
```

**Set your RPC node:**
```java
var sdk = new PwrApiSdk("https://pwrrpc.pwrlabs.io/");
```

**Generate a new wallet:** 
```java
 var wallet = new PwrWallet(sdk);
```

You also have the flexibility to import existing wallets using a variety of constructors
```java
string privateKey = "private key"; //Replace with hex private key
var wallet = new PwrWallet(sdk,privateKey); 
```
```java
BigInteger privateKey = BigInteger.Parse("...");
var wallet = new PwrWallet(sdk,privateKey); 
```
```java
EthECKey ecKey = ...; //Generate or import ecKey 
var wallet = new PwrWallet(sdk,ecKey); 
```

**Get wallet address:**
```java
string address = await wallet.GetAddress();
```

**Get wallet balance:**
```java
ulong balance = await wallet.GetBalance();
```

**Transfer PWR tokens:**
```java
var response = await wallet.TransferPWR("recipientAddress", amount); 
```

Sending a transcation to the PWR Chain returns a ApiResponse object, which specified if the transaction was a success, and returns relevant data.
If the transaction was a success, you can retrieive the transaction hash, if it failed, you can fetch the error.

```java
ApiResponse r = await wallet.TransferPWR("recipientAddress", amount); 

if(r.isSuccess) {
   Console.WriteLine("Transcation Hash: " + r.getMessage());
} else {
   Console.WriteLine("Error: " + r.Error);
}
```

**Send data to a VM:**
```java
uint vmId = 123;
byte[] data = ...;
var r = await wallet.SendVmDataTxn(vmId, data);

if(r.isSuccess)) {
    Console.WriteLine("Transcation Hash: " + r.getMessage());
} else {
    Console.WriteLine("Error: " + r.getError());
}
```
### Other Method Calls

**Update fee per byte:**

Fetches latest fee-per-byte rate from the RPC node and updates the local fee rate.

```java
await sdk.UpdateFeePerByte();
``` 

**Get Fee Per Byte: **

Gets the latest fee-per-byte rate.

```java
ulong fee = await sdk.GetFeePerByte();
```

**Get Balance Of Address:**

Gets the balance of a specific address.

```java
ulong balance = await sdk.GetBalanceOfAddress("0x...");
```

**Get Nonce Of Address:**

Gets the nonce/transaction count of a specific address.

```java
uint nonce = await sdk.GetNonceOfAddress("0x..."); 
```

**Broadcast Txn:**

Broadcasts a signed transaction to the network.

```java
byte[] signedTransaction = ...;
await sdk.GroadcastTxn(signedTransaction);
```

## Contributing

Pull requests are welcome! 

For major changes, please open an issue first to discuss what you would like to change.

## License

[MIT](https://choosealicense.com/licenses/mit/)
