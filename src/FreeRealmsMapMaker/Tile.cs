namespace FreeRealmsMapMaker;

/// <summary>
/// Represents a tile in a Free Realms map.
/// </summary>
/// <param name="Name">The name of the tile.</param>
/// <param name="X">The x-coordinate of the tile.</param>
/// <param name="Y">The y-coordinate of the tile.</param>
/// <param name="Z">The z-coordinate of the tile.</param>
/// <param name="Width">The width of the tile.</param>
/// <param name="Height">The height of the tile.</param>
public record Tile(string Name, int X, int Y, int Z, int Width, int Height)
{
    /// <summary>
    /// Parses a Free Realms map tile from <paramref name="parts"/>.
    /// </summary>
    /// <param name="parts">A string array containing tile info.</param>
    public Tile(string[] parts) : this(parts[0],
                                       (int)double.Parse(parts[1]),
                                       (int)double.Parse(parts[2]),
                                       (int)double.Parse(parts[3]),
                                       (int)double.Parse(parts[4]),
                                       (int)double.Parse(parts[5]))
    {
    }

    /// <summary>
    /// Parses a Free Realms map tile from the specified TileInfo.txt line.
    /// </summary>
    /// <param name="line">A line from a TileInfo.txt file.</param>
    public Tile(string line) : this(line.Split('\t'))
    {
    }
}
