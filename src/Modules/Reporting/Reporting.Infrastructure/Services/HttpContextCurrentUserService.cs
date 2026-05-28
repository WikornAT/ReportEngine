using Microsoft.AspNetCore.Http;

using ReportEngine.SharedKernel;

namespace Reporting.Infrastructure.Services;

internal sealed class HttpContextCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserId =>
        _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "system";

    public string? DisplayName =>
        _httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value;
}
