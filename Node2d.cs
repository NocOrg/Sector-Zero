using Godot;
using System;

public partial class Node2d : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
        // TOTO PŘIDEJTE:
        GD.Print(">>> KOD BEZI! HELLO WORLD! <<<");
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
