using Godot;

public class WorldGenerationProfile
{
	public int Width;
	public int Height;
	public long Seed;
	public bool UseBoundaries;

	// Continents
	public int Continents;
	public int MinDistance;
	public int MaxDistance;
	public int SizeVariance;   // 0–100
	public int Irregularity;   // 0–5

	// Water
	public int WaterPercent;
	public int WaterBodies;
	public int WaterRiverChance;
	public int MaxRiversPerBody;

	// Mountains
	public int MountainPercent;
	public int TallMountains;
	public int MountainRiverChance;
	public int MaxRiversFromTM;
}
