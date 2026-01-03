using System.Dynamic;
using System.Globalization;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Extensions
{
    public static class BsonExtensions
    {
        //public static ExpandoObject ToExpandoObject(this BsonDocument bsonDocument)
        //{
        //    return JsonConvert.DeserializeObject<ExpandoObject>(bsonDocument.ToJson(), new ExpandoObjectConverter());
        //}
        public static BsonArray ToBsonArray<T>(this IEnumerable<T> enumerable) => new BsonArray(enumerable);

        public static double? ConvertToNullableDouble(this BsonValue? bsonValue)
        {
            if (bsonValue is null) return null;
            if (bsonValue.BsonType == BsonType.Null) return null;
            return bsonValue.ConvertToDouble();
        }
        public static double ConvertToDouble(this BsonValue bsonValue)
        {
            switch (bsonValue.BsonType)
            {
                case BsonType.Int32:
                    return bsonValue.AsInt32;
                case BsonType.Int64:
                    return bsonValue.AsInt64;
                case BsonType.Double:
                    return bsonValue.AsDouble;
                default:
                    throw new InvalidOperationException($"Unsupported BSON type: {bsonValue.BsonType}");
            }
        }

        public static string? ConvertToString(this BsonValue? bsonValue)
        {
            if (bsonValue is null) return null;
            return bsonValue.BsonType switch
            {
                BsonType.String => bsonValue.AsString,
                BsonType.Null => string.Empty,
                BsonType.Int32 or
                BsonType.Int64 or
                BsonType.Double => bsonValue.
                                    ConvertToDouble().
                                    ToString("N0"),
                //BsonType.DateTime => bsonValue.ConvertToDate()?.ToAppFormatString(),
                BsonType.Array => string.Join(", ", bsonValue.AsBsonArray.Select(ConvertToString)),
                _ => bsonValue.ToString(),
            };
        }

        public static DateTime? ConvertToDate(this BsonValue? bsonValue, string format = "MM/dd/yyyy")
        {
            if (bsonValue is null) return null;
            switch (bsonValue.BsonType)
            {
                case BsonType.DateTime:
                    return bsonValue.ToUniversalTime();
                case BsonType.String:

                    if (DateTime.TryParseExact(bsonValue.AsString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    {
                        return date;
                    }
                    return null;
                case BsonType.Null:
                    return null;
                default:
                    throw new InvalidOperationException($"Unsupported BSON type: {bsonValue.BsonType}");
            }
            ;
        }

        public static BsonArray ConvertToBsonArray<T>(this IEnumerable<T>? values)
        {
            return BsonArray.Create(values ?? []);
        }

        public static BsonArray ConvertToBsonArray(this BsonValue? bsonValue)
        {
            if (bsonValue is null || bsonValue.BsonType == BsonType.Null)
                return new BsonArray(0);
            return bsonValue.AsBsonArray;
        }


        public static BsonDocument? AsDoc(this BsonValue bsonValue, string key)
        {
            var doc = bsonValue.AsBsonDocument;
            if (!doc.Contains(key))
            {
                return null;
            }
            return doc[key].AsBsonDocument;
        }

        public static BsonValue? GetByAnyKey(this BsonValue? bsonValue, params string[] keys)
        {
            if (bsonValue is null)
            {
                return null;
            }
            var doc = bsonValue.AsBsonDocument;

            foreach (var key in keys)
            {
                if (doc.Contains(key))
                {
                    return doc[key];
                }
            }
            return null;
        }

        public static BsonValue? GetNestedByKeys(this BsonValue? bsonValue, params string[] keys)
        {
            if (bsonValue is null)
            {
                return null;
            }

            var nestedKeys = keys.SelectMany(x => x.Split('.'));


            var current = bsonValue;
            foreach (var key in nestedKeys)
            {
                var doc = current.AsBsonDocument;

                if (!doc.Contains(key))
                {
                    return null;
                }
                current = doc[key];
            }
            return current;
        }

        public static BsonValue? AddOrUpdateField(this BsonValue? bsonValue, string key, BsonValue? value)
        {
            if (bsonValue is null)
            {
                return bsonValue;
            }

            var doc = bsonValue.AsBsonDocument;
            if (!doc.Contains(key))
            {
                doc.Add(key, value);
            }
            else
            {
                doc[key] = value;
            }
            return bsonValue;
        }

        public static BsonValue? RemoveIfExist(this BsonValue? bsonValue, string key)
        {
            if (bsonValue is null)
            {
                return bsonValue;
            }

            var doc = bsonValue.AsBsonDocument;
            if (doc.Contains(key))
            {
                doc.Remove(key);
            }
            return bsonValue;
        }

        public static async Task<List<BsonDocument>> PiplineToListAsync<T>(this IMongoCollection<T> collection, IEnumerable<BsonDocument> pipline) 
        {
            var pipelineDef = new BsonDocumentStagePipelineDefinition<T, BsonDocument>([.. pipline]);
            return await collection.Aggregate(pipelineDef).ToListAsync();
        }

        public static async Task<BsonDocument?> PiplineFirstOrDefaultAsync<T>(this IMongoCollection<T> collection, IEnumerable<BsonDocument> pipline)
        {
            var pipelineDef = new BsonDocumentStagePipelineDefinition<T, BsonDocument>([.. pipline]);
            return await collection.Aggregate(pipelineDef).FirstOrDefaultAsync();
        }

        public static IEnumerable<object> MapToDotNetValue(this IEnumerable<BsonValue> bsonValues)
        {
            return bsonValues.Select(BsonTypeMapper.MapToDotNetValue).ToList();
        }
        public static object MapToDotNetValue(this BsonValue bsonValue)
        {
            return BsonTypeMapper.MapToDotNetValue(bsonValue);
        }
    }
}
