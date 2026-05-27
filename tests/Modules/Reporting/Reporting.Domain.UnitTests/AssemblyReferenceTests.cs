using FluentAssertions;
using Xunit;

namespace Reporting.Domain.UnitTests;

public sealed class AssemblyReferenceTests
{
    [Fact]
    public void AssemblyReferenceShouldExist()
    {
        typeof(Reporting.Domain.AssemblyReference).Should().NotBeNull();
    }
}
