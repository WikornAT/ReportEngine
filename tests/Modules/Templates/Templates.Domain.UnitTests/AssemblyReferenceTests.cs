using FluentAssertions;
using Xunit;

namespace Templates.Domain.UnitTests;

public sealed class AssemblyReferenceTests
{
    [Fact]
    public void AssemblyReferenceShouldExist()
    {
        typeof(Templates.Domain.AssemblyReference).Should().NotBeNull();
    }
}
