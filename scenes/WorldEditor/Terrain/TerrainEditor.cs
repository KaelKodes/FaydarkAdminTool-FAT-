using Godot;
using System;

public class TerrainEditor
{
	public static TerrainEditor Instance { get; } = new TerrainEditor();

	public void Apply(EditorHexTile tile, WorldGenerationProfile profile, int[,] elevationMap)
	{
		GD.Print($"[Terrain Editor] Editing tile {tile.GridPosition}");

		// for now just toggle land/water
		int x = tile.GridPosition.X;
		int y = tile.GridPosition.Y;

		if (elevationMap[x, y] < 0)
			elevationMap[x, y] = 0;
		else
			elevationMap[x, y] = -1;

		tile.SetElevationPreview(elevationMap[x, y]);
	}
}
