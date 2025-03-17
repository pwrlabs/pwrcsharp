using PWR.Models;

namespace PWR;

public class TxnForGuardianApproval {
    public bool Valid {get;}
    public string GuardianAddress{get;}
    public String ErrorMessage {get;}
    public Transaction Transaction {get;}

    public TxnForGuardianApproval(bool valid,String errorMessage,string guardianAddress,Transaction transaction){
         Valid = valid;
         ErrorMessage = errorMessage;
         Transaction = transaction;
         GuardianAddress = guardianAddress;
    }
}