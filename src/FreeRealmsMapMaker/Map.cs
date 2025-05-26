namespace FreeRealmsMapMaker;

/// <summary>
/// Represents the map for a Free Realms area.
/// </summary>
/// <param name="tileInfoFile">A TileInfo.txt file containing tile names/locations.</param>
public class Map(string tileInfoFile)
{
    /// <summary>
    /// The tiles in this map.
    /// </summary>
    public List<Tile> Tiles { get; } = [.. File.ReadLines(tileInfoFile).Select(x => new Tile(x))];

    /// <summary>
    /// The name of this map.
    /// </summary>
    public string Name { get; } = Path.GetFileName(tileInfoFile)[..^"_TileInfo.txt".Length];

    /// <summary>
    /// Computes the borders of this map.
    /// </summary>
    /// <param name="minX">The minimum x-coordinate.</param>
    /// <param name="minZ">The minimum z-coordinate.</param>
    /// <param name="maxX">The maximum x-coordinate.</param>
    /// <param name="maxZ">The maximum z-coordinate.</param>
    public void ComputeMapBorders(out int minX, out int minZ, out int maxX, out int maxZ)
    {
        minX = int.MaxValue;
        minZ = int.MaxValue;
        maxX = int.MinValue;
        maxZ = int.MinValue;

        foreach (Tile tile in Tiles)
        {
            minX = Math.Min(minX, tile.X);
            minZ = Math.Min(minZ, tile.Z);
            maxX = Math.Max(maxX, tile.X + tile.Width);
            maxZ = Math.Max(maxZ, tile.Z + tile.Height);
        }
    }
}
