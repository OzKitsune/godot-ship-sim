using Godot;
using System;

public partial class Demo : Node2D
{
	[Export] private Ship _ship;
    [Export] private Map _map;

    public override void _Ready()
	{
        _map.Grid.GridClicked += OnGridClicked;
        _ship.AutopilotTargetReached += OnShipAutopilotTargetReached;
        _ship.AutopilotDisengaged += OnShipAutopilotDisengaged;
    }

    private void OnGridClicked(Vector2 worldPosition)
    {
        GD.Print($"Grid clicked: {worldPosition}");

        _ship.EnableAutopilot(worldPosition);
    }

    private void OnShipAutopilotDisengaged(bool reachedTarget)
    {
        _map.Grid.ClearMarker();
    }

    private void OnShipAutopilotTargetReached()
    {
        _map.Grid.ClearMarker();
    }

    public override void _PhysicsProcess(double delta)
    {
        _map.Grid.UpdateGrid(_ship.GlobalPosition);
    } 
}
