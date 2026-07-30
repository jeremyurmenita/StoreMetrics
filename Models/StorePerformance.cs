using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace StoreMetrics.Models
{
    public class StorePerformance
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("StoreId")]
        public string StoreId { get; set; } = "";

        [BsonElement("StoreName")]
        public string StoreName { get; set; } = "";

        [BsonElement("AverageRating")]
        public double AverageRating { get; set; }

        [BsonElement("PerformancePercent")]
        public double PerformancePercent { get; set; }

        [BsonElement("PerformanceDescription")]
        public string PerformanceDescription { get; set; } = "";

        [BsonElement("EvaluationDate")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Unspecified)]
        public DateTime EvaluationDate { get; set; } = DateTime.Today;
    }
}
