using SimpleObjectDb;

Console.WriteLine("Enter program to execute, valid options:");
Console.WriteLine("  1: performance");
Console.WriteLine("  2: serialization");
var input = Console.ReadKey();
Console.WriteLine("");
Console.WriteLine("");

switch (input.KeyChar)
{
    case '1':
        Performance.Run().GetAwaiter().GetResult();
        break;

    case '2':
        Serialization.Run().GetAwaiter().GetResult();
        break;

    default:
        Console.WriteLine("Invalid input!");
        break;
}







