namespace NoNameLogger;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting...");
        await Task.Delay(1000);
        Console.WriteLine("Done...");
    }
}