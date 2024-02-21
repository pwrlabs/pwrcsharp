using Newtonsoft.Json;

namespace PWRCS.Models;

public class Delegator
{

    [JsonProperty("address")]
    public string Address { get; }
    [JsonProperty("validatorAddress")]
    public string ValidatorAddress { get; }
    [JsonProperty("shares")]
    public decimal Shares { get; }
    [JsonProperty("delegatedPwr")]
    public decimal DelegatedPwr { get; }
      

    public Delegator(string address, string validatorAddress, decimal shares, decimal delegatedPwr)
    {
        Address = address;
        ValidatorAddress = validatorAddress;
        Shares = shares;
        DelegatedPwr = delegatedPwr;
    }
}