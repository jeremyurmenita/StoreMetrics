using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace StoreMetrics.Models
{
    public class StoreRating
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("StoreId")]
        public string StoreId { get; set; } = "";

        [BsonElement("StoreName")]
        public string StoreName { get; set; } = "";

        [BsonElement("Cleanliness")]
        public int Cleanliness { get; set; }

        [BsonElement("Condition")]
        public int Condition { get; set; }

        [BsonElement("CustomerEngagement")]
        public int CustomerEngagement { get; set; }

        [BsonElement("PersonalGrooming")]
        public int PersonalGrooming { get; set; }

        [BsonElement("Accuracy")]
        public int Accuracy { get; set; }

        [BsonElement("SpeedOfService")]
        public int SpeedOfService { get; set; }

        [BsonElement("ProductQuality")]
        public int ProductQuality { get; set; }

        [BsonElement("Remarks")]
        public string? Remarks { get; set; }

        [BsonElement("EvaluationDate")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Unspecified)]
        public DateTime EvaluationDate { get; set; } = DateTime.Today;
    }
}
