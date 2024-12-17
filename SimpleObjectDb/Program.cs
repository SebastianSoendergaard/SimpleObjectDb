using System.Diagnostics;
using AutoFixture;
using SimpleFileDatabase;
using SimpleObjectDb.db;
using SimpleObjectDb.db.postgresql;

Fixture fixture = new();
Stopwatch stopwatch = new();


SimpleObjectDbConfiguration config = new();
config.RegisterDataType<TestObjectA>(i => i.Id);
config.RegisterDataType<TestObjectB>(i => i.Id);

//ISimpleObjectDb db = new SimpleFileObjectDb(@"C:\Temp\SimpleObjectDb\", config);

//var sqlServerConnectionString = @"Server=localhost,9001;Database=SimpleObjectDb;User Id=sa;Password=Passw0rd;Pooling=true;TrustServerCertificate=True;";
//SimpleSqlServerObjectDb.CreateIfNotExist(sqlServerConnectionString, config);
//ISimpleObjectDb db = new SimpleSqlServerObjectDb(sqlServerConnectionString, config);

var postgresConnectionString = @"Server=localhost;Port=9002;User Id=postgres;Password=Passw0rd;Database=simple_object_db;";
SimplePostgreSqlObjectDb.CreateIfNotExist(postgresConnectionString, config);
ISimpleObjectDb db = new SimplePostgreSqlObjectDb(postgresConnectionString, config);

// ==============================
// Test with many small objects
// ==============================

var testObjectAList = fixture.CreateMany<TestObjectA>(10000);


Console.WriteLine("Creating many items in db");
stopwatch.Start();
foreach (var item in testObjectAList)
{
    await db.CreateAsync(item);
}
stopwatch.Stop();
Console.WriteLine($"Items created in db in {stopwatch.Elapsed}");


Console.WriteLine("Fetching many items from db one by one");
stopwatch.Restart();
foreach (var item in testObjectAList)
{
    var storedItem = await db.GetByIdAsync<TestObjectA>(item.Id);
}
stopwatch.Stop();
Console.WriteLine($"Items fetched from db in {stopwatch.Elapsed}");


Console.WriteLine("Updating many items in db one by one");
stopwatch.Restart();
foreach (var item in testObjectAList)
{
    var newItem = item with { Text = fixture.Create<string>() };
    await db.UpdateAsync(newItem);
}
stopwatch.Stop();
Console.WriteLine($"Items updated in db in {stopwatch.Elapsed}");


Console.WriteLine("Fetching all (many) items from db");
stopwatch.Restart();
List<TestObjectA> allItems = new();
await foreach (var item in db.GetAllAsync<TestObjectA>())
{
    allItems.Add(item);
}
stopwatch.Stop();
Console.WriteLine($"All items fetched from db in {stopwatch.Elapsed}");


Console.WriteLine("Deleting many items from db one by one");
stopwatch.Restart();
foreach (var item in testObjectAList)
{
    await db.DeleteByIdAsync<TestObjectA>(item.Id);
}
stopwatch.Stop();
Console.WriteLine($"Items deleted from db in {stopwatch.Elapsed}");


// ==============================
// Test with few large objects
// ==============================

List<TestObjectB> testObjectBList = new();
for (var i = 0; i < 1000; i++)
{
    testObjectBList.Add(new TestObjectB(Guid.NewGuid(), fixture.CreateMany<TestSubObjectB>(500).ToArray()));
}

Console.WriteLine("Creating large items in db");
stopwatch.Start();
foreach (var item in testObjectBList)
{
    await db.CreateAsync(item);
}
stopwatch.Stop();
Console.WriteLine($"Items created in db in {stopwatch.Elapsed}");

Console.WriteLine("Updating large items in db one by one");
stopwatch.Restart();
foreach (var item in testObjectBList)
{
    var newItem = item with { Values = fixture.CreateMany<TestSubObjectB>(500).ToArray() };
    await db.UpdateAsync(newItem);
}
stopwatch.Stop();
Console.WriteLine($"Items updated in db in {stopwatch.Elapsed}");

Console.WriteLine("Deleting large items from db one by one");
stopwatch.Restart();
foreach (var item in testObjectBList)
{
    await db.DeleteByIdAsync<TestObjectB>(item.Id);
}
stopwatch.Stop();
Console.WriteLine($"Items deleted from db in {stopwatch.Elapsed}");



internal record TestObjectA(int Id, string Text, List<TestSubObjectA> SubObjects);

internal record TestSubObjectA(int Id, string Text);

internal record TestObjectB(Guid Id, TestSubObjectB[] Values);

internal record TestSubObjectB(string A, string B, string C, string D, string E);

