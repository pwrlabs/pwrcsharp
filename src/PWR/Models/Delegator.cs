using Newtonsoft.Json;

namespace PWR.Models;

public class Delegator
{

    public string Address { get; }
   
    public string ValidatorAddress { get; }

    public ulong Shares { get; }

    public ulong DelegatedPwr { get; }
      

    public Delegator(string address, string validatorAddress, ulong shares, ulong delegatedPwr)
    {
        Address = address;
        ValidatorAddress = validatorAddress;
        Shares = shares;
        DelegatedPwr = delegatedPwr;
    }
}