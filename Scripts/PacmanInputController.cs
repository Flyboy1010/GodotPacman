using Godot;
using System;

public partial class PacmanInputController : Node2D
{
    [Signal]
    public delegate void ChangeDirectionSignalEventHandler(Actor.Direction direction);

    private Vector2 position = Vector2.Zero;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("Click"))
		{
			position = GetViewport().GetMousePosition();
		}

		if (Input.IsActionJustReleased("Click"))
		{
            Vector2 newPosition = GetViewport().GetMousePosition();
            Vector2 direction = (newPosition - position).Normalized();

            if (direction.X > 0)
            {
                if (direction.Y > 0)
                {
                    GD.Print(direction.X > direction.Y ? "derecha" : "abajo");
                    EmitSignal(SignalName.ChangeDirectionSignal, direction.X > direction.Y ? (int)Actor.Direction.Right : (int)Actor.Direction.Down);
                }
                else if (direction.Y <= 0)
                {
                    GD.Print(direction.X > Math.Abs(direction.Y) ? "derecha" : "arriba");
                    EmitSignal(SignalName.ChangeDirectionSignal, direction.X > Math.Abs(direction.Y) ? (int)Actor.Direction.Right : (int)Actor.Direction.Up);
                }
            }
            else if (direction.X <= 0)
            {
                if (direction.Y > 0)
                {
                    GD.Print(direction.X > direction.Y ? "izquierda" : "abajo");
                    EmitSignal(SignalName.ChangeDirectionSignal, Math.Abs(direction.X) > direction.Y ? (int)Actor.Direction.Left : (int)Actor.Direction.Down);
                }
                else if (direction.Y <= 0)
                {
                    GD.Print(direction.X > Math.Abs(direction.Y) ? "izquierda" : "arriba");
                    EmitSignal(SignalName.ChangeDirectionSignal, Math.Abs(direction.X) > Math.Abs(direction.Y) ? (int)Actor.Direction.Left : (int)Actor.Direction.Up);
                }
            }
        }
	}
}
