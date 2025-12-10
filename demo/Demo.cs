using Godot;
using System;

public partial class Demo : Node2D
{
	[Export] private Ship Ship;
    [Export] private Map Map;

    public override void _Ready()
	{
        Map.Grid.GridClicked += OnGridClicked;
        Ship.AutopilotTargetReached += OnShipAutopilotTargetReached;
        Ship.AutopilotDisengaged += OnShipAutopilotDisengaged;
    }

    private void OnGridClicked(Vector2 worldPosition)
    {
        GD.Print($"Grid clicked: {worldPosition}");

        Ship.EnableAutopilot(worldPosition);
    }

    private void OnShipAutopilotDisengaged(bool reachedTarget)
    {
        Map.Grid.ClearMarker();
    }

    private void OnShipAutopilotTargetReached()
    {
        Map.Grid.ClearMarker();
    }

    public override void _PhysicsProcess(double delta)
    {
        Map.Grid.UpdateGrid(Ship.GlobalPosition);
    } 
}
