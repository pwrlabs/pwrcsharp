
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PWRCS.Models;

public class TransactionConverter : JsonConverter
{
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
                case "Validator Claim Spot":
                    return jsonObject.ToObject<ClaimVlmdTxn>(serializer);
                case "VM Data":
                    return jsonObject.ToObject<DelegateTxn>(serializer);
                 case "join":
                    return jsonObject.ToObject<JoinTxn>(serializer);
                 case "transfer":
                    return jsonObject.ToObject<TransferTxn>(serializer);
                 case "vmdatatxn":
                    return jsonObject.ToObject<VmDataTxn>(serializer);
                 case "withdraw":
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