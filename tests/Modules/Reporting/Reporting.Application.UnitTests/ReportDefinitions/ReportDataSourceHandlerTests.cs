using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Reporting.Application.Features.ReportDefinitions.GetDataSourceById;
using Reporting.Application.Features.ReportDefinitions.GetDataSources;
using Reporting.Application.Features.ReportDefinitions.RemoveDataSource;
using Reporting.Application.Features.ReportDefinitions.UpdateDataSource;
using Reporting.Domain.Enums;
using Reporting.Domain.ReportDefinitions;

using ReportEngine.SharedKernel;

using Xunit;

namespace Reporting.Application.UnitTests.ReportDefinitions;

/// <summary>
/// Unit tests for DataSource CRUD command and query handlers.
/// Uses EF Core InMemory provider to exercise handler logic without infrastructure dependencies.
/// </summary>
public sealed class ReportDataSourceHandlerTests : IAsyncLifetime, IDisposable
{
    private readonly InMemoryReportingDbContext _dbContext;
    private readonly Mock<ICurrentUserService> _currentUserMock;

    public ReportDataSourceHandlerTests()
    {
        DbContextOptions<InMemoryReportingDbContext> options = new DbContextOptionsBuilder<InMemoryReportingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new InMemoryReportingDbContext(options);

        _currentUserMock = new Mock<ICurrentUserService>();
        _currentUserMock.Setup(x => x.UserId).Returns("test-user");
    }

    public async Task InitializeAsync() => await _dbContext.Database.EnsureCreatedAsync();

    public async Task DisposeAsync() => await _dbContext.DisposeAsync();

    public void Dispose() => _dbContext.Dispose();

    // ── Seed helpers ──────────────────────────────────────────────────────────

    private async Task<(ReportDefinition definition, Guid dataSourceId)> SeedDefinitionWithDataSourceAsync()
    {
        ReportDefinition definition = ReportDefinition.Create("Sales Report", "Finance", "system");
        ReportDataSource ds = definition.AddDataSource(
            name: "MainDataset",
            dataSourceType: ReportDataSourceType.SqlQuery,
            connectionStringName: "ReportingDb",
            queryText: "SELECT * FROM sales",
            sortOrder: 1,
            modifiedBy: "system");

        _dbContext.ReportDefinitions.Add(definition);
        await _dbContext.SaveChangesAsync();

        return (definition, ds.Id);
    }

    // ── GetReportDataSourcesQueryHandler ──────────────────────────────────────

