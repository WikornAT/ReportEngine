using Npgsql;
const string cs = "Host=192.168.14.94;Port=5432;Database=ReportingDb;Username=dev;Password=dev@2025";
await using var conn = new NpgsqlConnection(cs);
await conn.OpenAsync();
await using var cmd = new NpgsqlCommand("ALTER TABLE reporting.render_logs ALTER COLUMN \"ParametersJson\" TYPE jsonb USING \"ParametersJson\"::jsonb", conn);
await cmd.ExecuteNonQueryAsync();
Console.WriteLine("Done. ParametersJson is now jsonb.");
