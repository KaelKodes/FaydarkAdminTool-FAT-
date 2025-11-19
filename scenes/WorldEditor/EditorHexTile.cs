using Godot;
using System;

public partial class EditorHexTile : Area2D
{
	[Export] public Color BaseColor { get; set; } = new Color(0.2f, 0.6f, 0.2f);
	[Export] public Color SelectedColor { get; set; } = new Color(0.9f, 0.9f, 0.2f);

	public Vector2I GridPosition { get; private set; }

	private Polygon2D _fill;
	private Polygon2D _outline;

	public Action<EditorHexTile>? OnTileClicked;

	private bool _selected = false;

	public override void _Ready()
	{
		InputPickable = true;

		// Visual nodes
		_fill = GetNodeOrNull<Polygon2D>("CollisionPolygon2D/Fill");
		_outline = GetNodeOrNull<Polygon2D>("CollisionPolygon2D/Outline");

		if (_fill == null)
		{
			GD.PrintErr($"[EditorHexTile] Fill polygon NOT FOUND on tile {Name}. Check node paths!");
		}
		else
		{
			_fill.Color = BaseColor;
		}

		if (_outline != null)
			_outline.Color = new Color(0, 0, 0); // border
	}

	public void Initialize(Vector2I gridPos)
	{
		GridPosition = gridPos;
	}

	public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
	{
		if (@event is InputEventMouseButton mb &&
			mb.Pressed &&
			mb.ButtonIndex == MouseButton.Left)
		{
			OnTileClicked?.Invoke(this);   // delegate callback
			ToggleSelected();              // local highlight
		}
	}

	private void ToggleSelected()
	{
		_selected = !_selected;

		if (_fill != null)
			_fill.Color = _selected ? SelectedColor : BaseColor;

		GD.Print($"Hex clicked at {GridPosition}");
	}

	public void SetElevationPreview(int elevation)
{
	if (_fill == null)
	{
		GD.PrintErr($"[EditorHexTile] Cannot set elevation — Fill polygon missing on {GridPosition}");
		return;
	}

	Color color = elevation switch
	{
		3  => new Color(0.25f, 0.25f, 0.25f), // Tall mountain (peak)
		2  => new Color(0.40f, 0.40f, 0.40f), // Mountain ridge
		1  => new Color(0.65f, 0.65f, 0.65f), // Foothill

		0  => new Color(0.20f, 0.60f, 0.20f), // Plains

		-1 => new Color(0.00f, 0.25f, 0.60f), // Shallow water
		-2 => new Color(0.00f, 0.10f, 0.40f), // Deep water

		_  => new Color(1f, 0f, 1f) // Debug magenta
	};

	_fill.Color = color;
}

}
