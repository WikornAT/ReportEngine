using FluentAssertions;
using Xunit;

namespace Designer.Domain.UnitTests;

public sealed class AssemblyReferenceTests
{
    [Fact]
    public void AssemblyReferenceShouldExist()
    {
        typeof(Designer.Domain.AssemblyReference).Should().NotBeNull();
    }
}
