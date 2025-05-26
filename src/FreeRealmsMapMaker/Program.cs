using McMaster.Extensions.CommandLineUtils;

namespace FreeRealmsMapMaker;

public class Program
{
    public static void Main(string[] args)
    {
        CommandLineApplication.Execute<MapMaker>(args);
    }
}
