using Godot;
using System;

public static class Maze
{
    public enum Tile
    {
        Empty = ' ', Wall = 'X', Tunnel = '=', Dot = '.', Pill = 'o'
    }

    // maze dimensions

    public static readonly int Width = 28;
    public static readonly int Height = 31;
    public static readonly int TileSize = 8;

    public static readonly int NumDots = 244;

    private static Tile[] maze = new Tile[Width * Height];

    // maze template

    private static readonly string[] mazeTemplate = new string[] {
        "XXXXXXXXXXXXXXXXXXXXXXXXXXXX",
        "X............XX............X",
        "X.XXXX.XXXXX.XX.XXXXX.XXXX.X",
        "XoX  X.X   X.XX.X   X.X  XoX",
        "X.XXXX.XXXXX.XX.XXXXX.XXXX.X",
        "X..........................X",
        "X.XXXX.XX.XXXXXXXX.XX.XXXX.X",
        "X.XXXX.XX.XXXXXXXX.XX.XXXX.X",
        "X......XX....XX....XX......X",
        "XXXXXX.XXXXX XX XXXXX.XXXXXX",
        "     X.XXXXX XX XXXXX.X     ",
        "     X.XX          XX.X     ",
        "     X.XX XXXXXXXX XX.X     ",
        "XXXXXX.XX XX    XX XX.XXXXXX",
        "======.   XX    XX   .======",
        "XXXXXX.XX XXXXXXXX XX.XXXXXX",
        "     X.XX XXXXXXXX XX.X     ",
        "     X.XX          XX.X     ",
        "     X.XX XXXXXXXX XX.X     ",
        "XXXXXX.XX XXXXXXXX XX.XXXXXX",
        "X............XX............X",
        "X.XXXX.XXXXX.XX.XXXXX.XXXX.X",
        "X.XXXX.XXXXX.XX.XXXXX.XXXX.X",
        "Xo..XX.......  .......XX..oX",
        "XXX.XX.XX.XXXXXXXX.XX.XX.XXX",
        "XXX.XX.XX.XXXXXXXX.XX.XX.XXX",
        "X......XX....XX....XX......X",
        "X.XXXXXXXXXX.XX.XXXXXXXXXX.X",
        "X.XXXXXXXXXX.XX.XXXXXXXXXX.X",
        "X..........................X",
        "XXXXXXXXXXXXXXXXXXXXXXXXXXXX"
    };

    // redo maze from maze template

    public static void Reset()
    {
        for (int j = 0; j < Height; j++)
        {
            for (int i = 0; i < Width; i++)
            {
                maze[i + j * Width] = (Tile)mazeTemplate[j][i];
            }
        }
    }

    // set and get tiles from the maze

    public static Tile GetTile(Vector2I tilePosition)
    {
        tilePosition = tilePosition.Clamp(Vector2I.Zero, new Vector2I(Width - 1, Height - 1));
        return maze[tilePosition.X + tilePosition.Y * Width];
    }

    public static void SetTile(Vector2I tilePosition, Tile tile)
    {
        maze[tilePosition.X + tilePosition.Y * Width] = tile;
    }

    // check if a tile is in red zone

    public static bool IsRedZone(Vector2I tile)
    {
        return ((tile.Y == 23 || tile.Y == 11) && (tile.X >= 11 && tile.X <= 16));
    }
}
