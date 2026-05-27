using FluentAssertions;
using Xunit;

namespace Dashboard.Application.UnitTests;

public sealed class AssemblyReferenceTests
{
    [Fact]
    public void AssemblyReferenceShouldExist()
    {
        typeof(Dashboard.Application.AssemblyReference).Should().NotBeNull();
    }
}
