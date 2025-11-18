using Godot;
using System;

public partial class Intruder : CharacterBody3D
{
	// AI State Machine
	private enum AIState { Patrol, Chase }
	private AIState currentState = AIState.Patrol;

	// Movement Settings
	[Export] public float Speed = 5.0f;
	[Export] public float PatrolRadius = 20.0f;

	// Node References
	private NavigationAgent3D navAgent;
	private Area3D detectionZone;
	private Timer patrolTimer;

	// State
	private CharacterBody3D playerTarget;
	private float gravity;

	public override void _Ready()
	{
		// Get gravity from project settings
		gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");

		// Get node references
		navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");
		detectionZone = GetNode<Area3D>("DetectionZone");
		patrolTimer = GetNode<Timer>("PatrolTimer");

		// Connect signals
		if (patrolTimer != null)
		{
			patrolTimer.Timeout += OnPatrolTimerTimeout;
			patrolTimer.Start();
		}

		if (detectionZone != null)
		{
			detectionZone.BodyEntered += OnDetectionZoneBodyEntered;
			detectionZone.BodyExited += OnDetectionZoneBodyExited;
		}

		// Set initial patrol destination
		if (navAgent != null)
		{
			navAgent.TargetPosition = GetRandomReachablePoint();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		// Apply gravity
		if (!IsOnFloor())
		{
			Velocity = new Vector3(Velocity.X, Velocity.Y - gravity * (float)delta, Velocity.Z);
		}

		// State-dependent behavior
		switch (currentState)
		{
			case AIState.Patrol:
				HandlePatrolMovement();
				break;

			case AIState.Chase:
				HandleChaseMovement();
				break;
		}

		// Move the character
		MoveAndSlide();

		// Check for collision with player
		CheckPlayerCollision();
	}

	private void CheckPlayerCollision()
	{
		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			var collision = GetSlideCollision(i);
			var collider = collision.GetCollider();

			if (collider is CharacterBody3D body && body.IsInGroup("Player"))
			{
				// Kill the player
				var gameManager = GetNode<GameManager>("/root/GameManager");
				gameManager?.OnPlayerDeath();
				break;
			}
		}
	}

	private void HandlePatrolMovement()
	{
		if (navAgent == null || !navAgent.IsNavigationFinished())
		{
			MoveTowardsTarget();
		}
	}

	private void HandleChaseMovement()
	{
		if (playerTarget != null && navAgent != null)
		{
			// Constantly update target to player's position
			navAgent.TargetPosition = playerTarget.GlobalPosition;
			MoveTowardsTarget();
		}
	}

	private void MoveTowardsTarget()
	{
		if (navAgent == null || !navAgent.IsNavigationFinished())
		{
			Vector3 nextPathPosition = navAgent.GetNextPathPosition();
			Vector3 direction = (nextPathPosition - GlobalPosition).Normalized();

			// Set horizontal velocity only
			Velocity = new Vector3(
				direction.X * Speed,
				Velocity.Y, // Preserve vertical velocity (gravity)
				direction.Z * Speed
			);
		}
		else
		{
			// Stop moving if we've reached the destination
			Velocity = new Vector3(0, Velocity.Y, 0);
		}
	}

	private Vector3 GetRandomReachablePoint()
	{
		// Get a random point within patrol radius
		Vector3 randomOffset = new Vector3(
			(float)GD.RandRange(-PatrolRadius, PatrolRadius),
			0,
			(float)GD.RandRange(-PatrolRadius, PatrolRadius)
		);

		Vector3 targetPosition = GlobalPosition + randomOffset;

		// Use NavigationServer3D to find the nearest valid point on the navmesh
		Rid map = NavigationServer3D.MapGetClosestPointOwner(GetWorld3D().NavigationMap, targetPosition);

		if (map.IsValid)
		{
			Vector3 closestPoint = NavigationServer3D.MapGetClosestPoint(GetWorld3D().NavigationMap, targetPosition);
			return closestPoint;
		}

		// Fallback: return a point near current position
		return GlobalPosition + new Vector3(
			(float)GD.RandRange(-5, 5),
			0,
			(float)GD.RandRange(-5, 5)
		);
	}

	private void OnPatrolTimerTimeout()
	{
		// Only set new patrol point if we're in patrol state
		if (currentState == AIState.Patrol && navAgent != null)
		{
			navAgent.TargetPosition = GetRandomReachablePoint();
		}
	}

	private void OnDetectionZoneBodyEntered(Node3D body)
	{
		if (body.IsInGroup("Player") && body is CharacterBody3D player)
		{
			playerTarget = player;
			currentState = AIState.Chase;
		}
	}

	private void OnDetectionZoneBodyExited(Node3D body)
	{
		if (body == playerTarget)
		{
			playerTarget = null;
			currentState = AIState.Patrol;

			if (navAgent != null)
			{
				navAgent.TargetPosition = GetRandomReachablePoint();
			}
		}
	}
}
