using Extensions;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace web_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SubjectsController : ControllerBase
    {
        [HttpGet(Name = "SubjectsListByClassID")]
        public async Task<IEnumerable<dynamic>> SubjectsListByClassID(DBRepository dBRepo, [FromQuery] int classId)
        {
            var c = await dBRepo.Subjects.PiplineToListAsync(
                    [
                        new BsonDocument("$match",
                            new BsonDocument("ClassID", classId))
                    ]
                );
            return c.MapToDotNetValue(); 
        }
    }
}
