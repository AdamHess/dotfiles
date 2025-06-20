using System.Data;

namespace AccountDeduplication.DAL.DapperDuck;

public interface IRepository<T> where T : class
{
    public IDbConnection Connection { get; }
    public string TableName { get; }

    async Task<T?> GetByIdAsync(string id)
    {
        var sql = $"SELECT * FROM {TableName} WHERE {KeyProperty.Name} = @Id";
        return await Connection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id });
    }

    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(string id);
}


