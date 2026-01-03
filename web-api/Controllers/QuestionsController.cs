using Extensions;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace web_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class Questiones : ControllerBase
    {
        //[HttpGet("question-types", Name = "getQuestionTypes")]
        //public async Task<IEnumerable<dynamic>> GetQuestionTypes(DBRepository dBRepo, 
        //        [FromQuery][Required] int classId, 
        //        [FromQuery][Required] int subjectId, 
        //        [FromQuery][Required] List<int> chapterIds,
        //        [FromQuery][Required] List<int> topicIds)
        //{
        //    var c = await dBRepo.Topics.PiplineToListAsync(
        //            [
        //    //           new BsonArray
        //    //{
        //    //    new BsonDocument("$match",
        //    //    new BsonDocument("TopicID",
        //    //    new BsonDocument("$in",
        //    //    new BsonArray
        //    //                {
        //    //                    8103,
        //    //                    8104,
        //    //                    8105,
        //    //                    8106,
        //    //                    8107,
        //    //                    8108,
        //    //                    8109,
        //    //                    8110,
        //    //                    8111,
        //    //                    8112,
        //    //                    8113,
        //    //                    8114,
        //    //                    8115,
        //    //                    8116,
        //    //                    8117,
        //    //                    8118,
        //    //                    8119,
        //    //                    8120,
        //    //                    8121,
        //    //                    8122,
        //    //                    8123,
        //    //                    8124,
        //    //                    8125,
        //    //                    8126,
        //    //                    8127,
        //    //                    8128,
        //    //                    8129,
        //    //                    8130,
        //    //                    8687,
        //    //                    8688,
        //    //                    8689,
        //    //                    8690,
        //    //                    8691,
        //    //                    8692,
        //    //                    8131,
        //    //                    8132,
        //    //                    8133,
        //    //                    8134,
        //    //                    8135,
        //    //                    8136
        //    //                }))),
        //    //    new BsonDocument("$lookup",
        //    //    new BsonDocument
        //    //        {
        //    //            { "from", "questions" },
        //    //            { "localField", "TopicID" },
        //    //            { "foreignField", "TopicID" },
        //    //            { "as", "questions" }
        //    //        }),
        //    //    new BsonDocument("$unwind",
        //    //    new BsonDocument("path", "$questions")),
        //    //    new BsonDocument("$group",
        //    //    new BsonDocument("_id", "$questions.QuestionSubTypeID"))
        //    }


        //            ]
        //        );
        //    return c.MapToDotNetValue(); 
        //}
    }
}
