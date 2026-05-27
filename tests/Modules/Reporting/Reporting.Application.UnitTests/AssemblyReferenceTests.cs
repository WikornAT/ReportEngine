using FluentAssertions;
using Xunit;

namespace Reporting.Application.UnitTests;

public sealed class AssemblyReferenceTests
{
    [Fact]
    public void AssemblyReferenceShouldExist()
    {
        typeof(Reporting.Application.AssemblyReference).Should().NotBeNull();
    }
}