    [Fact]
    public async Task GetDataSourcesReturnsDataSourcesOrderedBySortOrder()
    {
        // Arrange
        ReportDefinition definition = ReportDefinition.Create("Report A", "Finance", "system");
        definition.AddDataSource("DatasetB", ReportDataSourceType.SqlQuery, "Db", "SELECT 2", 2, "system");
        definition.AddDataSource("DatasetA", ReportDataSourceType.SqlQuery, "Db", "SELECT 1", 1, "system");
        _dbContext.ReportDefinitions.Add(definition);
        await _dbContext.SaveChangesAsync();

        var handler = new GetReportDataSourcesQueryHandler(_dbContext);
        var query = new GetReportDataSourcesQuery(definition.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Name.Should().Be("DatasetA");
        result.Value[1].Name.Should().Be("DatasetB");
    }

    [Fact]
    public async Task GetDataSourcesWhenDefinitionNotFoundReturnsNotFound()
    {
        var handler = new GetReportDataSourcesQueryHandler(_dbContext);
        var query = new GetReportDataSourcesQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().EndWith(".NotFound");
    }

    // ── GetReportDataSourceByIdQueryHandler ───────────────────────────────────

    [Fact]
    public async Task GetDataSourceByIdReturnsCorrectDto()
    {
        var (definition, dsId) = await SeedDefinitionWithDataSourceAsync();

        var handler = new GetReportDataSourceByIdQueryHandler(_dbContext);
        var query = new GetReportDataSourceByIdQuery(definition.Id, dsId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(dsId);
        result.Value.Name.Should().Be("MainDataset");
        result.Value.QueryText.Should().Be("SELECT * FROM sales");
    }

    [Fact]
    public async Task GetDataSourceByIdWhenDefinitionNotFoundReturnsNotFound()
    {
        var handler = new GetReportDataSourceByIdQueryHandler(_dbContext);
        var query = new GetReportDataSourceByIdQuery(Guid.NewGuid(), Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().EndWith(".NotFound");
    }

    [Fact]
    public async Task GetDataSourceByIdWhenDataSourceNotFoundReturnsNotFound()
    {
        var (definition, _) = await SeedDefinitionWithDataSourceAsync();

        var handler = new GetReportDataSourceByIdQueryHandler(_dbContext);
        var query = new GetReportDataSourceByIdQuery(definition.Id, Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().EndWith(".NotFound");
    }

    // ── UpdateReportDataSourceCommandHandler ──────────────────────────────────

    [Fact]
    public async Task UpdateDataSourceUpdatesAllFieldsAndPersists()
    {
        var (definition, dsId) = await SeedDefinitionWithDataSourceAsync();

        var handler = new UpdateReportDataSourceCommandHandler(
            _dbContext, _currentUserMock.Object, NullLogger<UpdateReportDataSourceCommandHandler>.Instance);

        var command = new UpdateReportDataSourceCommand(
            definition.Id, dsId, "UpdatedDataset",
            ReportDataSourceType.StoredProcedure, "ReportingDb_Prod", "usp_GetSales", 3);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("UpdatedDataset");
        result.Value.DataSourceType.Should().Be(ReportDataSourceType.StoredProcedure);
        result.Value.ConnectionStringName.Should().Be("ReportingDb_Prod");
        result.Value.QueryText.Should().Be("usp_GetSales");
        result.Value.SortOrder.Should().Be(3);
    }

    [Fact]
    public async Task UpdateDataSourceWhenDefinitionNotFoundReturnsNotFound()
    {
        var handler = new UpdateReportDataSourceCommandHandler(
            _dbContext, _currentUserMock.Object, NullLogger<UpdateReportDataSourceCommandHandler>.Instance);

        var command = new UpdateReportDataSourceCommand(
            Guid.NewGuid(), Guid.NewGuid(), "X",
            ReportDataSourceType.SqlQuery, "Db", "SELECT 1", 1);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().EndWith(".NotFound");
    }

    [Fact]
    public async Task UpdateDataSourceWhenDataSourceNotFoundReturnsNotFound()
    {
        var (definition, _) = await SeedDefinitionWithDataSourceAsync();

        var handler = new UpdateReportDataSourceCommandHandler(
            _dbContext, _currentUserMock.Object, NullLogger<UpdateReportDataSourceCommandHandler>.Instance);

        var command = new UpdateReportDataSourceCommand(
            definition.Id, Guid.NewGuid(), "X",
            ReportDataSourceType.SqlQuery, "Db", "SELECT 1", 1);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().EndWith(".NotFound");
    }

    [Fact]
    public async Task UpdateDataSourceWithDuplicateNameReturnsDomainViolation()
    {
        // Arrange: definition with two data sources
        ReportDefinition definition = ReportDefinition.Create("Report B", "Finance", "system");
        definition.AddDataSource("DatasetA", ReportDataSourceType.SqlQuery, "Db", "SELECT 1", 1, "system");
        ReportDataSource dsB = definition.AddDataSource("DatasetB", ReportDataSourceType.SqlQuery, "Db", "SELECT 2", 2, "system");
        _dbContext.ReportDefinitions.Add(definition);
        await _dbContext.SaveChangesAsync();

        var handler = new UpdateReportDataSourceCommandHandler(
            _dbContext, _currentUserMock.Object, NullLogger<UpdateReportDataSourceCommandHandler>.Instance);

        // Try to rename DatasetB to DatasetA
        var command = new UpdateReportDataSourceCommand(
            definition.Id, dsB.Id, "DatasetA",
            ReportDataSourceType.SqlQuery, "Db", "SELECT 2", 2);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DomainViolation");
    }

    // ── RemoveReportDataSourceCommandHandler ──────────────────────────────────

    [Fact]
    public async Task RemoveDataSourceRemovesAndPersists()
    {
        // Arrange: two data sources so removal is allowed on a Draft report
        ReportDefinition definition = ReportDefinition.Create("Report C", "Finance", "system");
        ReportDataSource ds1 = definition.AddDataSource("DS1", ReportDataSourceType.SqlQuery, "Db", "SELECT 1", 1, "system");
        definition.AddDataSource("DS2", ReportDataSourceType.SqlQuery, "Db", "SELECT 2", 2, "system");
        _dbContext.ReportDefinitions.Add(definition);
        await _dbContext.SaveChangesAsync();

        var handler = new RemoveReportDataSourceCommandHandler(
            _dbContext, _currentUserMock.Object, NullLogger<RemoveReportDataSourceCommandHandler>.Instance);

        var command = new RemoveReportDataSourceCommand(definition.Id, ds1.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        ReportDefinition? persisted = await _dbContext.ReportDefinitions
            .Include(r => r.DataSources)
            .FirstOrDefaultAsync(r => r.Id == definition.Id);

        persisted!.DataSources.Should().HaveCount(1);
        persisted.DataSources[0].Name.Should().Be("DS2");
    }

    [Fact]
    public async Task RemoveDataSourceWhenDefinitionNotFoundReturnsNotFound()
    {
        var handler = new RemoveReportDataSourceCommandHandler(
            _dbContext, _currentUserMock.Object, NullLogger<RemoveReportDataSourceCommandHandler>.Instance);

        var command = new RemoveReportDataSourceCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().EndWith(".NotFound");
    }

    [Fact]
    public async Task RemoveDataSourceWhenDataSourceNotFoundReturnsNotFound()
    {
        var (definition, _) = await SeedDefinitionWithDataSourceAsync();

        var handler = new RemoveReportDataSourceCommandHandler(
            _dbContext, _currentUserMock.Object, NullLogger<RemoveReportDataSourceCommandHandler>.Instance);

        var command = new RemoveReportDataSourceCommand(definition.Id, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().EndWith(".NotFound");
    }

    [Fact]
    public async Task RemoveLastDataSourceOnActiveReportReturnsDomainViolation()
    {
        var (definition, dsId) = await SeedDefinitionWithDataSourceAsync();

        // Publish to make it Active
        definition.Publish("system");
        await _dbContext.SaveChangesAsync();

        var handler = new RemoveReportDataSourceCommandHandler(
            _dbContext, _currentUserMock.Object, NullLogger<RemoveReportDataSourceCommandHandler>.Instance);

        var command = new RemoveReportDataSourceCommand(definition.Id, dsId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DomainViolation");
    }
}
