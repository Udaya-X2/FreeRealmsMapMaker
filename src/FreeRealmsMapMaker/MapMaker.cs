using McMaster.Extensions.CommandLineUtils;
using ShellProgressBar;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.Versioning;

namespace FreeRealmsMapMaker;

/// <summary>
/// The driver class of the command-line <see cref="FreeRealmsMapMaker"/>.
/// </summary>
public class MapMaker
{
    /// <summary>
    /// Gets the input directory containing the TileInfo.txt/tile files.
    /// </summary>
    [Argument(0, Name = "InputDirectory", Description = "The input directory containing the TileInfo.txt/tile files.")]
    [Required]
    [DirectoryExists]
    public string InputDirectory { get; } = "";

    /// <summary>
    /// Gets the output directory to put the map files.
    /// </summary>
    [Argument(1, Name = "OutputDirectory", Description = "The output directory to put the map files.")]
    [FileNotExists]
    public string OutputDirectory { get; } = "./maps";

    /// <summary>
    /// Gets the directory containing the intermediate tile files.
    /// </summary>
    [Option(ShortName = "t", Description = "The directory containing the intermediate tile files.")]
    public string TileDirectory { get; } = "./tiles";

    /// <summary>
    /// Gets the image format of the output map.
    /// </summary>
    [Option(ShortName = "f", Description = "The image format of the output map.")]
    public string Format { get; } = ".png";

    /// <summary>
    /// Gets whether to automatically answer yes to any question.
    /// </summary>
    [Option(ShortName = "y", Description = "Automatically answer yes to any question.")]
    public bool AnswerYes { get; }

    /// <summary>
    /// The default progress bar options.
    /// </summary>
    private static readonly ProgressBarOptions PbarOptions = new() { ProgressCharacter = '─' };

    /// <summary>
    /// The entry point of the command line <see cref="FreeRealmsMapMaker"/>, following command parsing.
    /// </summary>
    /// <returns>The process exit code.</returns>
    [SupportedOSPlatform("windows")]
    public void OnExecute()
    {
        if (!Directory.Exists(OutputDirectory)) Directory.CreateDirectory(OutputDirectory);
        if (!Directory.Exists(TileDirectory)) Directory.CreateDirectory(TileDirectory);

        // Create map specifications from the TileInfo.txt files.
        List<Map> maps = [.. Directory.EnumerateFiles(InputDirectory, "*_TileInfo.txt")
                                      .Select(x => new Map(x))];

        // Create a list of new tiles by comparing the input directory to the tiles directory.
        HashSet<string> tiles = [.. maps.SelectMany(x => x.Tiles)
                                        .Select(x => x.Name.ToLower())];
        tiles.ExceptWith(Directory.EnumerateFiles(TileDirectory)
                                  .Select(x => $"{Path.GetFileNameWithoutExtension(x)}.dds".ToLower()));
        List<FileInfo> newTiles = [.. new DirectoryInfo(InputDirectory).EnumerateFiles("*_Tile_*.dds")
                                                                       .Where(x => tiles.Contains(x.Name.ToLower()))];

        // Copy new tiles to the tiles directory and convert them from DDS -> PNG.
        CopyFiles(newTiles, TileDirectory);
        ConvertDdsFiles(TileDirectory);

        // Create a mapping from lowercase tile .dds file -> tile .png file.
        Dictionary<string, FileInfo> nameToTile = new DirectoryInfo(TileDirectory).EnumerateFiles()
            .ToDictionary(x => Path.ChangeExtension(x.Name, ".dds").ToLower());

        // Remove non-existent tiles and remove maps that don't have any tiles.
        maps.ForEach(x => x.Tiles.RemoveAll(x => !nameToTile.ContainsKey(x.Name.ToLower())));
        maps.RemoveAll(x => x.Tiles.Count == 0);

        if (maps.Count == 0)
        {
            Console.WriteLine("No tiles found.");
            return;
        }

        using ProgressBar pbarMap = new(maps.Count, "Creating maps", PbarOptions);

        // Create maps from the collected tiles.
        foreach (Map map in maps)
        {
            CreateMap(nameToTile, pbarMap, map);
        }
    }

