using Godot;

public partial class Switch : StaticBody3D
{
	[Export] public Material ActivatedMaterial;

	private MeshInstance3D _mesh;
	private Label3D _label;
	private bool _isActivated;

	public override void _Ready()
	{
		_mesh = GetNode<MeshInstance3D>("MeshInstance3D");
		_label = GetNode<Label3D>("Label3D");
		UpdateVisuals();
	}

	public void Interact()
	{
		if (_isActivated) return;

		_isActivated = true;
		UpdateVisuals();

		var gameManager = GetNode<GameManager>("/root/GameManager");
		gameManager?.OnSwitchActivated();
	}

	private void UpdateVisuals()
	{
		if (_isActivated && ActivatedMaterial != null && _mesh != null)
			_mesh.MaterialOverride = ActivatedMaterial;

		if (_label != null)
			_label.Text = _isActivated ? "ACTIVATED" : "Press E to Activate";
	}
}
