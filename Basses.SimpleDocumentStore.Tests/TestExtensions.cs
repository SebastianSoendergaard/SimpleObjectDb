using System.Text.Json;

namespace Basses.SimpleDocumentStore.Tests;

internal static class TestExtensions
{
    public static string AsString<T>(this T obj)
    {
        return JsonSerializer.Serialize(obj);
    }
}
