using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace LannisterPay.Models
{
    public class FeeConfig
    {

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonIgnore]
        public string? Id { get; set; }
        public string FeeId { get; set; }
        public string FeeLocale { get; set; }
        public string FeeEntity { get; set; }
        public string EntityProperty { get; set; }
        public string FeeType { get; set; }
        public string FeeValue { get; set; }
        public string FeeCurrency { get; set; }

        public int FeePriority { get; set; }
    }
}
