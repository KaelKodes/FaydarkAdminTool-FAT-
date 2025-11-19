using Godot;
using System;

public partial class EditorHexTile : Area2D
{
	[Export]
	public Color BaseColor { get; set; } = new Color(0.25f, 0.5f, 0.25f); // land-ish

	[Export]
	public Color SelectedColor { get; set; } = new Color(0.9f, 0.9f, 0.2f);

	public Vector2I GridPosition { get; private set; }

	private Polygon2D _fill;
	private bool _selected;

	public override void _Ready()
	{
		// Make sure Area2D can receive mouse events
		InputPickable = true;

		_fill = GetNodeOrNull<Polygon2D>("Fill");
		if (_fill != null)
			_fill.Color = BaseColor;
	}

	// Called by the world editor when it spawns this tile
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
			ToggleSelected();
		}
	}

	private void ToggleSelected()
	{
		_selected = !_selected;

		if (_fill != null)
			_fill.Color = _selected ? SelectedColor : BaseColor;

		GD.Print($"Hex clicked at {GridPosition}");
		// Later: open tile details / change elevation / etc.
	}
}
