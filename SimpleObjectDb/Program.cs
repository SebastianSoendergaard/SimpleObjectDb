﻿using System.Diagnostics;
using AutoFixture;
using Basses.SimpleDocumentStore;
using Basses.SimpleDocumentStore.PostgreSql;

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


Console.WriteLine($"Creating ({testObjectAList.Count()}) items in db");
stopwatch.Start();
foreach (var item in testObjectAList)
{
    await db.CreateAsync(item);
}
stopwatch.Stop();
Console.WriteLine($"Items created in db in {stopwatch.Elapsed}");


Console.WriteLine($"Fetching ({testObjectAList.Count()}) items from db one by one");
stopwatch.Restart();
foreach (var item in testObjectAList)
{
    var storedItem = await db.GetByIdAsync<TestObjectA>(item.Id);
}
stopwatch.Stop();
Console.WriteLine($"Items fetched from db in {stopwatch.Elapsed}");


Console.WriteLine($"Updating ({testObjectAList.Count()}) items in db one by one");
stopwatch.Restart();
foreach (var item in testObjectAList)
{
    var newItem = item with { Text = fixture.Create<string>() };
    await db.UpdateAsync(newItem);
}
stopwatch.Stop();
Console.WriteLine($"Items updated in db in {stopwatch.Elapsed}");


Console.WriteLine($"Fetching all ({testObjectAList.Count()}) items from db");
stopwatch.Restart();
var allItems = new List<TestObjectA>();
var enumerator = db.GetAllAsync<TestObjectA>().GetAsyncEnumerator();
try
{
    while (await enumerator.MoveNextAsync()) { allItems.Add(enumerator.Current); }
}
finally
{
    if (enumerator != null) { await enumerator.DisposeAsync(); }
}
stopwatch.Stop();
Console.WriteLine($"All items fetched from db in {stopwatch.Elapsed}");


Console.WriteLine($"Deleting ({testObjectAList.Count()}) items from db one by one");
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

List<TestObjectB> testObjectBList = [];
for (var i = 0; i < 1000; i++)
{
    testObjectBList.Add(new TestObjectB(Guid.NewGuid(), fixture.CreateMany<TestSubObjectB>(500).ToArray()));
}

Console.WriteLine($"Creating ({testObjectBList.Count()}) large items in db");
stopwatch.Start();
foreach (var item in testObjectBList)
{
    await db.CreateAsync(item);
}
stopwatch.Stop();
Console.WriteLine($"Items created in db in {stopwatch.Elapsed}");

Console.WriteLine($"Updating ({testObjectBList.Count()}) large items in db one by one");
stopwatch.Restart();
foreach (var item in testObjectBList)
{
    var newItem = item with { Values = fixture.CreateMany<TestSubObjectB>(500).ToArray() };
    await db.UpdateAsync(newItem);
}
stopwatch.Stop();
Console.WriteLine($"Items updated in db in {stopwatch.Elapsed}");

Console.WriteLine($"Deleting ({testObjectBList.Count()}) large items from db one by one");
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

