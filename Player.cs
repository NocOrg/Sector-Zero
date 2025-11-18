using Godot;

public enum MovementState { Idle, Crouching, Walking, Sprinting }

public partial class Player : CharacterBody3D
{
	[Export] public float WalkSpeed = 3.0f;
	[Export] public float SprintSpeed = 10.0f;
	[Export] public float CrouchSpeed = 1.5f;
	[Export] public float MouseSensitivity = 0.002f;
	[Export] public float Gravity = 20.0f;

	[Export] public float MaxStamina = 100.0f;
	[Export] public float StaminaDrainRate = 40.0f;
	[Export] public float StaminaRegenRate = 10.0f;
	[Export] public float StaminaRegenDelay = 5.0f;
	[Export] public float MinStaminaToSprint = 5.0f;

	[Export] public float StandingHeight = 2.0f;
	[Export] public float CrouchingHeight = 1.2f;
	[Export] public float CrouchTransitionSpeed = 10.0f;

	private Camera3D _camera;
	private RayCast3D _interactionRay;
	private CollisionShape3D _collisionShape;
	private CapsuleShape3D _capsuleShape;

	private float _currentHeight;
	private float _currentStamina;
	private float _timeSinceLastSprint;

	// Public properties for stealth detection
	public MovementState CurrentMovementState { get; private set; } = MovementState.Idle;
	public bool IsMoving { get; private set; }
	public Vector3 EyePosition => _camera != null ? _camera.GlobalPosition : GlobalPosition + Vector3.Up * 1.6f;

	public override void _Ready()
	{
		_camera = GetNode<Camera3D>("Camera3D");
		_interactionRay = GetNode<RayCast3D>("Camera3D/InteractionRay");
		_collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
		_capsuleShape = _collisionShape.Shape as CapsuleShape3D;

		_currentHeight = StandingHeight;
		_currentStamina = MaxStamina;

		if (_capsuleShape != null)
			_capsuleShape.Height = StandingHeight;

		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			RotateY(-mouseMotion.Relative.X * MouseSensitivity);
			_camera.RotateX(-mouseMotion.Relative.Y * MouseSensitivity);

			Vector3 rot = _camera.Rotation;
			rot.X = Mathf.Clamp(rot.X, -Mathf.Pi / 2, Mathf.Pi / 2);
			_camera.Rotation = rot;
		}

		if (@event.IsActionPressed("ui_cancel"))
			Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
				? Input.MouseModeEnum.Visible
				: Input.MouseModeEnum.Captured;

		if (@event.IsActionPressed("interact") && _interactionRay.IsColliding())
		{
			var collider = _interactionRay.GetCollider();
			if (collider is Node node && node.HasMethod("Interact"))
				node.Call("Interact");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		Vector3 velocity = Velocity;

		// Crouch
		bool isCrouching = Input.IsActionPressed("crouch");
		float targetHeight = isCrouching ? CrouchingHeight : StandingHeight;
		_currentHeight = Mathf.Lerp(_currentHeight, targetHeight, CrouchTransitionSpeed * dt);

		if (_capsuleShape != null)
		{
			_capsuleShape.Height = _currentHeight;
			_collisionShape.Position = new Vector3(0, _currentHeight / 2, 0);
		}
		_camera.Position = new Vector3(0, _currentHeight * 0.8f, 0);

		// Gravity
		if (!IsOnFloor())
			velocity.Y -= Gravity * dt;

		// Stamina
		bool wantsToSprint = Input.IsActionPressed("sprint") && !isCrouching;
		bool isSprinting = wantsToSprint && _currentStamina >= MinStaminaToSprint;

		if (isSprinting)
		{
			_currentStamina = Mathf.Max(_currentStamina - StaminaDrainRate * dt, 0);
			_timeSinceLastSprint = 0;
		}
		else
		{
			_timeSinceLastSprint += dt;
			if (_timeSinceLastSprint >= StaminaRegenDelay && _currentStamina < MaxStamina)
				_currentStamina = Mathf.Min(_currentStamina + StaminaRegenRate * dt, MaxStamina);
		}

		// Movement
		float speed = isCrouching ? CrouchSpeed : (isSprinting ? SprintSpeed : WalkSpeed);
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * speed;
			velocity.Z = direction.Z * speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(velocity.X, 0, speed);
			velocity.Z = Mathf.MoveToward(velocity.Z, 0, speed);
		}

		// Update movement state for stealth system
		IsMoving = direction != Vector3.Zero;
		if (!IsMoving)
			CurrentMovementState = MovementState.Idle;
		else if (isCrouching)
			CurrentMovementState = MovementState.Crouching;
		else if (isSprinting)
			CurrentMovementState = MovementState.Sprinting;
		else
			CurrentMovementState = MovementState.Walking;

		Velocity = velocity;
		MoveAndSlide();
	}
}
