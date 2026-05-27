using FluentAssertions;
using Xunit;

namespace Templates.Application.UnitTests;

public sealed class AssemblyReferenceTests
{
    [Fact]
    public void AssemblyReferenceShouldExist()
    {
        typeof(Templates.Application.AssemblyReference).Should().NotBeNull();
    }
}
