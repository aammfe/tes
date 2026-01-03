using Extensions;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace web_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ClassesController : ControllerBase
    {
        [HttpGet(Name = "ClassesListByCourseID")]
        public async Task<IEnumerable<dynamic>> ClassesListByCourseID(DBRepository dBRepo, [FromQuery] int courseId)
        {
            var c = await dBRepo.Classes.PiplineToListAsync(
                    [
                       new BsonDocument("$match",
                            new BsonDocument("Course.CourseID", courseId)),
                       new BsonDocument("$project",
                            new BsonDocument("ClassesList", 1)),
                       new BsonDocument("$unwind",
                            new BsonDocument("path", "$ClassesList")),
                       new BsonDocument("$replaceRoot",
                            new BsonDocument("newRoot", "$ClassesList"))
                    ]
                );
            return c.MapToDotNetValue(); 
        }
    }
}
