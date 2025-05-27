using FreeRealmsMapMaker.Dds;
using McMaster.Extensions.CommandLineUtils;
using ShellProgressBar;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace FreeRealmsMapMaker;

/// <summary>
/// The driver class of the command-line <see cref="FreeRealmsMapMaker"/>.
/// </summary>
[SupportedOSPlatform("windows")]
public partial class MapMaker
{
    /// <summary>
    /// Gets the input directory containing the TileInfo.txt/tile files.
    /// </summary>
    [Argument(0, Name = "InputDirectory", Description = "The input directory containing the TileInfo.txt/tile files.")]
    [DirectoryExists]
    [Required]
    public string InputDirectory { get; } = "";

    /// <summary>
    /// Gets the output directory to put the map files.
    /// </summary>
    [Argument(1, Name = "OutputDirectory", Description = "The output directory to put the map files.")]
    [FileNotExists]
    [Required]
    public string OutputDirectory { get; } = "./maps";

    /// <summary>
    /// Gets the image format of the output map.
    /// </summary>
    [Option(ShortName = "f", Description = "The image format of the output map.")]
    [Required]
    public ImageFormat Format { get; } = ImageFormat.Png;

    /// <summary>
    /// Gets the maximum number of threads to use.
    /// </summary>
    [Option(ShortName = "m", Description = "The maximum number of threads to use."
                                           + "\nBy default (-1), there is no upper limit.")]
    [Range(-1, int.MaxValue)]
    public int MaxThreads { get; } = -1;

    /// <summary>
    /// Gets the file extension of the output map.
    /// </summary>
    private string Extension => _extension ??= $".{Format.ToString().ToLower()}";

    private string? _extension;

    [GeneratedRegex(@"[^/\\]*?_Tile_[^/\\]*?\.dds$", RegexOptions.IgnoreCase | RegexOptions.RightToLeft, "en-US")]
    private static partial Regex TileRegex();

    /// <summary>
    /// The entry point of the command line <see cref="FreeRealmsMapMaker"/>, following command parsing.
    /// </summary>
    /// <returns>The process exit code.</returns>
    public void OnExecute()
    {
        if (!Directory.Exists(OutputDirectory)) Directory.CreateDirectory(OutputDirectory);

        List<Map> maps = [];
        HashSet<string> tiles = [];

        foreach (string path in Directory.EnumerateFiles(InputDirectory))
        {
            // Create map specifications from the TileInfo.txt files.
            if (path.EndsWith("TileInfo.txt", StringComparison.OrdinalIgnoreCase))
            {
                maps.Add(new Map(path));
            }
            // Keep track of existing tile .dds files.
            else if (TileRegex().IsMatch(path))
            {
                tiles.Add(Path.GetFileName(path).ToLower());
            }
        }

        // Remove non-existent tiles and remove maps that don't have any tiles.
        maps.ForEach(x => x.Tiles.RemoveAll(x => !tiles.Contains(x.Name.ToLower())));
        maps.RemoveAll(x => x.Tiles.Count == 0);

        if (maps.Count == 0)
        {
            Console.WriteLine("No tiles found.");
            return;
        }

        // Create maps from the collected tiles.
        ConsoleColor[] colors = [.. Enum.GetValues<ConsoleColor>().Except([ConsoleColor.Black, ConsoleColor.White])];
        int i = 0;
        using ProgressBar pbarMap = new(maps.Count, "Creating maps", new ProgressBarOptions
        {
            ProgressCharacter = '─'
        });
        Parallel.ForEach(maps, new ParallelOptions { MaxDegreeOfParallelism = MaxThreads }, (map) =>
        {
            CreateMap(pbarMap, map, colors[i++ % colors.Length]);
        });
    }

    /// <summary>
    /// Creates the specified <paramref name="map"/> using the tiles in <see cref="InputDirectory"/>.
    /// </summary>
    private void CreateMap(ProgressBar pbarMap, Map map, ConsoleColor color)
    {
        map.ComputeBorders(out int minX, out int minZ, out int maxX, out int maxZ);
        using Bitmap bitmap = new(maxZ - minZ, maxX - minX);
        using Graphics g = Graphics.FromImage(bitmap);
        using ChildProgressBar pbarTiles = pbarMap.Spawn(map.Tiles.Count, "Adding tiles", new ProgressBarOptions
        {
            ProgressCharacter = '─',
            CollapseWhenFinished = true,
            ForegroundColor = color
        });

        foreach (Tile tile in map.Tiles)
        {
            UpdateProgress(pbarTiles, $"Adding tile: {tile.Name}");
            DdsImage ddsImage = new(Path.Combine(InputDirectory, tile.Name));
            using Image image = ddsImage.Images[0];
            int x = tile.Z - minZ;
            int y = bitmap.Height - tile.X + minX - tile.Height;
            g.DrawImage(image, x, y, tile.Width, tile.Height);
        }

        bitmap.Save($"{OutputDirectory}/{map.Name}{Extension}", Format);
        UpdateProgress(pbarMap, $"Created map: {map.Name}{Extension}");
    }

    /// <summary>
    /// Moves the progress bar one tick and displays the specified message.
    /// </summary>
    private static void UpdateProgress(ProgressBarBase progressBar, string message)
        => progressBar.Tick($"({progressBar.CurrentTick + 1}/{progressBar.MaxTicks}) {message}");
}
