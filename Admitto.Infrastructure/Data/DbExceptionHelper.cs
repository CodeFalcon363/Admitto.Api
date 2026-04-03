using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Admitto.Infrastructure.Data
{
    /// <summary>
    /// Helpers for interpreting EF Core DbUpdateException causes.
    /// SQL Server error 2601 = unique index violation (cannot insert duplicate key row).
    /// SQL Server error 2627 = unique constraint violation (violation of PRIMARY KEY / UNIQUE constraint).
    /// Both fire when a unique index rejects a duplicate — checking both covers all index types.
    /// </summary>
    internal static class DbExceptionHelper
    {
        internal static bool IsDuplicateKey(DbUpdateException ex)
            => ex.InnerException is SqlException sql
               && (sql.Number == 2601 || sql.Number == 2627);
    }
}
