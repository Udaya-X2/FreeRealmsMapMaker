using McMaster.Extensions.CommandLineUtils;
using System.Runtime.Versioning;

namespace FreeRealmsMapMaker;

internal class Program
{
    [SupportedOSPlatform("windows")]
    public static void Main(string[] args)
    {
        CommandLineApplication.Execute<MapMaker>(args);
    }
}
