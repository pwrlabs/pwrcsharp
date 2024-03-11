using PWRCS.Models;

namespace PWRCS;

public class TxnForGuardianApproval {
    private bool Valid {get;}
    private String ErrorMessage {get;}
    private Transaction Transaction {get;}

    public TxnForGuardianApproval(bool valid,String errorMessage,Transaction transaction){
         Valid = valid;
         ErrorMessage = errorMessage;
         Transaction = transaction;
    }
}