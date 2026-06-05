using System.Data;

namespace Shared.Database;

public abstract class BaseRepository
{
    protected readonly DapperContext _context;

    protected BaseRepository(DapperContext context)
    {
        _context = context;
    }

    protected IDbConnection Connection => _context.CreateConnection();
}
