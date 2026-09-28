using Microsoft.EntityFrameworkCore;

namespace SOFIA.Application.Common.Extensions;

public static class DbUpdateExceptionExtensions
{
    // SQL Server names the violated unique index in the message, so races can map to the same conflict as the pre-check
    public static bool IsUniqueIndexViolation(this DbUpdateException exception, string indexName) =>
        exception.InnerException?.Message.Contains(indexName, StringComparison.OrdinalIgnoreCase) == true;
}
