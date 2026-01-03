using Extensions;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace web_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SyllabusesController : ControllerBase
    {
        [HttpGet(Name = "GetSyllabus")]
        public async Task<IEnumerable<dynamic>> GetAsync(DBRepository dBRepo)
        {
            var c = await dBRepo.Classes.PiplineToListAsync(
                    [
                        new BsonDocument("$match",
                        new BsonDocument("Course.IsActive", true)),
                        new BsonDocument("$project",
                        new BsonDocument("Course", 1)),
                        new BsonDocument("$replaceRoot",
                        new BsonDocument("newRoot", "$Course"))
                    ]
                );
            return c.MapToDotNetValue(); 
        }
    }
}
