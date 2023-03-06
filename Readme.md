# Tiled2Dmap

Project that convert Conquer Online game maps to and from a [Tiled](https://www.mapeditor.org/) project.

## Description

This CLI utility converts Conquer Online game maps to a format that can be easily modified by an existing, well established, map editor. In theory you can edit any existing map, save for a few unsupported map objects (scenes and additional puzzle layers). You can also create new maps from new or existing resources. 

## Getting Started

### Usage
#### 7-zip Path
Program.cs line 17 needs to be updated to point to 7z.dll
#### dmap2tiled
> Converts a Conquer Online game map to a tiled project
``` bat
dmap2tiled [--project <String>] [--client <String>] [--dmap <String>] [--save-background] [--help]
```
Options:
  * `--project <String>`       Directory of Project (Required)
  * `--client <String>`        Directory of client resources (Required)
  * `--dmap <String>`          Path to Dmap File (Required)
  * `-s, --save-background`    Saves the stiched together map background
  * `-f, --force-convert`      Forces the conversion, even with unsupported map features
  * `-h, --help`               Show help message
  
#### tiled2dmap
>Converts a tiled project into a Conquer Online game map
``` bat
tiled2dmap [--project <String>] [--map-name <String>] [--help]
```
Options:
  * `--project <String>`     Directory of Project (Required)
  * `--map-name <String>`    Name of the map (Required)
  * `-h, --help`             Show help message

#### Preview
>Displays a specific Conquer online game map
``` bat
preview [--width <Int32>] [--height <Int32>] [--help] client dmap
```
Arguments:
  * `client`    Directory of client resources (Required)
  * `dmap`      Path to Dmap File

Options:
  * `-w, --width <Int32>`     Preview Window Width (Default: 1024)
  * `-h, --height <Int32>`    Preview Window Height (Default: 768)
  * `--help`                  Show help message

#### Extract  
>Extracts all client resources for a specific game map
``` bat
extract [--help] output dmap client name
```
Arguments:
  * `output`    Output Directory (Required)
  * `dmap`      Path to Dmap File (Required)
  * `client`    Directory of client resources (Required)
  * `name`      Name of the new dmap (Required)

Options:
  * `-h, --help`    Show help message
  
#### Install
>Copies all resources to the target game client. Modifies the gamemap.dat file to add the map with the specific map Id
``` bat
install [--puzzle-size <UInt16>] [--help] project map-id client
```
Arguments:
  * `project`    Project Directory (Required)
  * `map-id`     New Map Id (Required)
  * `client`     Client root directory to install (Required)

Options:
  * `--puzzle-size <UInt16>`    Size in pixels of puzzle pieces (Default: 256)
  * `-h, --help`                Show help message

#### stitch-dmap
>Assembles a maps background puzzle into an image
``` bat
stitch-dmap [--output <String>] [--client <String>] [--dmap <String>] [--help]
```
Options:
  * `--output <String>`    Directory of Project (Required)
  * `--client <String>`    Directory of client resources (Required)
  * `--dmap <String>`      Path to Dmap File (Required)
  * `-h, --help`           Show help message
  
#### new-project
>Creates a directory...todo: scaffold project dir
``` bat
new-project [--project <String>] [--directory <String>] [--help]
```
Options:
  * `--project <String>`      Name of the project (Required)
  * `--directory <String>`    Directory for project folder to be created, default is current dir
  * `-h, --help`              Show help message

#### serialize
>Serializes a DMAP file to a JSON file, for simple manual edits.
``` bat
serialize [--exclude-tiles] [--help] dmap output
```
Arguments:
  * `dmap`      Path to Dmap File (Required)
  * `output`    Output Directory for json file (Required)

Options:
  * `-x, --exclude-tiles`    Exclude tile set in serlaization
  * `-h, --help`             Show help message
#### deserialize
>Deserializes a JSON file to a DMAP file
``` bat
deserialize [--help] json output
```
Arguments:
  * `json`      Path to Json File (Required)
  * `output`    Output Directory for dmap file (Required)

Options:
  * `-h, --help`    Show help message

### Dependencies
* 7-zip
* [BCnEncoder.Net.ImageSharp](https://github.com/Nominom/BCnEncoder.NET)
* [Cocona](https://github.com/mayuki/Cocona)
* [ImageSharp](https://github.com/SixLabors/ImageSharp)
* [MonoGame](https://www.monogame.net/)
* [Squid-Box.SevenZipSharp](https://github.com/squid-box/SevenZipSharp)

## Acknowledgments
* Relic - Motivation, RE assistance
* CptSky - [WDF](https://gitlab.com/conquer-online/tools/co2_core_dll/-/blob/master/src/IO/WDF.cs) and [DNP](https://gitlab.com/conquer-online/tools/co2_core_dll/-/blob/master/src/IO/DNP.cs) structures
