using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace StoreMetrics.ViewModels
{
    public class EvaluationVm
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [Required(ErrorMessage = "Please select a store.")]
        [BsonElement("StoreId")]
        public string StoreId { get; set; } = "";

        [BsonElement("StoreName")]
        public string StoreName { get; set; } = "";

        [Range(1, 5)][BsonElement("Cleanliness")] public int Cleanliness { get; set; }
        [Range(1, 5)][BsonElement("Condition")] public int Condition { get; set; }
        [Range(1, 5)][BsonElement("CustomerEngagement")] public int CustomerEngagement { get; set; }
        [Range(1, 5)][BsonElement("PersonalGrooming")] public int PersonalGrooming { get; set; }
        [Range(1, 5)][BsonElement("Accuracy")] public int Accuracy { get; set; }
        [Range(1, 5)][BsonElement("SpeedOfService")] public int SpeedOfService { get; set; }
        [Range(1, 5)][BsonElement("ProductQuality")] public int ProductQuality { get; set; }

        [BsonElement("Remarks")]
        [DataType(DataType.MultilineText)]
        public string? Remarks { get; set; }

        [BsonElement("EvaluationDate")]
        [DataType(DataType.Date)]
        public DateTime EvaluationDate { get; set; } = DateTime.Today;

        [BsonIgnore]
        public double AverageRating =>
            new[] { Cleanliness, Condition, CustomerEngagement, PersonalGrooming, Accuracy, SpeedOfService, ProductQuality }
            .Where(v => v >= 1).DefaultIfEmpty(0).Average();

        [BsonIgnore]
        public double PerformancePercent => Math.Round((AverageRating / 5.0) * 100.0, 2);

        [BsonIgnore]
        public string PerformanceDescription =>
            AverageRating switch
            {
                >= 4.5 => "Excellent",
                >= 3.5 => "Good",
                >= 2.5 => "Average",
                >= 1.5 => "Poor",
                _ => "Very Poor"
            };
    }

   
}