    /// <summary>
    /// Prompts the user to copy the specified files to the given directory.
    /// </summary>
    private void CopyFiles(List<FileInfo> files, string dir)
    {
        if (files.Count == 0) return;

        if (!AnswerYes)
        {
            files.ForEach(x => Console.WriteLine(x.Name));
        }

        if (PromptUser($"\nCopy the above {files.Count} files to '{dir}'?"))
        {
            using ProgressBar pbar = new(files.Count, "Copying files", PbarOptions);

            foreach (FileInfo tile in files)
            {
                tile.CopyTo(Path.Combine(dir, tile.Name));
                UpdateProgress(pbar, $"Copying {tile.Name}");
            }
        }
    }

    /// <summary>
    /// Converts image files in the specified directory from DDS -> PNG.
    /// </summary>
    private static void ConvertDdsFiles(string path)
    {
        List<string> ddsFiles = [.. Directory.EnumerateFiles(path)
                                             .Where(x => x.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))];

        if (ddsFiles.Count == 0) return;

        string converter = Where("magick.exe") ?? throw new FileNotFoundException("Cannot find ImageMagick binary.");
        using ProgressBar pbar = new(ddsFiles.Count, "Converting tiles", PbarOptions);

        foreach (string ddsFile in ddsFiles)
        {
            Process.Start(converter, $"{ddsFile} {Path.ChangeExtension(ddsFile, ".png")}").WaitForExit();
            pbar.Tick();
        }

        foreach (string ddsFile in ddsFiles)
        {
            File.Delete(ddsFile);
        }
    }

    /// <summary>
    /// Creates the specified <paramref name="map"/> using the tiles from <paramref name="nameToTile"/>.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private void CreateMap(Dictionary<string, FileInfo> nameToTile, ProgressBar pbarMap, Map map)
    {
        UpdateProgress(pbarMap, $"Creating map: {map.Name}");
        map.ComputeMapBorders(out int minX, out int minZ, out int maxX, out int maxZ);
        using Bitmap bitmap = new(maxZ - minZ, maxX - minX);
        using Graphics g = Graphics.FromImage(bitmap);
        using ChildProgressBar pbarTiles = pbarMap.Spawn(map.Tiles.Count, "Creating tiles", new ProgressBarOptions
        {
            ProgressCharacter = '─',
            CollapseWhenFinished = true
        });

        foreach (Tile tile in map.Tiles)
        {
            UpdateProgress(pbarTiles, $"Adding tile: {tile.Name}");
            FileInfo tileFile = nameToTile[tile.Name.ToLower()];
            using Image image = Image.FromFile(tileFile.FullName);
            int x = tile.Z - minZ;
            int y = bitmap.Height - tile.X + minX - tile.Height;
            g.DrawImage(image, x, y, tile.Width, tile.Height);
        }

        bitmap.Save($"{OutputDirectory}/{map.Name}{Format}");
    }

    /// <summary>
    /// Locates the given file by searching the current directory and paths specified in the PATH environment variable.
    /// </summary>
    /// <returns>The first location of the given file, or <see langword="null"/> if the file cannot be found.</returns>
    private static string? Where(string file)
    {
        IEnumerable<string> paths =
        [
            Environment.CurrentDirectory,
            .. Environment.GetEnvironmentVariable("PATH")!.Split(';', StringSplitOptions.TrimEntries)
        ];

        foreach (string path in paths)
        {
            string filePath = Path.Combine(path, file);

            if (File.Exists(filePath))
            {
                return filePath;
            }
        }

        return null;
    }

    /// <summary>
    /// Moves the progress bar one tick and displays the specified message.
    /// </summary>
    private static void UpdateProgress(ProgressBarBase progressBar, string message)
        => progressBar.Tick($"({progressBar.CurrentTick + 1}/{progressBar.MaxTicks}) {message}");

    /// <summary>
    /// Gets a yes/no response from the console after displaying <paramref name="message"/>.
    /// </summary>
    /// <returns><see langword="true"/> if the answer is 'yes'; otherwise, <see langword="false"/>.</returns>
    private bool PromptUser(string message) => AnswerYes || Prompt.GetYesNo(message, true);
}
