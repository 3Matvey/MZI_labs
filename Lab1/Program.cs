using System.Text;

namespace MziLab;

internal static class Program
{
    public static void Main()
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;
        new ConsoleMenu().Run();
    }
}
