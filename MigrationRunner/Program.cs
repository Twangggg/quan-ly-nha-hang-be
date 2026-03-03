using Npgsql;

// Connect to postgres maintenance DB (not FoodHubDb, so we can drop it)
var adminConn = "Host=127.0.0.1;Port=5432;Database=postgres;Username=postgres;Password=123456@";

await using var conn = new NpgsqlConnection(adminConn);
await conn.OpenAsync();
Console.WriteLine("Connected to postgres (admin)");

// Terminate all connections to FoodHubDb
await using var killCmd = new NpgsqlCommand(
    "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = 'FoodHubDb' AND pid <> pg_backend_pid();",
    conn);
await killCmd.ExecuteNonQueryAsync();
Console.WriteLine("Terminated existing connections to FoodHubDb");

// Drop
await using var dropCmd = new NpgsqlCommand("DROP DATABASE IF EXISTS \"FoodHubDb\";", conn);
await dropCmd.ExecuteNonQueryAsync();
Console.WriteLine("Dropped FoodHubDb");

// Recreate
await using var createCmd = new NpgsqlCommand("CREATE DATABASE \"FoodHubDb\";", conn);
await createCmd.ExecuteNonQueryAsync();
Console.WriteLine("Created FoodHubDb — ready for migrations!");

