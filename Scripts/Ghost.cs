using Godot;
using System;
using System.Collections.Generic;

public partial class Ghost : Actor
{
	public enum Type
	{
		Blinky,
        Pinky,
        Inky,
        Clyde
	}

	public enum Mode
	{
		Chase,
        Scatter,
        Frightened,
        Eyes,
        InHouse,
        LeaveHouse,
        EnterHouse
	}

    public delegate bool LeaveHouseCallback(Ghost g);
    public delegate bool IsFrightenedCallback(Ghost g);
    public delegate Mode ScatterChasePhaseCallback();

    // some constants between ghosts

    private static readonly Vector2I[] scatterTiles = new Vector2I[4]
    {
        new Vector2I(25, -3), new Vector2I(2, -3), new Vector2I(27, 31), new Vector2I(0, 31)
    };

    private static readonly Vector2I[] startPositions = new Vector2I[]
    {
        new Vector2I(112, 92), new Vector2I(112, 116), new Vector2I(96, 116), new Vector2I(128, 116)
    };

    private static readonly Vector2I[] housePositions = new Vector2I[]
    {
        new Vector2I(112, 116), new Vector2I(112, 116), new Vector2I(96, 116), new Vector2I(128, 116)
    };

    private static readonly Random rand = new Random();

    public Type type = Type.Blinky;
	public Mode mode = Mode.Chase;
	public Vector2I targetTile = Vector2I.Zero;
    public Direction nextDirection;

    private Direction GetReverseDirection(Direction dir)
    {
        switch (dir)
        {
            case Direction.Right: return Direction.Left;
            case Direction.Left: return Direction.Right;
            case Direction.Up: return Direction.Down;
            case Direction.Down: return Direction.Up;
        }

        return Direction.Right;
    }

    public void SetStartState()
    {
        // set start position

        Position = startPositions[(int)type];
        animationTick = 0;

        // set start mode

        switch (type)
        {
            case Type.Blinky:
                mode = Mode.Scatter;
                nextDirection = direction = Direction.Left;
                break;
            case Type.Pinky:
                mode = Mode.InHouse;
                nextDirection = direction = Direction.Down;
                break;
            default:
                mode = Mode.InHouse;
                nextDirection = direction = Direction.Up;
                break;
        }
    }

    public Vector2I GetHousePosition()
    {
        return housePositions[(int)type];
    }

    public Vector2I GetScatterTile()
    {
        return scatterTiles[(int)type];
    }

    // get ghost chase tile, it needs to know pacman's position, also inky uses blinky position
    // to determine his target tile, so we need to pass an array with all the ghosts too

    public Vector2I GetChaseTile(Pacman pacman, Ghost[] ghosts)
    {
        Vector2I pacmanDirectionVector = pacman.GetDirectionVector();
        Vector2I pacmanTile = pacman.PositionToTile();

        switch (type)
        {
            case Type.Blinky:
                return pacmanTile;
            case Type.Pinky:
                if (pacmanDirectionVector.Y < 0)
                {
                    return pacmanTile + new Vector2I(-4, -4);
                }
                else
                {
                    return pacmanTile + 4 * pacmanDirectionVector;
                }
            case Type.Inky:
                Vector2I blinkyTile = ghosts[(int)Type.Blinky].PositionToTile();
                Vector2I tileInPacmanDirection;

                if (pacmanDirectionVector.Y < 0)
                {
                    tileInPacmanDirection = pacmanTile + new Vector2I(-2, -2);
                }
                else
                {
                    tileInPacmanDirection = pacmanTile + 2 * pacmanDirectionVector;
                }

                return tileInPacmanDirection + (tileInPacmanDirection - blinkyTile);
            case Type.Clyde:
                Vector2I ghostTile = PositionToTile();
                int distanceToPacman = (pacmanTile - ghostTile).LengthSquared();

                if (distanceToPacman < 8 * 8)
                {
                    return GetScatterTile();
                }
                else
                {
                    return pacmanTile;
                }
        }

        return Vector2I.Zero;
    }

    public void UpdateTargetTile(Pacman pacman, Ghost[] ghosts)
    {
        switch (mode)
        {
            case Mode.Chase:
                targetTile = GetChaseTile(pacman, ghosts);
                break;
            case Mode.Scatter:
                targetTile = scatterTiles[(int)type];
                break;
            case Mode.Frightened:
                targetTile = new Vector2I(rand.Next(Maze.Width), rand.Next(Maze.Height));
                break;
            case Mode.Eyes:
                targetTile = new Vector2I(13, 11);
                break;
            default:
                targetTile = Vector2I.Zero;
                break;
        }
    }

