using Godot;

public partial class CameraRig : Node2D
{
	private Camera2D _cam;

	// Zoom configuration
	[Export] public float ZoomStep = 0.1f;
	[Export] public float MaxZoom = 3.0f;
	[Export] public float MinZoom = 0.3f;

	// Pan configuration
	[Export] public float PanSpeed = 800f;

	private bool _dragging = false;
	private Vector2 _lastMousePos;

	public override void _Ready()
	{
		_cam = GetNode<Camera2D>("Camera2D");
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		HandleZoom(@event);
		HandlePan(@event);
	}


	private void HandleZoom(InputEvent e)
	{
		if (e is InputEventMouseButton mb && mb.Pressed)
		{
			if (mb.ButtonIndex == MouseButton.WheelUp)
			{
				_cam.Zoom -= new Vector2(ZoomStep, ZoomStep);
			}
			else if (mb.ButtonIndex == MouseButton.WheelDown)
			{
				_cam.Zoom += new Vector2(ZoomStep, ZoomStep);
			}

			// Clamp zoom
			_cam.Zoom = new Vector2(
				Mathf.Clamp(_cam.Zoom.X, MinZoom, MaxZoom),
				Mathf.Clamp(_cam.Zoom.Y, MinZoom, MaxZoom)
			);
		}
	}


	private void HandlePan(InputEvent e)
	{
		// --- Start dragging ---
		if (e is InputEventMouseButton mb)
		{
			if ((mb.ButtonIndex == MouseButton.Right || mb.ButtonIndex == MouseButton.Middle) &&
				mb.Pressed)
			{
				_dragging = true;
				_lastMousePos = GetViewport().GetMousePosition();
			}
			else if (!mb.Pressed &&
					 (mb.ButtonIndex == MouseButton.Right || mb.ButtonIndex == MouseButton.Middle))
			{
				_dragging = false;
			}
		}

		// --- Drag movement ---
		if (e is InputEventMouseMotion mm && _dragging)
		{
			Vector2 mouseNow = mm.Position;
			Vector2 delta = (_lastMousePos - mouseNow) * _cam.Zoom.X;

			Position += delta;
			_lastMousePos = mouseNow;
		}

		// --- WASD fallback movement ---
Vector2 input = Vector2.Zero;

if (Input.IsActionPressed("ui_up")) input.Y -= 1;
if (Input.IsActionPressed("ui_down")) input.Y += 1;
if (Input.IsActionPressed("ui_left")) input.X -= 1;
if (Input.IsActionPressed("ui_right")) input.X += 1;

float dt = (float)GetProcessDeltaTime();

if (input != Vector2.Zero)
{
	Position += input.Normalized() * PanSpeed * dt * _cam.Zoom.X;
}

	}
}
