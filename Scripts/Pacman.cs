using Godot;
using System;

public partial class Pacman : Actor
{
    private Direction selectedDirection;

    // eat animation frames

    private static readonly int[] animationFramePhase = new int[] { 1, 0, 1, 2 };

    // set start state

    public void SetStartState()
    {
        Position = new Vector2I(112, 188);
        direction = Direction.Left;
        selectedDirection = Direction.Left;
        animationTick = 0;
        SetStartRoundSprite();
    }

    // set direction

    public void SetDirection(Direction newDirection)
    {
        selectedDirection = newDirection;
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
        direction = selectedDirection;

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
