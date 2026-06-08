using Npgsql;
var connStr = ""Host=192.168.14.94;Port=5432;Database=ReportingDb;Username=dev;Password=dev@2025"";
await using var conn = new NpgsqlConnection(connStr);
await conn.OpenAsync();
await using var cmd = new NpgsqlCommand(@""DELETE FROM templates.__ef_migrations_history WHERE ""MigrationId"" = '20260602065056_AddTemplateAssetsAndVersions';"", conn);
int rows = await cmd.ExecuteNonQueryAsync();
Console.WriteLine($""Deleted {rows} row(s) from migration history."");
