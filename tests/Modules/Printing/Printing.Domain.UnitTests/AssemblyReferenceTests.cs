using FluentAssertions;
using Xunit;

namespace Printing.Domain.UnitTests;

public sealed class AssemblyReferenceTests
{
    [Fact]
    public void AssemblyReferenceShouldExist()
    {
        typeof(Printing.Domain.AssemblyReference).Should().NotBeNull();
    }
}
