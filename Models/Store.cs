using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace StoreMetrics.Models
{
    public class Store
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("StoreName")]
        public string StoreName { get; set; } = "";

        // NEW: all address fields moved here
        [BsonElement("BuildingNumber")]
        public string? BuildingNumber { get; set; }

        [BsonElement("StreetName")]
        public string StreetName { get; set; } = "";

        [BsonElement("Brgy")]
        public string Brgy { get; set; } = "";

        [BsonElement("City")]
        public string City { get; set; } = "";

        [BsonElement("Province")]
        public string Province { get; set; } = "";

        [BsonElement("PostalCode")]
        public string PostalCode { get; set; } = "";

        [BsonElement("AuditDate")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Unspecified)]
        public DateTime AuditDate { get; set; }

        [BsonElement("IsActive")]
        public bool IsActive { get; set; } = true;
    }
}
