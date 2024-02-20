namespace PWRCS.Models;

public class Delegator
{
    public string Address { get; }
    public string ValidatorAddress { get; }
    public decimal Shares { get; }
    public decimal DelegatedPwr { get; }

    public Delegator(string address, string validatorAddress, decimal shares, decimal delegatedPwr)
    {
        Address = address;
        ValidatorAddress = validatorAddress;
        Shares = shares;
        DelegatedPwr = delegatedPwr;
    }
}