using Godot;

public partial class GameManager : Node
{
	[Export] public int TotalSwitches = 1;

	private int _activatedSwitches;

	public void OnSwitchActivated()
	{
		_activatedSwitches++;

		if (_activatedSwitches >= TotalSwitches)
			TriggerEnding();
	}

	private void TriggerEnding()
	{
		GD.Print("=================================");
		GD.Print("YOU ACTIVATED THE EMERGENCY SHUTDOWN!");
		GD.Print("But something went wrong...");
		GD.Print("The portal is collapsing!");
		GD.Print("REALITY IS TEARING APART!");
		GD.Print("=================================");

		GetTree().CreateTimer(5.0).Timeout += () => GetTree().Quit();
	}
}
