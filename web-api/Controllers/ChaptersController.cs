using Extensions;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace web_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChaptersController : ControllerBase
    {
        [HttpGet(Name = "ChaptersListBySubjectID")]
        public async Task<IEnumerable<dynamic>> ChaptersListBySubjectID(DBRepository dBRepo, [FromQuery] int subjectId)
        {
            var c = await dBRepo.Chapters.PiplineToListAsync(
                    [
                        new BsonDocument("$match",
                            new BsonDocument("SubjectID", subjectId)),
                        new BsonDocument("$lookup",
                            new BsonDocument
                                {
                                    { "from", "topics" },
                                    { "localField", "ChapterID" },
                                    { "foreignField", "ChapterID" },
                                    { "as", "Topics" }
                                })
                    ]
                );
            return c.MapToDotNetValue(); 
        }
    }
}
