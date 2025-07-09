
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PWR.Models;

public class TransactionConverter : JsonConverter
{
    public ulong TimeStamp {get;}
    public TransactionConverter(ulong timestamp){
        TimeStamp = timestamp;
    }
    public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Transaction);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            JObject jsonObject = JObject.Load(reader);
            string type = jsonObject["type"]?.ToString() ?? "";

            switch (type)
            {
                case "VIDA Data":
                    return jsonObject.ToObject<VidaDataTransaction>(serializer);
                case "Set Guardian":
                    return jsonObject.ToObject<SetGuardianTxn>(serializer);
                case "Remove Guardian":
                    return jsonObject.ToObject<RemoveGuardianTxn>(serializer);
                case "Guardian Approval":
                    return jsonObject.ToObject<GuardianApprovalTxn>(serializer);
                case "Validator Claim Spot":
                    return jsonObject.ToObject<ClaimSpotTxn>(serializer);
                case "Payable VIDA Data":
                    return jsonObject.ToObject<PayableVidaDataTxn>(serializer);
                case "Claim VIDA ID":
                    return jsonObject.ToObject<ClaimVlmdTxn>(serializer);
                case "Conduit Approval":
                    return jsonObject.ToObject<ConduitApprovalTxn >(serializer);  
                case "Delegate":
                    return jsonObject.ToObject<DelegateTxn>(serializer);
                 case "Validator Join":
                    return jsonObject.ToObject<JoinTxn>(serializer);
                 case "Transfer":
                    return jsonObject.ToObject<TransferTxn>(serializer);
                 case "Withdraw":
                    return jsonObject.ToObject<WithdrawTxn>(serializer);
                default:
                   return jsonObject.ToObject<Transaction>(serializer);
            }
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
}