    /*
     * get the next direction the ghost should continue if it returns true the direction is forced,
     * that means that the ghost should continue with that direction even if encounters a wall going through it
     * this is needed when crossing the ghost door
     */

    private bool GetNextDirection()
	{
        // check if the ghost is in the house or entering or leaving from it

        Vector2I pos = (Vector2I)Position;

        switch (mode)
        {
            case Mode.InHouse:
                if (pos.Y <= 14 * Maze.TileSize)
                {
                    nextDirection = Direction.Down;
                }
                else if (pos.Y >= 15 * Maze.TileSize)
                {
                    nextDirection = Direction.Up;
                }

                direction = nextDirection;

                // force movement
                return true;
            case Mode.LeaveHouse:

                // if it is in front of the door

                if (pos.X == 112)
                {
                    if (pos.Y > 92)
                    {
                        nextDirection = Direction.Up;
                    }
                }
                else
                {
                    // if not in front of the door then first go to Y = 116

                    if (pos.Y < 116)
                    {
                        nextDirection = Direction.Down;
                    }
                    else if (pos.Y > 116)
                    {
                        nextDirection = Direction.Up;
                    }
                    else
                    {
                        // if it is already on Y = 116 then go to door x = 112

                        nextDirection = (pos.X < 112) ? Direction.Right : Direction.Left;
                    }
                }

                direction = nextDirection;

                return true;
            case Mode.EnterHouse:

                // if is not at the door go there first

                if (pos.X != 112 && pos.Y < 116)
                {
                    nextDirection = (pos.X < 112) ? Direction.Right : Direction.Left;
                }
                else
                {
                    // once at the door go down

                    if (pos.Y < 116)
                    {
                        nextDirection = Direction.Down;
                    }
                    else
                    {
                        // if already is in the middle of the house go to its house position

                        nextDirection = pos.X < housePositions[(int)type].X ? Direction.Right : Direction.Left;
                    }
                }

                direction = nextDirection;

                return true;
        }

        // if it is in chase scatter or frightened mode

        Vector2I distanceToTileMid = DistanceToTileMid();

        if (distanceToTileMid.X == 0 && distanceToTileMid.Y == 0) // only compute next direction if it is in the middle of the tile
        {
            direction = nextDirection;
            Vector2I lookAheadTile = PositionToTile() + GetDirectionVector();

            Vector2I[] neightbourTiles = new Vector2I[] { new Vector2I(1, 0), new Vector2I(-1, 0), new Vector2I(0, -1), new Vector2I(0, 1) };

            for (int i = 0; i < 4; i++)
            {
                neightbourTiles[i] += lookAheadTile;
            }

            // for each possible intersection tile check the distance to the target tile

            int lowestDistance = int.MaxValue;
            Direction[] testDirections = new Direction[] { Direction.Up, Direction.Left, Direction.Down, Direction.Right }; // the ghost prefers directions in this order

            foreach (Direction d in testDirections)
            {
                // in red zones you cant go upwards (unless the ghost is in frightened mode)

                if (mode != Mode.Frightened)
                {
                    if (d == Direction.Up && Maze.IsRedZone(lookAheadTile))
                    {
                        continue;
                    }
                }

                // you cant go reverse

                if (d != GetReverseDirection(direction) && Maze.GetTile(neightbourTiles[(int)d]) != Maze.Tile.Wall)
                {
                    int distanceToTarget = (targetTile - neightbourTiles[(int)d]).LengthSquared();

                    if (distanceToTarget < lowestDistance)
                    {
                        lowestDistance = distanceToTarget;
                        nextDirection = d;
                    }
                }
            }
        }

        return false;
    }

    // update ghost mode

    public void UpdateGhostMode(LeaveHouseCallback leaveHouse, IsFrightenedCallback isFrightened, ScatterChasePhaseCallback scatterChasePhase)
    {
        Mode newMode = mode;
        Vector2I pos = (Vector2I)Position;

        switch (newMode)
        {
            case Mode.InHouse:
                if (leaveHouse(this))
                {
                    newMode = Mode.LeaveHouse;
                }
                break;
            case Mode.LeaveHouse:
                // ghosts immediately switch to scatter mode after leaving the ghost house

                if (pos.Y == 92)
                {
                    newMode = Mode.Scatter;
                }
                break;
            case Mode.EnterHouse:
                // check if the ghost is inside the house
                Vector2I housePosition = GetHousePosition();

                if (IsNearEqual(housePosition, 1))
                {
                    newMode = Mode.LeaveHouse;
                }

                break;
            case Mode.Eyes:
                if (IsNearEqual(new Vector2I(13 * 8 + 4, 11 * 8 + 4), 1))
                {
                    newMode = Mode.EnterHouse;
                }
                break;
            default:
                // check if the ghost should be in frightened state

                if (isFrightened(this))
                {
                    newMode = Mode.Frightened;
                }
                else
                {
                    // if not alternate between chase and scatter

                    newMode = scatterChasePhase();
                }

                break;
        }

        // if the new state is different from the previous one handle transitions between states

        if (newMode != mode)
        {
            switch (mode)
            {
                case Mode.LeaveHouse:
                    // after leaving the ghost house, head to the left
                    nextDirection = direction = Direction.Left;
                    break;
                case Mode.EnterHouse:

                    break;
                case Mode.Scatter:
                case Mode.Chase:
                    // any transition from scatter and chase mode causes a reversal of direction

                    nextDirection = GetReverseDirection(direction);
                    break;
            }

            mode = newMode;
        }
    }

