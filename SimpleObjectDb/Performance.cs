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
        DocumentStoreConfiguration config = new();
        config.RegisterDataType<TestObjectA>(i => i.Id);
        config.RegisterDataType<TestObjectB>(i => i.Id);

        IDocumentStore inMemoryDb = new InMemoryDocumentStore(config);
        IDocumentStore fileDb = new FileDocumentStore(Constants.FileConnectionString, config);
        IDocumentStore sqlserverDb = new SqlServerDocumentStore(Constants.SqlServerConnectionString, config);
        IDocumentStore postgresDb = new PostgreSqlDocumentStore(Constants.PostgresConnectionString, config);

        await RunTest(inMemoryDb);
        await RunTest(fileDb);
        await RunTest(sqlserverDb);
        await RunTest(postgresDb);
    }

    public static async Task RunTest(IDocumentStore store)
    {
        Fixture fixture = new();
        Stopwatch stopwatch = new();
        Stopwatch totalStopwatch = new();

        // ==============================
        // Test with many small objects
        // ==============================

        Console.WriteLine("----------------------------------------------------------------------------");
        Console.WriteLine($"Starting performance test of: {store.GetType().Name}");
        Console.WriteLine("----------------------------------------------------------------------------");

        LogAction($"Creating test objects");
        stopwatch.Start();
        var smallTestObjects = fixture.CreateMany<TestObjectA>(10000);

        List<TestObjectB> largeTestObjects = [];
        for (var i = 0; i < 1000; i++)
        {
            largeTestObjects.Add(new TestObjectB(Guid.NewGuid(), fixture.CreateMany<TestSubObjectB>(500).ToArray()));
        }
        stopwatch.Stop();
        LogExecutionTime(stopwatch);

        Console.WriteLine($"Executing tests...");
        totalStopwatch.Start();

        LogAction($"Creating {smallTestObjects.Count()} small items in db");
        stopwatch.Restart();
        foreach (var item in smallTestObjects)
        {
            await store.CreateAsync(item);
        }
        stopwatch.Stop();
        LogExecutionTime(stopwatch);


        LogAction($"Fetching {smallTestObjects.Count()} small items from db one by one");
        stopwatch.Restart();
        foreach (var item in smallTestObjects)
        {
            var storedItem = await store.GetByIdAsync<TestObjectA>(item.Id);
        }
        stopwatch.Stop();
        LogExecutionTime(stopwatch);


        LogAction($"Updating {smallTestObjects.Count()} small items in db one by one");
        stopwatch.Restart();
        foreach (var item in smallTestObjects)
        {
            var newItem = item with { Text = fixture.Create<string>() };
            await store.UpdateAsync(newItem);
        }
        stopwatch.Stop();
        LogExecutionTime(stopwatch);


        LogAction($"Fetching {smallTestObjects.Count()} small items from db at once");
        stopwatch.Restart();
        var allItems = new List<TestObjectA>();
        var enumerator = store.GetAllAsync<TestObjectA>().GetAsyncEnumerator();
        try
        {
            while (await enumerator.MoveNextAsync()) { allItems.Add(enumerator.Current); }
        }
        finally
        {
            if (enumerator != null) { await enumerator.DisposeAsync(); }
        }
        stopwatch.Stop();
        LogExecutionTime(stopwatch);


        LogAction($"Deleting {smallTestObjects.Count()} small items from db one by one");
        stopwatch.Restart();
        foreach (var item in smallTestObjects)
        {
            await store.DeleteByIdAsync<TestObjectA>(item.Id);
        }
        stopwatch.Stop();
        LogExecutionTime(stopwatch);


        // ==============================
        // Test with few large objects
        // ==============================

        LogAction($"Creating {largeTestObjects.Count()} large items in db");
        stopwatch.Start();
        foreach (var item in largeTestObjects)
        {
            await store.CreateAsync(item);
        }
        stopwatch.Stop();
        LogExecutionTime(stopwatch);

        LogAction($"Updating {largeTestObjects.Count()} large items in db one by one");
        stopwatch.Restart();
        foreach (var item in largeTestObjects)
        {
            var newItem = item with { Values = fixture.CreateMany<TestSubObjectB>(500).ToArray() };
            await store.UpdateAsync(newItem);
        }
        stopwatch.Stop();
        LogExecutionTime(stopwatch);

        LogAction($"Deleting {largeTestObjects.Count()} large items from db one by one");
        stopwatch.Restart();
        foreach (var item in largeTestObjects)
        {
            await store.DeleteByIdAsync<TestObjectB>(item.Id);
        }
        stopwatch.Stop();
        LogExecutionTime(stopwatch);

        LogAction("Total execution time");
        LogExecutionTime(totalStopwatch);

        Console.WriteLine("");
        Console.WriteLine("");
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
}
