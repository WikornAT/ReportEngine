using FluentAssertions;
using Xunit;

namespace Printing.Application.UnitTests;

public sealed class AssemblyReferenceTests
{
    [Fact]
    public void AssemblyReferenceShouldExist()
    {
        typeof(Printing.Application.AssemblyReference).Should().NotBeNull();
    }
}
