using Godot;
using System;

/*
 *  WorldTerrainEditor
 *  -------------------
 *  This scene displays the hex tile world and handles all terrain, biomes,
 *  POIs, roads, and DB saving in later phases.
 *  
 *  For now: it receives full world-gen parameters and draws the base grid.
 *  
 *  (Actual world generation will be added next in steps.)
 */

public partial class WorldTerrainEditor : Node2D
{
	[Export] public PackedScene HexTileScene { get; set; }
	[Export] public float HexWidth = 64f;
	[Export] public float HexHeight = 64f;

	private Node2D _tileRoot;

	public override void _Ready()
	{
		_tileRoot = GetNode<Node2D>("TileRoot");

		// Show Create World menu immediately
		var window = GetNode<Window>("CanvasLayer/Control/CreateWorldWindow");
		window.PopupCentered();
	}

	public void GenerateWorld(
	int width,
	int height,
	long seed,
	bool useBoundaries,

	int continents,
	int minDistance,
	int maxDistance,
	int sizeVariance,      // NEW
	int irregularity,      // NEW

	int waterPercent,
	int waterBodies,
	int waterRiverChance,
	int maxRiversPerBody,

	int mountainPercent,
	int tallMountains,
	int mountainRiverChance,
	int maxRiversTM
)
{
	GD.Print($"Size Variance: {sizeVariance}");
	GD.Print($"Irregularity: {irregularity}");

	// Phase 1: Generate Elevation      <-- NEXT STEP
	// Phase 2: Apply Water/Mountain %
	// Phase 3: Rivers
	// etc...

	DrawGrid(width, height);
}


	// TEMPORARY FUNCTION (draws plain hex grid)
	private void DrawGrid(int width, int height)
	{
		_tileRoot.RemoveAndQueueFreeChildren();

		for (int row = 0; row < height; row++)
		{
			for (int col = 0; col < width; col++)
			{
				var hex = HexTileScene.Instantiate<Node2D>();
				hex.Position = GetHexPosition(col, row);

				if (hex is EditorHexTile tile)
					tile.Initialize(new Vector2I(col, row));

				_tileRoot.AddChild(hex);
			}
		}
	}

	private Vector2 GetHexPosition(int col, int row)
	{
		float x = col * HexWidth;

		// Offset every odd row by half a hex
		if ((row & 1) == 1)
			x += HexWidth * 0.5f;

		float y = row * (HexHeight * 0.75f);
		return new Vector2(x, y);
	}
}
