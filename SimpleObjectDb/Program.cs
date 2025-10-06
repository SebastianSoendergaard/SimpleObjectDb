using Basses.SimpleDocumentStore;
using Basses.SimpleDocumentStore.Files;
using Basses.SimpleDocumentStore.InMemory;
using Basses.SimpleDocumentStore.PostgreSql;
using Basses.SimpleDocumentStore.SqlServer;
using SimpleObjectDb;

DocumentStoreConfiguration config = new();
config.RegisterDataType<TestObjectA>(i => i.Id);
config.RegisterDataType<TestObjectB>(i => i.Id);

IDocumentStore inMemoryDb = new InMemoryDocumentStore(config);
PerformanceStats.Run(inMemoryDb).GetAwaiter().GetResult();

IDocumentStore fileDb = new FileDocumentStore(@"C:\Temp\SimpleObjectDb\", config);
PerformanceStats.Run(fileDb).GetAwaiter().GetResult();

var sqlServerConnectionString = @"Server=localhost,9001;Database=SimpleObjectDb;User Id=sa;Password=Passw0rd;Pooling=true;TrustServerCertificate=True;";
IDocumentStore sqlserverDb = new SqlServerDocumentStore(sqlServerConnectionString, config);
PerformanceStats.Run(sqlserverDb).GetAwaiter().GetResult();

var postgresConnectionString = @"Server=localhost;Port=9002;User Id=postgres;Password=Passw0rd;Database=simple_object_db;";
IDocumentStore postgresDb = new PostgreSqlDocumentStore(postgresConnectionString, config);
PerformanceStats.Run(postgresDb).GetAwaiter().GetResult();


internal record TestObjectA(int Id, string Text, List<TestSubObjectA> SubObjects);

internal record TestSubObjectA(int Id, string Text);

internal record TestObjectB(Guid Id, TestSubObjectB[] Values);

internal record TestSubObjectB(string A, string B, string C, string D, string E);

