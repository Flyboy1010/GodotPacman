using Godot;
using System;

public partial class Pacman : Actor
{
    // eat animation frames

    private static readonly int[] animationFramePhase = new int[] { 1, 0, 1, 2 };

    // set start state

    public void SetStartState()
    {
        Position = new Vector2I(112, 188);
        direction = Direction.Left;
        animationTick = 0;
        SetStartRoundSprite();
    }

    // get input

    private Direction GetInputDirection()
    {
        if (Input.IsActionPressed("Right"))
            return Direction.Right;
        else if (Input.IsActionPressed("Left"))
            return Direction.Left;
        else if (Input.IsActionPressed("Up"))
            return Direction.Up;
        else if (Input.IsActionPressed("Down"))
            return Direction.Down;

        return direction;
    }

    // sprite frame stuff

    public void SetStartRoundSprite()
    {
        FrameCoords = new Vector2I(2, (int)Direction.Left);
    }

    public void SetDefaultSpriteAnimation()
    {
        int phase = (animationTick / 2) & 3;
        FrameCoords = new Vector2I(animationFramePhase[phase], (int)direction);
    }

    public void SetDeathSpriteAnimation(int tick)
    {
        int phase = 3 + tick / 8;

        if (phase >= 14)
        {
            Visible = false;
        }

        phase = Mathf.Clamp(phase, 3, 13);
        FrameCoords = new Vector2I(phase, 0);
    }

    // Called when the node enters the scene tree for the first time.

    public override void _Ready()
	{
        direction = Direction.Left;
	}

    // tick

    public override void Tick(int ticks)
    {
        /* Handle movement */

        Direction oldDirection = direction;
        direction = GetInputDirection();

        // check if it cant switch directions

        if (!CanMove(true))
        {
            // if not then swap back to the old direction

            direction = oldDirection;
        }

        // check movement in the current direction

        if (CanMove(true))
        {
            Move(true);
            animationTick++;
        }
    }
}
