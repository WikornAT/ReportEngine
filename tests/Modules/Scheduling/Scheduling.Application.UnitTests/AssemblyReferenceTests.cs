using FluentAssertions;
using Xunit;

namespace Scheduling.Application.UnitTests;

public sealed class AssemblyReferenceTests
{
    [Fact]
    public void AssemblyReferenceShouldExist()
    {
        typeof(Scheduling.Application.AssemblyReference).Should().NotBeNull();
    }
}
