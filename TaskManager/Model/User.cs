using MongoDB.Bson.Serialization.Attributes;

namespace TaskManager.Model
{
    public class User
    {
        public string id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        [BsonElement("Tasks")]
        public List<TaskProperty> Tasks { get; set; }
    }
}
