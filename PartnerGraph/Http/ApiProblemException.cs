namespace PartnerGraph.Http;

public sealed class ApiProblemException : Exception
{
    private ApiProblemException(
        int statusCode,
        string title,
        string detail,
        string code,
        IReadOnlyDictionary<string, string[]>? errors = null)
        : base(detail)
    {
        StatusCode = statusCode;
        Title = title;
        Code = code;
        Errors = errors;
    }

    public int StatusCode { get; }

    public string Title { get; }

    public string Code { get; }

    public IReadOnlyDictionary<string, string[]>? Errors { get; }

    public static ApiProblemException Validation(string field, string message)
    {
        return new ApiProblemException(
            StatusCodes.Status400BadRequest,
            "One or more validation errors occurred.",
            message,
            "validation_error",
            new Dictionary<string, string[]>
            {
                [field] = [message]
            });
    }

    public static ApiProblemException Conflict(
        string title,
        string code,
        string detail)
    {
        return new ApiProblemException(
            StatusCodes.Status409Conflict,
            title,
            detail,
            code);
    }

    public static ApiProblemException NotFound(
        string title,
        string code,
        string detail)
    {
        return new ApiProblemException(
            StatusCodes.Status404NotFound,
            title,
            detail,
            code);
    }
}
