using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace FileNova.Middleware
{
    public class DbQueryInterceptor : DbCommandInterceptor
    {
        public static int QueryCount = 0;

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            QueryCount++;
            Console.WriteLine($"📊 [SQL #{QueryCount}] {command.CommandText.Substring(0, Math.Min(100, command.CommandText.Length))}...");
            return base.ReaderExecuting(command, eventData, result);
        }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            QueryCount++;
            Console.WriteLine($"📊 [SQL #{QueryCount}] {command.CommandText.Substring(0, Math.Min(100, command.CommandText.Length))}...");
            return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
