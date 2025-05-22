# PWR

PWR is a C# library for interacting with the PWR network. It provides an easy interface for wallet management and sending transactions on PWR.

<div align="center">
<!-- markdownlint-restore -->

[![Pull Requests welcome](https://img.shields.io/badge/PRs-welcome-ff69b4.svg?style=flat-square)](https://github.com/pwrlabs/pwrcsharp/issues?q=is%3Aissue+is%3Aopen+label%3A%22help+wanted%22)
<a href="https://github.com/pwrlabs/pwrcsharp/blob/main/LICENSE/">
  <img src="https://img.shields.io/badge/license-MIT-black">
</a>
<!-- <a href="https://github.com/pwrlabs/pwrcsharp/stargazers">
  <img src='https://img.shields.io/github/stars/pwrlabs/pwrcsharp?color=yellow' />
</a> -->
<a href="https://pwrlabs.io/">
  <img src="https://img.shields.io/badge/powered_by-PWR Chain-navy">
</a>
<a href="https://www.youtube.com/@pwrlabs">
  <img src="https://img.shields.io/badge/Community%20calls-Youtube-red?logo=youtube"/>
</a>
<a href="https://twitter.com/pwrlabs">
  <img src="https://img.shields.io/twitter/follow/pwrlabs?style=social"/>
</a>

</div>

## 🌐 Documentation

How to [Guides](https://docs.pwrlabs.io/pwrchain/overview) 🔜 & [API](https://docs.pwrlabs.io/developers/developing-on-pwr-chain/what-is-a-decentralized-application) 💻

Play with [Code Examples](https://github.com/keep-pwr-strong/pwr-examples/) 🎮

### Installation

PWR is available on The NuGet Gallery. Add this dependency to your `.csproj` file:

```bash
dotnet add package PWR
```

## 💫 Getting Started

**Import the library:**

```csharp 
using PWR;
using PWR.Models;
```

**Set your RPC node:**

```csharp
var rpc = new RPC("https://pwrrpc.pwrlabs.io/");
```

**Generate a new random wallet:**

```csharp
var wallet = new Wallet(12); 
```

**Import wallet by Seed Phrase:**

```csharp
string seedPhrase = "your seed phrase here";
var wallet = new Wallet(seedPhrase); 
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

Sending a transcation to the PWR Chain returns a Response object, which specified if the transaction was a success, and returns relevant data.
If the transaction was a success, you can retrieive the transaction hash, if it failed, you can fetch the error.

```csharp
WalletResponse response = await wallet.TransferPWR("recipientAddress", amount);

if(response.Success) {
   	Console.WriteLine("Transcation Hash: " + response.Hash);
} else {
	Console.WriteLine("Error: " + response.Error);
}
```

**Send data to a VIDA:**

```csharp
uint vidaId = 123;
byte[] data = Encoding.UTF8.GetBytes("Hello, World!");
var response = await wallet.SendVidaData(vidaId, data);

if(response.Success) {
   	Console.WriteLine("Transcation Hash: " + response.Hash);
} else {
	Console.WriteLine("Error: " + response.Error);
}
```

### Other Static Calls

**Get RPC Node Url:**

Returns currently set RPC node URL.

```csharp
var url = rpc.GetRpcNodeUrl()
```

**Get Fee Per Byte:**

Gets the latest fee-per-byte rate.

```csharp
ulong fee = await rpc.GetFeePerByte();
```

**Get Balance Of Address:**

Gets the balance of a specific address.

```csharp
ulong balance = await rpc.GetBalanceOfAddress("0x...");
```

**Get Nonce Of Address:**

Gets the nonce/transaction count of a specific address.

```csharp
uint nonce = await rpc.GetNonceOfAddress("0x..."); 
```

## ✏️ Contributing

If you consider to contribute to this project please read [CONTRIBUTING.md](https://github.com/pwrlabs/pwrcsharp/blob/main/CONTRIBUTING.md) first.

You can also join our dedicated channel for [developers](https://discord.com/channels/1141787507189624992/1180224756033790014) on the [PWR Chain Discord](https://discord.com/invite/YASmBk9EME)

## 📜 License

Copyright (c) 2025 PWR Labs

Licensed under the [MIT license](https://github.com/pwrlabs/pwrcsharp/blob/main/LICENSE).
