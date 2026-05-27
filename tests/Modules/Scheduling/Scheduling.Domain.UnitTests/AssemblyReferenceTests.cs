using FluentAssertions;
using Xunit;

namespace Scheduling.Domain.UnitTests;

public sealed class AssemblyReferenceTests
{
    [Fact]
    public void AssemblyReferenceShouldExist()
    {
        typeof(Scheduling.Domain.AssemblyReference).Should().NotBeNull();
    }
}
