using McMaster.Extensions.CommandLineUtils;

namespace FreeRealmsMapMaker;

internal class Program
{
    public static void Main(string[] args)
    {
        CommandLineApplication.Execute<MapMaker>(args);
    }
}
