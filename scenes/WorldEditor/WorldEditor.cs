using Godot;
using System;
using System.Collections.Generic;

/*
 *  WorldEditor
 *  -------------------
 *  - Receives world-gen parameters from CreateWorldWindow
 *  - Calls ContinentGenerator to build elevation map
 *  - Owns the reusable hex tile grid (EditorHexTile[,])
 *  - Applies elevation to tiles (no re-instancing on regen)
 *  - Routes tile clicks based on current editor mode
 */

public partial class WorldEditor : Node2D
{
	[Export] public PackedScene HexTileScene { get; set; }
	[Export] public float HexWidth = 64f;
	[Export] public float HexHeight = 64f;

	private Node2D _tileRoot;

	private enum EditorMode { Terrain, Biome, POI, Roads }
	private EditorMode _currentMode = EditorMode.Terrain;

	// Single reusable grid of tile instances
	private EditorHexTile[,] _tiles;

	// Generation state
	private WorldGenerationProfile _profile;
	private RandomNumberGenerator _rng;
	private int[,] _elevationMap;

	public override void _Ready()
	{
		_tileRoot = GetNode<Node2D>("TileRoot");

		var window = GetNode<CreateWorldWindow>("CanvasLayer/Control/CreateWorldWindow");
		window.SetEditor(this);
		window.PopupCentered();

		// Tool buttons
		GetNode<Button>("CanvasLayer/ToolsPanel/TerrainButton").Pressed += () => SetMode(EditorMode.Terrain);
		GetNode<Button>("CanvasLayer/ToolsPanel/BiomeButton").Pressed += () => SetMode(EditorMode.Biome);
		GetNode<Button>("CanvasLayer/ToolsPanel/POIButton").Pressed += () => SetMode(EditorMode.POI);
		GetNode<Button>("CanvasLayer/ToolsPanel/RoadsButton").Pressed += () => SetMode(EditorMode.Roads);
	}

	private void SetMode(EditorMode mode)
	{
		_currentMode = mode;
		GD.Print($"Editor mode => {_currentMode}");
	}

	/// <summary>
	/// Called by CreateWorldWindow when the admin clicks Generate.
	/// </summary>
	public void GenerateWorld(
		int width,
		int height,
		long seed,
		bool useBoundaries,

		int continents,
		int minDistance,
		int maxDistance,
		int sizeVariance,
		int irregularity,

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
		if (HexTileScene == null)
		{
			GD.PushError("WorldEditor.GenerateWorld(): HexTileScene is null. Assign it in the inspector.");
			return;
		}

		// 1) Build profile (pure data)
		_profile = new WorldGenerationProfile
		{
			Width = width,
			Height = height,
			Seed = seed,
			UseBoundaries = useBoundaries,

			Continents = continents,
			MinDistance = minDistance,
			MaxDistance = maxDistance,
			SizeVariance = sizeVariance,
			Irregularity = irregularity,

			WaterPercent = waterPercent,
			WaterBodies = waterBodies,
			WaterRiverChance = waterRiverChance,
			MaxRiversPerBody = maxRiversPerBody,

			MountainPercent = mountainPercent,
			TallMountains = tallMountains,
			MountainRiverChance = mountainRiverChance,
			MaxRiversFromTM = maxRiversTM
		};

		GD.Print($"WorldEditor: Generating world {width}x{height}, seed={seed}");

		// 2) Ensure tile grid exists for this size
		EnsureTileGrid(_profile.Width, _profile.Height);

		// 3) Init RNG
		_rng = new RandomNumberGenerator();
		_rng.Seed = (ulong)_profile.Seed;

		// 4) Generate elevation map via ContinentGenerator
		_elevationMap = ContinentGenerator.Generate(_profile, _rng);
		MountainGenerator.Generate(_profile, _rng, _elevationMap);

		// 5) Apply elevation to tiles
		ApplyElevationToTiles();
	}

	/// <summary>
	/// Ensures we have a single, reusable hex tile grid for the given size.
	/// If size changes, old tiles are freed and a new grid is created.
	/// </summary>
	private void EnsureTileGrid(int width, int height)
	{
		if (_tiles != null &&
			_tiles.GetLength(0) == width &&
			_tiles.GetLength(1) == height)
		{
			GD.Print("WorldEditor: Reusing existing tile grid.");
			return;
		}

		GD.Print("WorldEditor: Creating new tile grid.");

		// Clear old tiles if any
		foreach (Node child in _tileRoot.GetChildren())
			child.QueueFree();

		_tiles = new EditorHexTile[width, height];

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				Node2D hexNode = HexTileScene.Instantiate<Node2D>();
				hexNode.Position = GetHexPosition(x, y);

				if (hexNode is EditorHexTile tile)
				{
					tile.Initialize(new Vector2I(x, y));
					tile.OnTileClicked = OnTileClicked;
					_tiles[x, y] = tile;
				}
				else
				{
					GD.PushError("WorldEditor: HexTileScene instance is not an EditorHexTile.");
				}

				_tileRoot.AddChild(hexNode);
			}
		}
	}

	private void ApplyElevationToTiles()
	{
		if (_profile == null)
		{
			GD.PushError("WorldEditor.ApplyElevationToTiles(): _profile is NULL.");
			return;
		}

		if (_tiles == null)
		{
			GD.PushError("WorldEditor.ApplyElevationToTiles(): Tile array not initialized.");
			return;
		}

		if (_elevationMap == null)
		{
			GD.PushError("WorldEditor.ApplyElevationToTiles(): Elevation map not initialized.");
			return;
		}

		int width = _profile.Width;
		int height = _profile.Height;

		if (_tiles.GetLength(0) != width || _tiles.GetLength(1) != height)
		{
			GD.PushError("WorldEditor.ApplyElevationToTiles(): Tile array size does not match profile dimensions!");
			return;
		}

		if (_elevationMap.GetLength(0) != width || _elevationMap.GetLength(1) != height)
		{
			GD.PushError("WorldEditor.ApplyElevationToTiles(): Elevation map size does not match profile dimensions!");
			return;
		}

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				EditorHexTile tile = _tiles[x, y];
				if (tile == null)
					continue;

				int elevation = _elevationMap[x, y];
				tile.SetElevationPreview(elevation);
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

	// ----------------- Tile click routing -----------------

	private void OnTileClicked(EditorHexTile tile)
	{
		switch (_currentMode)
		{
			case EditorMode.Terrain:
				// Terrain editing will use _elevationMap later
				GD.Print($"[Terrain] Clicked {tile.GridPosition}");
				break;

			case EditorMode.Biome:
				GD.Print($"[Biome] Clicked {tile.GridPosition}");
				break;

			case EditorMode.POI:
				GD.Print($"[POI] Clicked {tile.GridPosition}");
				break;

			case EditorMode.Roads:
				GD.Print($"[Roads] Clicked {tile.GridPosition}");
				break;
		}
	}
}
