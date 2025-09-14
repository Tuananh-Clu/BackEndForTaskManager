using MongoDB.Driver;
using TaskManager.Model;

namespace TaskManager.Context
{
    public class MongoDBContext
    {
        public readonly IMongoDatabase Database;
        public MongoDBContext(IMongoDatabase mongo)
        {
            Database = mongo;
        }
        public IMongoCollection<TaskProperty> Task=>Database.GetCollection<TaskProperty>("Tasks");
        public IMongoCollection<User> User => Database.GetCollection<User>("User");
    }
}