    // gets the number of pixels the ghost should move

    private int GetSpeed(int ticks)
    {
        switch (mode)
        {
            case Mode.InHouse:
            case Mode.LeaveHouse:
            case Mode.Frightened:
                return ticks & 1;
            case Mode.EnterHouse:
            case Mode.Eyes:
                return 2;
        }

        // check if it the ghost is in the tunnel 

        Vector2I tile = PositionToTile();

        if (Maze.GetTile(tile) == Maze.Tile.Tunnel)
        {
            return ticks & 1;
        }

        // move a little slower than pacman

        return (ticks % 20) != 0 ? 1 : 0;
    }

    // set sprites

    public void SetDefaultSpriteAnimation()
    {
        int phase = (animationTick / 8) & 1;
        FrameCoords = new Vector2I(phase + 2 * (int)nextDirection, (int)type);
    }

    public void SetFrightenedSpriteAnimation(int phase, bool flashTick)
    {
        FrameCoords = new Vector2I(phase + (flashTick ? 10 : 8), 0);
    }

    public void SetScoreSprite(int scoreIndex)
    {
        FrameCoords = new Vector2I(scoreIndex + 8, 2);
    }

    public void SetEyesSprite()
    {
        FrameCoords = new Vector2I(8 + (int)nextDirection, 1);
    }

    /* DEBUG */

    // get ghost path

    public void GetCurrentPath(List<Vector2I> path, int maxDepth)
    {
        path.Clear();

        if (mode != Mode.Chase && mode != Mode.Scatter && mode != Mode.Eyes)
            return;

        Vector2I currentTile = PositionToTile();
        Direction currentDirection = direction;

        do
        {
            Vector2I[] neightbourTiles = new Vector2I[] { new Vector2I(1, 0), new Vector2I(-1, 0), new Vector2I(0, -1), new Vector2I(0, 1) };

            for (int i = 0; i < 4; i++)
            {
                neightbourTiles[i] += currentTile;
            }

            // for each possible intersection tile check the distance to the target tile

            int lowestDistance = int.MaxValue;
            Direction[] testDirections = new Direction[] { Direction.Up, Direction.Left, Direction.Down, Direction.Right }; // the ghost prefers directions in this order
            Direction chosenNextDirection = Direction.Left;

            foreach (Direction d in testDirections)
            {
                // in red zones you cant go upwards (unless the ghost is in frightened mode)

                if (mode != Mode.Frightened)
                {
                    if (d == Direction.Up && Maze.IsRedZone(currentTile))
                    {
                        continue;
                    }
                }

                // you cant go reverse

                if (d != GetReverseDirection(currentDirection) && Maze.GetTile(neightbourTiles[(int)d]) != Maze.Tile.Wall)
                {
                    int distanceToTarget = (targetTile - neightbourTiles[(int)d]).LengthSquared();

                    if (distanceToTarget < lowestDistance)
                    {
                        lowestDistance = distanceToTarget;
                        chosenNextDirection = d;
                    }
                }
            }

            currentDirection = chosenNextDirection;
            currentTile += directionsMap[(int)chosenNextDirection];
            path.Add(currentTile);

        } while (currentTile != targetTile && currentTile != PositionToTile() && path.Count < maxDepth);
    }

    // Called when the node enters the scene tree for the first time.

    public override void _Ready()
    {
        mode = Mode.Chase;
    }

    // tick

    public override void Tick(int ticks)
    {
        // move the ghost

        int numPixelsToMove = GetSpeed(ticks);

        for (int i = 0; i < numPixelsToMove; i++)
        {
            // calculate next direction (only when in midpoint of the tile)

            bool forcedMove = GetNextDirection();

            // move along the direction

            if (CanMove(false) || forcedMove)
            {
                Move(false);
                animationTick++;
            }
        }
    }
}
