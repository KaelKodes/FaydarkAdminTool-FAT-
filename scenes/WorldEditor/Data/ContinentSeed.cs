using Godot;

public class ContinentSeed
{
	public Vector2I Center;    // grid coords (col,row)
	public int SizeWeight;     // relative size (from SizeVariance)
	public int Irregularity;   // 0–5 for this continent (can vary later)

	public ContinentSeed(Vector2I center, int sizeWeight, int irregularity)
	{
		Center = center;
		SizeWeight = sizeWeight;
		Irregularity = irregularity;
	}
}
