using System.Net;

namespace EnterpriseTask.Mobile.Services.Api;

public sealed class ApiException(string message, HttpStatusCode? statusCode = null) : Exception(message)
{
    public HttpStatusCode? StatusCode { get; } = statusCode;
}
