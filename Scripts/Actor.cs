using Godot;
using System;

public abstract partial class Actor : Sprite2D
{
    public enum Direction
    {
        Right,
        Left,
        Up,
        Down
    }

    protected static readonly Vector2I[] directionsMap = new Vector2I[]
    {
        new Vector2I(1, 0), new Vector2I(-1, 0), new Vector2I(0, -1), new Vector2I(0, 1) 
    };

    public Direction direction;
    protected int animationTick;

    // gets the tile the actor is in

    public Vector2I PositionToTile()
    {
        return (Vector2I)Position / Maze.TileSize;
    }

    // gets a vector2i direction from the enum direction

    public Vector2I GetDirectionVector()
    {
        return directionsMap[(int)direction];
    }

    // get the distance from a position to the position tile midpoint

    public Vector2I DistanceToTileMid()
    {
        return new Vector2I(Maze.TileSize / 2, Maze.TileSize / 2) - ((Vector2I)Position % Maze.TileSize);
    }

    // check if the actor is near a certain position with a tolerance factor

    public bool IsNearEqual(Vector2I p, int tolerance)
    {
        Vector2I pos = (Vector2I)Position;
        return (Mathf.Abs(pos.X - p.X) <= tolerance) && (Mathf.Abs(pos.Y - p.Y) <= tolerance);
    }

    // checks if the actor can move in a direction and if is cornering allowed for that actor

    protected bool CanMove(bool allowCornering)
    {
        Vector2I directionVector = GetDirectionVector();
        Vector2I distanceToTileMid = DistanceToTileMid();

        int moveDistanceToMid, perpendicularDistanceToMid;

        if (directionVector.Y != 0)
        {
            moveDistanceToMid = distanceToTileMid.Y;
            perpendicularDistanceToMid = distanceToTileMid.X;
        }
        else
        {
            moveDistanceToMid = distanceToTileMid.X;
            perpendicularDistanceToMid = distanceToTileMid.Y;
        }

        Vector2I tile = PositionToTile();
        Vector2I tileNext = tile + directionVector;
        bool blocked = (Maze.GetTile(tileNext) == Maze.Tile.Wall);

        if ((moveDistanceToMid == 0 && blocked) || (!allowCornering && perpendicularDistanceToMid != 0))
        {
            return false;
        }

        return true;
    }

    // gets the next position of the move (it doesnt check for blocked tiles)

    protected void Move(bool allowCornering)
    {
        Vector2I directionVector = GetDirectionVector();
        Vector2I pos = (Vector2I)Position;
        pos += directionVector;

        if (allowCornering)
        {
            Vector2I distanceToTileMid = DistanceToTileMid();

            if (directionVector.X != 0)
            {
                if (distanceToTileMid.Y < 0) { pos.Y--; }
                else if (distanceToTileMid.Y > 0) { pos.Y++; }
            }
            else if (directionVector.Y != 0)
            {
                if (distanceToTileMid.X < 0) { pos.X--; }
                else if (distanceToTileMid.X > 0) { pos.X++; }
            }
        }

        // wrap position around

        if (pos.X < 0)
        {
            pos.X = Maze.TileSize * Maze.Width - 1;
        }
        else if (pos.X >= Maze.TileSize * Maze.Width)
        {
            pos.X = 0;
        }

        Position = pos;
    }

    // tick (must be overriden)

    public abstract void Tick(int ticks);
}
