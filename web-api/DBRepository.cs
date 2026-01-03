namespace web_api
{
    public class DBRepository : crawler.Database.DBRepository
    {
        public DBRepository(string connectionString, string dbName) : base(connectionString, dbName)
        {
        }
    }
}
