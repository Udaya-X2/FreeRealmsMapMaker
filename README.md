# FreeRealmsMapMaker

A console application that lets you create maps from Free Realms tile assets.

## Requirements

Must have [ImageMagick](https://imagemagick.org/) for Windows installed, and check "Add application path to your system path."

## Introduction

Simply place your `*_TileInfo.txt` and `*_Tile_*.dds` files (i.e., `FabledRealms_TileInfo.txt` and `FabledRealms_Tile_000_-08_0_0.dds`) in the same directory and pass it as the first argument to `FreeRealmsMapMaker.exe`. The second argument specifies where to create the maps.

For example,

```
FreeRealmsMapMaker.exe "C:/Users/udaya/Downloads/assets" "C:/Users/Udaya/Downloads/maps"
```

## Usage

```
Usage: FreeRealmsMapMaker [options] <InputDirectory> <OutputDirectory>

Arguments:
  InputDirectory                        The input directory containing the TileInfo.txt/tile files.
  OutputDirectory                       The output directory to put the map files.
                                        Default value is: ./maps.

Options:
  -t|--tile-directory <TILE_DIRECTORY>  The directory containing the intermediate tile files.
                                        Default value is: ./tiles.
  -f|--format <FORMAT>                  The image format of the output map.
                                        Default value is: .png.
  -m|--max-threads <MAX_THREADS>        The maximum number of threads to use during conversion.
                                        Set the value to -1 to specify no upper limit.
                                        Default value is: 1.
  -y|--answer-yes                       Automatically answer yes to any question.
  -?|-h|--help                          Show help information.
```

## Credits

Icon by [surang](https://www.freepik.com/icon/game-map_3862772).
