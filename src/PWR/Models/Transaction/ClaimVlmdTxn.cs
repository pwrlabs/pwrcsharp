using System;
using Newtonsoft.Json;
namespace PWR.Models;

public class ClaimVlmdTxn : Transaction
{
    [JsonProperty("vidaId")]
    public ulong VidaId {get;}
    public ClaimVlmdTxn(uint size, ulong blockNumber, uint positionintheBlock, ulong fee, string type, string sender, string receiver, uint nonce, string hash,ulong value, ulong timestamp,ulong vidaId)
     : base(size, blockNumber, positionintheBlock, fee, type, sender, receiver, nonce, hash,value, timestamp)
    {
        this.VidaId = vidaId;
    }
}