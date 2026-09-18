using SOFIA.Domain.Common;

namespace SOFIA.API.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result) =>
        result.IsSuccess ? new OkObjectResult(result.Value) : result.ToProblemResult();

    public static IActionResult ToActionResult(this Result result) =>
        result.IsSuccess ? new NoContentResult() : result.ToProblemResult();

    public static ObjectResult ToProblemResult(this Result result)
    {
        var problemDetails = new ProblemDetails
        {
            Status = result.StatusCode,
            Detail = result.Error.Message,
        };
        problemDetails.Extensions["code"] = result.Error.Code;

        return new ObjectResult(problemDetails) { StatusCode = result.StatusCode };
    }
}
