using CP.Migrator.Data.SQLite;
using CP.Migrator.Test.Shared;
using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;

namespace CP.Migrator.Test.Integration.SQLite;

public class DatabaseInitializerTests
{
	[Fact]
	public void Initialise_CreatesAllFourTables()
	{
		var factory = new FixedConnectionFactory();

		// Keep a connection open so the in-memory DB persists.
		using var keepAlive = factory.CreateConnection();
		keepAlive.Open();

		var initializer = new SQLiteDatabaseInitializer(factory);
		initializer.Initialise();

		var tables = keepAlive.Query<string>(
			"SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name != 'SchemaVersions' ORDER BY name;")
			.ToList();

		Assert.Equal(4, tables.Count);
		Assert.Contains("tblPatient", tables);
		Assert.Contains("tblInvoice", tables);
		Assert.Contains("tblTreatment", tables);
		Assert.Contains("tblInvoiceLineItem", tables);
	}

	[Fact]
	public void Initialise_CanBeCalledMultipleTimes_WithoutError()
	{
		var factory = new FixedConnectionFactory();

		using var keepAlive = factory.CreateConnection();
		keepAlive.Open();

		var initializer = new SQLiteDatabaseInitializer(factory);

		initializer.Initialise();
		initializer.Initialise(); // Should not throw

		var tableCount = keepAlive.ExecuteScalar<int>(
			"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name != 'SchemaVersions';");

		Assert.Equal(4, tableCount);
	}

	[Fact]
	public void Initialise_EnforcesForeignKeys()
	{
		var factory = new FixedConnectionFactory();

		using var keepAlive = factory.CreateConnection();
		keepAlive.Open();

		var initializer = new SQLiteDatabaseInitializer(factory);
		initializer.Initialise();

		// Inserting a treatment
		var ex = Assert.Throws<SqliteException>(() =>
		{
			using var conn = factory.CreateConnection();
			conn.Open();
			conn.Execute(
				@"INSERT INTO tblTreatment 
				  (TreatmentIdentifier, Description, ItemCode, Quantity, Fee, PatientId)
				  VALUES ('T1', 'Filling', 'D2140', 1, 100.0, 999);");
		});

		Assert.Contains("FOREIGN KEY", ex.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void ConnectionFactory_CreateConnection_ReturnsValidConnection()
	{
		var factory = new FixedConnectionFactory();

		using var connection = factory.CreateConnection();
		connection.Open();

		Assert.Equal(ConnectionState.Open, connection.State);
	}
}
