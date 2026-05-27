using FluentAssertions;
using Xunit;

namespace Designer.Application.UnitTests;

public sealed class AssemblyReferenceTests
{
    [Fact]
    public void AssemblyReferenceShouldExist()
    {
        typeof(Designer.Application.AssemblyReference).Should().NotBeNull();
    }
}
