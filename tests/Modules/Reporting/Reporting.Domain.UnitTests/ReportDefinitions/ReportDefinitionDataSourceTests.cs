using FluentAssertions;

using Reporting.Domain.Common;
using Reporting.Domain.Enums;
using Reporting.Domain.ReportDefinitions;

using Xunit;

namespace Reporting.Domain.UnitTests.ReportDefinitions;

/// <summary>
/// Unit tests for the DataSource CRUD behaviour on the <see cref="ReportDefinition"/> aggregate.
/// Covers: AddDataSource, UpdateDataSource, RemoveDataSource invariants and guard clauses.
/// </summary>
public sealed class ReportDefinitionDataSourceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ReportDefinition CreateDraftDefinition() =>
        ReportDefinition.Create("Sales Report", "Finance", "system");

    private static ReportDefinition CreateDefinitionWithOneDataSource(out Guid dataSourceId)
    {
        ReportDefinition definition = CreateDraftDefinition();
        ReportDataSource ds = definition.AddDataSource(
            name: "MainDataset",
            dataSourceType: ReportDataSourceType.SqlQuery,
            connectionStringName: "ReportingDb",
            queryText: "SELECT * FROM sales",
            sortOrder: 1,
            modifiedBy: "system");
        dataSourceId = ds.Id;
        return definition;
    }

    // ── AddDataSource ──────────────────────────────────────────────────────────

    [Fact]
    public void AddDataSourceWithValidInputsAddsToCollection()
    {
        // Arrange
        ReportDefinition definition = CreateDraftDefinition();

        // Act
        ReportDataSource ds = definition.AddDataSource(
            name: "MainDataset",
            dataSourceType: ReportDataSourceType.SqlQuery,
            connectionStringName: "ReportingDb",
            queryText: "SELECT * FROM sales",
            sortOrder: 1,
            modifiedBy: "user1");

        // Assert
        definition.DataSources.Should().HaveCount(1);
        definition.DataSources[0].Id.Should().Be(ds.Id);
        definition.DataSources[0].Name.Should().Be("MainDataset");
        definition.DataSources[0].DataSourceType.Should().Be(ReportDataSourceType.SqlQuery);
        definition.DataSources[0].ConnectionStringName.Should().Be("ReportingDb");
        definition.DataSources[0].QueryText.Should().Be("SELECT * FROM sales");
        definition.DataSources[0].SortOrder.Should().Be(1);
        definition.ModifiedBy.Should().Be("user1");
    }

    [Fact]
    public void AddDataSourceWithDuplicateNameThrows()
    {
        // Arrange
        ReportDefinition definition = CreateDefinitionWithOneDataSource(out _);

        // Act
        Action act = () => definition.AddDataSource(
            name: "MAINDATASET", // case-insensitive duplicate
            dataSourceType: ReportDataSourceType.StoredProcedure,
            connectionStringName: "ReportingDb",
            queryText: "usp_GetSales",
            sortOrder: 2,
            modifiedBy: "user1");

        // Assert
        act.Should().Throw<ReportingDomainException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public void AddDataSourceOnArchivedReportThrows()
    {
        // Arrange
        ReportDefinition definition = CreateDefinitionWithOneDataSource(out _);
        definition.Publish("system");
        definition.Archive("system");

        // Act
        Action act = () => definition.AddDataSource(
            name: "NewDs",
            dataSourceType: ReportDataSourceType.SqlQuery,
            connectionStringName: "ReportingDb",
            queryText: "SELECT 1",
            sortOrder: 2,
            modifiedBy: "user1");

        // Assert
        act.Should().Throw<ReportingDomainException>()
            .WithMessage("*immutable*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddDataSourceWithEmptyNameThrows(string name)
    {
        ReportDefinition definition = CreateDraftDefinition();

        Action act = () => definition.AddDataSource(
            name: name,
            dataSourceType: ReportDataSourceType.SqlQuery,
            connectionStringName: "ReportingDb",
            queryText: "SELECT 1",
            sortOrder: 1,
            modifiedBy: "user1");

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void AddDataSourceWithNameExceeding100CharsThrows()
    {
        ReportDefinition definition = CreateDraftDefinition();
        string longName = new('A', 101);

        Action act = () => definition.AddDataSource(
            name: longName,
            dataSourceType: ReportDataSourceType.SqlQuery,
            connectionStringName: "ReportingDb",
            queryText: "SELECT 1",
            sortOrder: 1,
            modifiedBy: "user1");

        act.Should().Throw<ReportingDomainException>()
            .WithMessage("*100 characters*");
    }

    // ── UpdateDataSource ───────────────────────────────────────────────────────

    [Fact]
    public void UpdateDataSourceWithValidInputsUpdatesAllFields()
    {
        // Arrange
        ReportDefinition definition = CreateDefinitionWithOneDataSource(out Guid dsId);

        // Act
        definition.UpdateDataSource(
            dataSourceId: dsId,
            name: "UpdatedDataset",
            dataSourceType: ReportDataSourceType.StoredProcedure,
            connectionStringName: "ReportingDb_Prod",
            queryText: "usp_GetSales",
            sortOrder: 5,
            modifiedBy: "admin");

        // Assert
        ReportDataSource ds = definition.DataSources.Single(x => x.Id == dsId);
        ds.Name.Should().Be("UpdatedDataset");
        ds.DataSourceType.Should().Be(ReportDataSourceType.StoredProcedure);
        ds.ConnectionStringName.Should().Be("ReportingDb_Prod");
        ds.QueryText.Should().Be("usp_GetSales");
        ds.SortOrder.Should().Be(5);
        definition.ModifiedBy.Should().Be("admin");
    }

    [Fact]
    public void UpdateDataSourceWithSameNameIsIdempotent()
    {
        // Arrange — updating with the same name should not throw
        ReportDefinition definition = CreateDefinitionWithOneDataSource(out Guid dsId);

        // Act
        Action act = () => definition.UpdateDataSource(
            dataSourceId: dsId,
            name: "MainDataset", // same name, same id — OK
            dataSourceType: ReportDataSourceType.SqlQuery,
            connectionStringName: "ReportingDb",
            queryText: "SELECT * FROM sales WHERE active = 1",
            sortOrder: 1,
            modifiedBy: "user1");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void UpdateDataSourceWithDuplicateNameOnDifferentDataSourceThrows()
    {
        // Arrange
        ReportDefinition definition = CreateDefinitionWithOneDataSource(out _);
        ReportDataSource second = definition.AddDataSource(
            name: "SecondDataset",
            dataSourceType: ReportDataSourceType.SqlQuery,
            connectionStringName: "ReportingDb",
            queryText: "SELECT * FROM orders",
            sortOrder: 2,
            modifiedBy: "system");

        // Act — try to rename second to the first's name
        Action act = () => definition.UpdateDataSource(
            dataSourceId: second.Id,
            name: "MainDataset",
            dataSourceType: ReportDataSourceType.SqlQuery,
            connectionStringName: "ReportingDb",
            queryText: "SELECT * FROM orders",
            sortOrder: 2,
            modifiedBy: "user1");

        // Assert
        act.Should().Throw<ReportingDomainException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public void UpdateDataSourceWithUnknownIdThrows()
    {
        ReportDefinition definition = CreateDraftDefinition();

        Action act = () => definition.UpdateDataSource(
            dataSourceId: Guid.NewGuid(),
            name: "X",
            dataSourceType: ReportDataSourceType.SqlQuery,
            connectionStringName: "ReportingDb",
            queryText: "SELECT 1",
            sortOrder: 1,
            modifiedBy: "user1");

        act.Should().Throw<ReportingDomainException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public void UpdateDataSourceOnArchivedReportThrows()
    {
        ReportDefinition definition = CreateDefinitionWithOneDataSource(out Guid dsId);
        definition.Publish("system");
        definition.Archive("system");

        Action act = () => definition.UpdateDataSource(
            dataSourceId: dsId,
            name: "MainDataset",
            dataSourceType: ReportDataSourceType.SqlQuery,
            connectionStringName: "ReportingDb",
            queryText: "SELECT 1",
            sortOrder: 1,
            modifiedBy: "user1");

        act.Should().Throw<ReportingDomainException>()
            .WithMessage("*immutable*");
    }

    // ── RemoveDataSource ───────────────────────────────────────────────────────

    [Fact]
    public void RemoveDataSourceRemovesFromCollection()
    {
        // Arrange
        ReportDefinition definition = CreateDefinitionWithOneDataSource(out Guid dsId);
        ReportDataSource second = definition.AddDataSource(
            name: "SecondDataset",
            dataSourceType: ReportDataSourceType.SqlQuery,
            connectionStringName: "ReportingDb",
            queryText: "SELECT 1",
            sortOrder: 2,
            modifiedBy: "system");

        // Act
        definition.RemoveDataSource(dsId, "admin");

        // Assert
        definition.DataSources.Should().HaveCount(1);
        definition.DataSources[0].Id.Should().Be(second.Id);
        definition.ModifiedBy.Should().Be("admin");
    }

    [Fact]
    public void RemoveLastDataSourceOnActiveReportThrows()
    {
        // Arrange
        ReportDefinition definition = CreateDefinitionWithOneDataSource(out Guid dsId);
        definition.Publish("system");

        // Act
        Action act = () => definition.RemoveDataSource(dsId, "user1");

        // Assert
        act.Should().Throw<ReportingDomainException>()
            .WithMessage("*last data source*");
    }

    [Fact]
    public void RemoveDataSourceWithUnknownIdThrows()
    {
        ReportDefinition definition = CreateDraftDefinition();

        Action act = () => definition.RemoveDataSource(Guid.NewGuid(), "user1");

        act.Should().Throw<ReportingDomainException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public void RemoveDataSourceOnArchivedReportThrows()
    {
        ReportDefinition definition = CreateDefinitionWithOneDataSource(out Guid dsId);
        definition.Publish("system");
        definition.Archive("system");

        Action act = () => definition.RemoveDataSource(dsId, "user1");

        act.Should().Throw<ReportingDomainException>()
            .WithMessage("*immutable*");
    }

    // ── Publish guard: requires at least one data source ──────────────────────

    [Fact]
    public void PublishWithNoDataSourcesThrows()
    {
        ReportDefinition definition = CreateDraftDefinition();

        Action act = () => definition.Publish("system");

        act.Should().Throw<ReportingDomainException>()
            .WithMessage("*at least one data source*");
    }

    [Fact]
    public void PublishWithAtLeastOneDataSourceSucceeds()
    {
        ReportDefinition definition = CreateDefinitionWithOneDataSource(out _);

        Action act = () => definition.Publish("system");

        act.Should().NotThrow();
        definition.Status.Should().Be(ReportStatus.Active);
    }
}
