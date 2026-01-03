using MongoDB.Bson;
using MongoDB.Driver;

namespace crawler.Database
{
    public class DBRepository
    {
        private IMongoDatabase db;

        public DBRepository(string connectionString, string dbName)
        {
            var client = new MongoClient(connectionString);
            this.db = client.GetDatabase(dbName);
        }

        public IMongoCollection<BsonDocument> Courses => this.db.GetCollection<BsonDocument>("courses");
        public IMongoCollection<BsonDocument> Classes => this.db.GetCollection<BsonDocument>("classes");
        public IMongoCollection<BsonDocument> Subjects => this.db.GetCollection<BsonDocument>("subjects");
        public IMongoCollection<BsonDocument> Chapters => this.db.GetCollection<BsonDocument>("chapters");
        public IMongoCollection<BsonDocument> Questions => this.db.GetCollection<BsonDocument>("questions");
        public IMongoCollection<BsonDocument> Topics => this.db.GetCollection<BsonDocument>("topics");
        public IMongoCollection<BsonDocument> QuestionsSubTypes => this.db.GetCollection<BsonDocument>("questionSubTypes");
        public IMongoCollection<QuestionPriority> QuestionPriorities => this.db.GetCollection<QuestionPriority>("questionPriorities");
    }
}
