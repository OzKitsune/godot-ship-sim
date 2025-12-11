using Godot;
using System;

public partial class Info : Control
{
    [Export] private Label _label;

    public void Show(string info) 
    {
        _label.Text = info;
    }

    public void Clear() 
    {
        _label.Text = string.Empty;
    }
}
