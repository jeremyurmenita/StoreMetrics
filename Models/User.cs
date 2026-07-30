using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace StoreMetrics.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("FirstName")]
        public string FirstName { get; set; } = "";

        // ✅ New Middle Name field
        [BsonElement("MiddleName")]
        public string? MiddleName { get; set; } = "";

        [BsonElement("LastName")]
        public string LastName { get; set; } = "";

        [BsonElement("Email")]
        public string Email { get; set; } = "";

        [BsonElement("PhoneNumber")]
        public string PhoneNumber { get; set; } = "";

        [BsonElement("Username")]
        public string Username { get; set; } = "";

        [BsonElement("Password")]
        public string Password { get; set; } = "";
    }
}
