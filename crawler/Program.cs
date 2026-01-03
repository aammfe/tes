using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using crawler;
using crawler.Database;
using Extensions;
using Flurl;
using Flurl.Http;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;

var repo = new DBRepository("mongodb://localhost:27017", "test-soltion");

await SaveQuestions();
Console.WriteLine("Hello, World!");



async Task SaveAssets()
{
    var questionsWithAssets = await (await repo.Questions.FindAsync(new BsonDocument("$or",
                new BsonArray
                    {
                        new BsonDocument("MultipleOptions.EmOption",
                        new Regex("src=")),
                        new BsonDocument("EnglishQuestionDetails",
                        new Regex("src=")),
                        new BsonDocument("UrduQuestionDetails",
                        new Regex("src="))
                    }))).ToListAsync();

    var urls = questionsWithAssets.SelectMany(x =>
    {
        var eng = GetUrls(x.GetByAnyKey("EnglishQuestionDetails").ConvertToString());
        var urdu = GetUrls(x.GetByAnyKey("UrduQuestionDetails").ConvertToString());
        var options = x.GetByAnyKey("MultipleOptions").ConvertToBsonArray().SelectMany(x =>
        {
            return GetUrls(x.GetByAnyKey("EmOption").ConvertToString())
            .Concat(
                 GetUrls(x.GetByAnyKey("UmOption").ConvertToString()));
        });

        return eng.Concat(options).Concat(options);
    }).
    DistinctBy(x=>x.ToLowerInvariant());

    // classes
    {
       var classesImages = (await repo.Classes.AsQueryable().ToListAsync()).Select(x=>x.GetNestedByKeys("Course","ImageUrl").ConvertToString());
        urls = urls.Concat(classesImages);
    }

    // subjects
    {
        var subjectsImages = (await repo.Subjects.AsQueryable().ToListAsync()).Select(x => x.GetNestedByKeys("ImageUrl").ConvertToString()).Where(x=>!string.IsNullOrEmpty(x));
        urls = urls.Concat(subjectsImages);
    }



    foreach (var assetUrl in urls)
    {
        if (assetUrl.Contains("base64", StringComparison.CurrentCultureIgnoreCase))
            continue;
        if (assetUrl.StartsWith("https://latex.codecogs.com", StringComparison.CurrentCultureIgnoreCase))
            continue;

        var url = assetUrl.StartsWith("http") ?
            new Uri(assetUrl, UriKind.Absolute).PathAndQuery : 
            assetUrl;

        var filePath = ConvertToFilePath(url);
        
        if (File.Exists(filePath)) 
            continue;

        try
        {
            var bytes = await ("https://paktestsolution.com" + url).GetBytesAsync();
            await File.WriteAllBytesAsync(filePath, bytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to download {assetUrl}: {ex.Message}");
        }
    }

   

    IEnumerable<string> GetUrls(string content) 
    {
        var matches = Regex.Matches(content, "src\\s*=\\s*\"([^\"]+)\"");
        return matches.Select(m => m.Groups[1].Value);
    }

    string ConvertToFilePath(string url) 
    {
        if (url.StartsWith('/'))
        {
            url = url.Substring(1);
        }

        var filePath = Path.Combine("assets", url);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        return filePath;
    }
}


async Task SaveQuestions()
{

    var pipeline = new BsonDocumentStagePipelineDefinition<BsonDocument, BsonDocument>(new[]
                {
                    new BsonDocument("$group", new BsonDocument
                    {
                        { "_id", new BsonDocument
                            {
                                { "ClassID", "$ClassID" },
                                { "SubjectID", "$SubjectID" }
                            }
                        },
                        { "topics_list", new BsonDocument("$push", "$TopicID") },
                        { "chapters_list", new BsonDocument("$push", "$ChapterID") }
                    })
                });

    var classes = await repo.Topics.Aggregate(pipeline).ToListAsync();

    foreach (var @class in classes)
    {
        var topicIds = @class.GetByAnyKey("topics_list").ConvertToBsonArray().Select(x => x.AsInt32).ToArray();
        var chaptersIds = @class.GetByAnyKey("chapters_list").ConvertToBsonArray().Select(x => x.AsInt32).ToArray();
        (int[] questionSubTypes, QuestionPriority[] periorities) = await GetQuestionsMetaData(
                                @class.GetNestedByKeys("_id", "ClassID").ConvertToString(), 
                                @class.GetNestedByKeys("_id", "SubjectID").ConvertToString(), 
                                chaptersIds, 
                                topicIds);

        foreach (var periority in periorities)
        {
            await repo.QuestionPriorities.ReplaceOneAsync(x=>x.QuestionPriorityID == periority.QuestionPriorityID, periority, new ReplaceOptions { IsUpsert = true });
        }

        foreach (var subtypeId in questionSubTypes)
        {
            var subjectsRes = await GetQuestionListAsync(string.Join(',', topicIds), subtypeId.ToString(), string.Join(',', periorities.Select(x=>x.QuestionPriorityID))) ?? "{}";
            var questionList = BsonSerializer.Deserialize<BsonDocument>(subjectsRes).GetByAnyKey("QuestionsList").ConvertToBsonArray().Select(x => x.AsBsonDocument);

            if (!questionList.Any()) continue;

            var questionSubType = BsonSerializer.Deserialize<BsonDocument>(subjectsRes).GetByAnyKey("QuestionSubType").AsBsonDocument;

            await repo.QuestionsSubTypes.ReplaceOneAsync(new BsonDocument("QuestionSubTypeID", subtypeId), questionSubType, new ReplaceOptions { IsUpsert = true });

            foreach (var question in questionList)
            {
                question["QuestionSubTypeID"] = questionSubType.GetByAnyKey("QuestionSubTypeID");
            }

            if (questionList.Any())
            {
                await repo.Questions.InsertManyAsync(questionList);
            }
        }
    }

    async Task<string> GetQuestionListAsync(string topicId, string subtype, string questionPeriorityId)
    {
        return await GetAsync("GetQuestions", new Dictionary<string, string>
        {
            { "TopicIDs", topicId},
            { "QuestionPeriorityID", questionPeriorityId },
            { "SyllabusType", $"{(int)SyllabusType.Full}" },
            { "QuestionSubTypeID", subtype } 
        });
    }
}

async Task SaveChapters()
{
    var allSubjects = await repo.Subjects.AsQueryable().ToListAsync();

    foreach (var subject in allSubjects)
    {
        var subjectsRes = await GetChapterListAsync(subject.GetByAnyKey("SubjectID")?.ConvertToString() ?? "") ?? "{}";
        var chapterList = BsonSerializer.Deserialize<BsonDocument>(subjectsRes).GetByAnyKey("ChaptersList").ConvertToBsonArray().Select(x => x.AsBsonDocument);
        var topicList = BsonSerializer.Deserialize<BsonDocument>(subjectsRes).GetByAnyKey("TopicsList").ConvertToBsonArray().Select(x => x.AsBsonDocument);
        if (chapterList.Any()) 
        {
            await repo.Chapters.InsertManyAsync(chapterList);
            await repo.Topics.InsertManyAsync(topicList);
        }
    }

    async Task<string> GetChapterListAsync(string chapterId)
    {
        return await GetAsync("ChaptersListBySubjectID", new Dictionary<string, string> { { "SubjectID", chapterId } });
    }
}

async Task SaveSubjects()
{
    var allClassIds = await GetAllClassIds();

    foreach (var classId in allClassIds)
    {
        var subjectsRes = await GetClassAsync(classId.GetByAnyKey("classId")?.ConvertToString() ?? "") ?? "{}";
        var subjects = BsonSerializer.Deserialize<BsonDocument>(subjectsRes).GetByAnyKey("SubjectsList").ConvertToBsonArray().Select(x=> x.AsBsonDocument);
        await repo.Subjects.InsertManyAsync(subjects);
    }

    async Task<string> GetClassAsync(string classId)
    {
        return await GetAsync("SubjectsListByClassID", new Dictionary<string, string> { { "classId", classId } });
    }

    async Task<List<BsonDocument>> GetAllClassIds()
    {
        var pipline = new List<BsonDocument>
    {
        new BsonDocument("$unwind",
        new BsonDocument("path", "$ClassesList")),
        new BsonDocument("$project",
        new BsonDocument
            {
                { "_id", 0 },
                { "classId", "$ClassesList.ClassID" }
            })
    };

        return await (await repo.Classes.AggregateAsync<BsonDocument>(pipline)).ToListAsync();

    }
}



async Task SaveClasses()
{

    var courses = new Dictionary<string, string>
    {
        {"PTB", "1" },
        {"AFAQ SNC", "2" },
        {"OXFORD SNC", "3" },
        {"GOHAR SNC", "4" },
        {"B.A", "5" },
    };


    foreach (var course in courses)
    {
        var json = await GetClassesAsync(course.Value);
        var doc = BsonSerializer.Deserialize<BsonDocument>(json);
        await repo.Classes.InsertOneAsync(doc);
    }


    async Task<string> GetClassesAsync(string courseId)
    {
        return await GetAsync("ClassesListByCourseID", new Dictionary<string, string> { { "CourseID", courseId } });
    }
}

static async Task<string> GetAsync(string endpointName ,Dictionary<string, string> queryParams)
{
    Url req = $"https://paktestsolution.com/AjaxCalling/{endpointName}";

    foreach (var param in queryParams)
    {
        req.AppendQueryParam(param.Key, param.Value);
    }


    return await GenerateValidRequest(req).GetStringAsync();
}

static async Task<(int[]  questionSubTypes, QuestionPriority[] periorities)> GetQuestionsMetaData(string classId, string subjectId, int [] chapterIds, int[] topicIds)
{
    Url req = 
        $"https://paktestsolution.com/GeneratePaper/GetGenerate?ClassID={classId}&SubjectID={subjectId}&ChapterIDs={string.Join(',', chapterIds)}&TopicIDs={string.Join(',', topicIds)}";

    var htmlContent = await GenerateValidRequest(req).GetStringAsync();
    var document = new HtmlParser().ParseDocument(htmlContent);
    var subtypesElements = document.GetElementById("QuestionType") as IHtmlSelectElement;
    int[] subTypes = [.. subtypesElements.Options.Where(x => !string.IsNullOrEmpty(x.Value)).Select(x => int.Parse(x.Value))];


    var questionPeriorityElements = document.GetElementById("QuestionPeriority") as IHtmlSelectElement;
    QuestionPriority[] periorities = 
        [.. questionPeriorityElements.Options.
            Where(x => !string.IsNullOrEmpty(x.Value)).
            Select(x => new QuestionPriority
                {
                    QuestionPriorityID = int.Parse(x.Value),
                    QuestionPriorityLabel = x.TextContent
                } )];

    return (subTypes, periorities);
}

static IFlurlRequest GenerateValidRequest(Url req)
{
    req.Host = "paktestsolution.com";

    return req
    .WithHeader("Cookie", "School_SchoolName=YOUR%20SCHOOL%20NAME%20HERE; School_SchoolAddress=YOUR%20SCHOOL%20ADDRESS%20HERE; School_PrincipalName=MUHAMMAD%20AHMAD; School_ExpiryDate=12%2F30%2F2026; School_PendingBalance=0; School_ImageUrl=%2Fimages%2Fschools%2F87a322b8-bbc1-4bcf-9a8e-844c638d95c3.jpg; .AspNetCore.Antiforgery.ReFOm65_nGU=CfDJ8PzwGwsk92xKu8VCtIR0V2rLALYsU6ROSHNM5x6Eh3DH1JtP3W32LlYikaTsEU77S2OkJMVEHDYeyStd4VcKsonVhP2HBudwBf2_hqCjnxVqEhT4x54niR8ZiQRyYR_vS5vRbaxlh7d-1NdZZ0LMm6M; SchoolAuthCookie=CfDJ8PzwGwsk92xKu8VCtIR0V2obJNeB-3-p5yqRvrO-VsgN0QF4UbxNciUlFrXHPCoCXRdinlzvhD19qeQHJ-VxXq34E8WDbJFFWIRBrnnKqcnvec_8_DiDVSAIsWRUDGC7CsjhnHfmDI-IM45Yx5ezYbXiQhSucaFmFgUcNeqJoNKZZuSWlsROPFz_O2ztDJJYif-vE1YTbpD-r-cWXgDk4THcfRywIudSn6SgO4QZUYjSfDIFqhyYeC08MVjd1vNkDGbZa6CdS05ohR8rLhoRSjdIT0UY3fqT1qXNshARArdGbuJ0vpVcAVtyM56m8LvThBQM9D1FG45IusXbq7G2Elg; .AspNetCore.Mvc.CookieTempDataProvider=CfDJ8PzwGwsk92xKu8VCtIR0V2oPBdEEM9wBT82NdmwRxWIwXuqOHTQxG1-EP_zy978PKE358-c3QN5gNuSDJI9Medmyltj6DAPRlgpjw7ASLiy6GHDdDVLpdAho6g0ZbI3ZFFcTBPB6xc9qgltDeDICbAt0K7qaaU7JyA3Ckedf8fuM")
    .WithHeader("Accept", "*/*")
    .WithHeader("User-Agent",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36");
}