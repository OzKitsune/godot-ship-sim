using Godot;
using System;

public partial class Map : Control
{
	[Export] public Grid Grid { get; private set; }
}
