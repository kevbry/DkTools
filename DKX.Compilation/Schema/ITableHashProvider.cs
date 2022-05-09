namespace DKX.Compilation.Schema
{
    public interface ITableHashProvider
    {
        string GetTableHash(string tableName);
    }
}
