using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace crawler
{
    enum SyllabusType { Full = 0, Smart = 1 }

    public class QuestionPriority
    {
        [BsonId]
        public required int QuestionPriorityID { get; set; }
        public required string QuestionPriorityLabel { get; set; }

    }
}
