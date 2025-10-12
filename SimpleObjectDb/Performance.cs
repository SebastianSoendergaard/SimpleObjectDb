using System.Diagnostics;
using AutoFixture;
using Basses.SimpleDocumentStore;
using Basses.SimpleDocumentStore.Files;
using Basses.SimpleDocumentStore.InMemory;
using Basses.SimpleDocumentStore.PostgreSql;
using Basses.SimpleDocumentStore.SqlServer;

namespace SimpleObjectDb;

internal static class Performance
{
    public static async Task Run()
    {
        Stopwatch stopwatch = new();

        LogAction($"Creating test objects");
        stopwatch.Start();
        var testData = CreateTestData();
        stopwatch.Stop();
        LogExecutionTime(stopwatch);

        LogAction($"Setting up store options");
        stopwatch.Restart();
        DocumentStoreConfiguration config = new();
        config.RegisterDataType<TestObjectA>(i => i.Id);
        config.RegisterDataType<TestObjectB>(i => i.Id);

        IDocumentStore inMemoryDb = new InMemoryDocumentStore(config);
        IDocumentStore fileDb = new FileDocumentStore(Constants.FileConnectionString, config);
        IDocumentStore sqlserverDb = new SqlServerDocumentStore(Constants.SqlServerConnectionString, config);
        IDocumentStore postgresDb = new PostgreSqlDocumentStore(Constants.PostgresConnectionString, config);
        stopwatch.Stop();
        LogExecutionTime(stopwatch);
        Console.WriteLine("");
        Console.WriteLine("");

        await RunTest(inMemoryDb, testData);
        await RunTest(fileDb, testData);
        await RunTest(sqlserverDb, testData);
        await RunTest(postgresDb, testData);
    }

    private static async Task RunTest(IDocumentStore store, TestData testData)
    {
        Stopwatch stopwatch = new();
        Stopwatch totalStopwatch = new();

        // ==============================
        // Test with many small objects
        // ==============================

        Console.WriteLine("----------------------------------------------------------------------------");
        Console.WriteLine($"Starting performance test of: {store.GetType().Name}");
        Console.WriteLine("----------------------------------------------------------------------------");

        Console.WriteLine($"Executing tests...");
        totalStopwatch.Start();

        LogAction($"Creating {testData.SmallObjects.Count()} small items in db");
        stopwatch.Restart();
        foreach (var item in testData.SmallObjects)
        {
            await store.CreateAsync(item);
        }
        stopwatch.Stop();
        LogExecutionTime(stopwatch);


        LogAction($"Fetching {testData.SmallObjects.Count()} small items from db one by one");
        stopwatch.Restart();
        foreach (var item in testData.SmallObjects)
        {
            var storedItem = await store.GetByIdAsync<TestObjectA>(item.Id);
        }
        stopwatch.Stop();
        LogExecutionTime(stopwatch);


        LogAction($"Fetching {testData.SmallObjects.Count()} small items from db at once");
        stopwatch.Restart();
        var allItems = new List<TestObjectA>();
        await foreach (var item in store.GetAllAsync<TestObjectA>())
        {
            allItems.Add(item);
        }
        stopwatch.Stop();
        LogExecutionTime(stopwatch);


        LogAction($"Updating {testData.SmallObjects.Count()} small items in db one by one");
        stopwatch.Restart();
        foreach (var item in testData.SmallObjects)
        {
            var newItem = testData.SmallObjectUpdates[item.Id];
            await store.UpdateAsync(newItem);
        }
        stopwatch.Stop();
        LogExecutionTime(stopwatch);


        LogAction($"Deleting {testData.SmallObjects.Count()} small items from db one by one");
        stopwatch.Restart();
        foreach (var item in testData.SmallObjects)
        {
            await store.DeleteByIdAsync<TestObjectA>(item.Id);
        }
        stopwatch.Stop();
        LogExecutionTime(stopwatch);


        // ==============================
        // Test with few large objects
        // ==============================

        LogAction($"Creating {testData.LargeObjects.Count()} large items in db");
        stopwatch.Start();
        foreach (var item in testData.LargeObjects)
        {
            await store.CreateAsync(item);
        }
        stopwatch.Stop();
        LogExecutionTime(stopwatch);
        Thread.Sleep(1000);

        LogAction($"Updating {testData.LargeObjects.Count()} large items in db one by one");
        stopwatch.Restart();
        foreach (var item in testData.LargeObjects)
        {
            var newItem = testData.LargeObjectUpdates[item.Id];
            await store.UpdateAsync(newItem);
        }
        stopwatch.Stop();
        LogExecutionTime(stopwatch);
        Thread.Sleep(1000);

        LogAction($"Deleting {testData.LargeObjects.Count()} large items from db one by one");
        stopwatch.Restart();
        foreach (var item in testData.LargeObjects)
        {
            await store.DeleteByIdAsync<TestObjectB>(item.Id);
        }
        stopwatch.Stop();
        LogExecutionTime(stopwatch);
        Thread.Sleep(1000);

        LogAction("Total execution time");
        LogExecutionTime(totalStopwatch);
        Console.WriteLine("");
        Console.WriteLine("");
    }

    private static TestData CreateTestData()
    {
        int smallTestObjectCount = 10000;
        int largeTestObjectCount = 10;

        Fixture fixture = new();
        Console.WriteLine("");

        var smallTestObjects = new List<TestObjectA>();
        var smallUpdates = new List<TestObjectA>();
        for (int i = 0; i < smallTestObjectCount; i++)
        {
            var obj = new TestObjectA(i, fixture.Create<string>(), fixture.CreateMany<TestSubObjectA>(3).ToList());
            var updatedObj = obj with { Text = fixture.Create<string>() };
            smallTestObjects.Add(obj);
            smallUpdates.Add(updatedObj);
            OverrideCurrentConsoleLine($"Created small object: {i + 1} of {smallTestObjectCount}");
        }

        Console.WriteLine("");

        var largeTestObjects = new List<TestObjectB>();
        var largeUpdates = new List<TestObjectB>();
        for (var i = 0; i < largeTestObjectCount; i++)
        {
            var obj = new TestObjectB(Guid.NewGuid(), fixture.CreateMany<TestSubObjectB>(500).ToArray());
            var updatedObj = obj with { Values = fixture.CreateMany<TestSubObjectB>(500).ToArray() };
            largeTestObjects.Add(obj);
            largeUpdates.Add(updatedObj);
            OverrideCurrentConsoleLine($"Created large object: {i + 1} of {largeTestObjectCount}");
        }

        Console.WriteLine("");
        LogAction("Objects created in");
        return new TestData(smallTestObjects, smallUpdates.ToDictionary(x => x.Id), largeTestObjects, largeUpdates.ToDictionary(x => x.Id));
    }

    private static void OverrideCurrentConsoleLine(string text)
    {
        int currentLineCursor = Console.CursorTop;
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(text);
        Console.SetCursorPosition(0, currentLineCursor);
    }

    private static void LogAction(string text)
    {
        var logText = $"{text}...".PadRight(60);
        Console.Write(logText);
    }

    private static void LogExecutionTime(Stopwatch stopwatch)
    {
        Console.WriteLine(stopwatch.Elapsed);
    }

    private record TestData(IEnumerable<TestObjectA> SmallObjects, IDictionary<int, TestObjectA> SmallObjectUpdates, IEnumerable<TestObjectB> LargeObjects, IDictionary<Guid, TestObjectB> LargeObjectUpdates);
}
