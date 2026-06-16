using ReportEngine.SharedKernel;

namespace Reporting.Infrastructure.Services;

/// <summary>
/// Production implementation of <see cref="IDateTimeProvider"/>.
/// Returns <see cref="DateTimeOffset.UtcNow"/> from the system clock.
/// Replace with a deterministic stub in tests.
/// </summary>
internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
