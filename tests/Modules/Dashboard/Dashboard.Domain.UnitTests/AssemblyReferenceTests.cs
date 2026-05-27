using FluentAssertions;
using Xunit;

namespace Dashboard.Domain.UnitTests;

public sealed class AssemblyReferenceTests
{
    [Fact]
    public void AssemblyReferenceShouldExist()
    {
        typeof(Dashboard.Domain.AssemblyReference).Should().NotBeNull();
    }
}
