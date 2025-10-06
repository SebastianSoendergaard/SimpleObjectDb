using System.Diagnostics;
using AutoFixture;
using Basses.SimpleDocumentStore;

namespace SimpleObjectDb;

public static class PerformanceStats
{
    public static async Task Run(IDocumentStore db)
    {
        Fixture fixture = new();
        Stopwatch stopwatch = new();
        Stopwatch totalStopwatch = new();

        // ==============================
        // Test with many small objects
        // ==============================

        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine($"Starting performance test of: {db.GetType().Name}");
        Console.WriteLine("--------------------------------------------------------------------------------");

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
            await db.CreateAsync(item);
        }
        stopwatch.Stop();
        LogExecutionTime(stopwatch);


        LogAction($"Fetching {smallTestObjects.Count()} small items from db one by one");
        stopwatch.Restart();
        foreach (var item in smallTestObjects)
        {
            var storedItem = await db.GetByIdAsync<TestObjectA>(item.Id);
        }
        stopwatch.Stop();
        LogExecutionTime(stopwatch);


        LogAction($"Updating {smallTestObjects.Count()} small items in db one by one");
        stopwatch.Restart();
        foreach (var item in smallTestObjects)
        {
            var newItem = item with { Text = fixture.Create<string>() };
            await db.UpdateAsync(newItem);
        }
        stopwatch.Stop();
        LogExecutionTime(stopwatch);


        LogAction($"Fetching {smallTestObjects.Count()} small items from db at once");
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
        LogExecutionTime(stopwatch);


        LogAction($"Deleting {smallTestObjects.Count()} small items from db one by one");
        stopwatch.Restart();
        foreach (var item in smallTestObjects)
        {
            await db.DeleteByIdAsync<TestObjectA>(item.Id);
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
            await db.CreateAsync(item);
        }
        stopwatch.Stop();
        LogExecutionTime(stopwatch);

        LogAction($"Updating {largeTestObjects.Count()} large items in db one by one");
        stopwatch.Restart();
        foreach (var item in largeTestObjects)
        {
            var newItem = item with { Values = fixture.CreateMany<TestSubObjectB>(500).ToArray() };
            await db.UpdateAsync(newItem);
        }
        stopwatch.Stop();
        LogExecutionTime(stopwatch);

        LogAction($"Deleting {largeTestObjects.Count()} large items from db one by one");
        stopwatch.Restart();
        foreach (var item in largeTestObjects)
        {
            await db.DeleteByIdAsync<TestObjectB>(item.Id);
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
