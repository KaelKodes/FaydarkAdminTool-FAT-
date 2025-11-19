using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Builds a base elevation map using:
///  - Continent seeds
///  - Noise-shaped growth
///  - Mild irregularity based on profile.Irregularity
///  - Land percent based on WaterPercent
/// 
/// Elevation values (for now):
///   -1 = ocean
///    0 = land (continents)
/// (Mountains and deep water will be added in later phases.)
/// </summary>
public static class ContinentGenerator
{
	private struct FrontierCell
	{
		public Vector2I Pos;
		public int SeedIndex;
		public int Distance;

		public FrontierCell(Vector2I pos, int seedIndex, int distance)
		{
			Pos = pos;
			SeedIndex = seedIndex;
			Distance = distance;
		}
	}

	// Neighbor offsets for odd-r horizontal layout (what your hex grid uses)
	private static readonly Vector2I[] EvenRowOffsets =
	{
		new Vector2I(+1, 0),
		new Vector2I(-1, 0),
		new Vector2I(0, -1),
		new Vector2I(-1, -1),
		new Vector2I(0, +1),
		new Vector2I(-1, +1),
	};

	private static readonly Vector2I[] OddRowOffsets =
	{
		new Vector2I(+1, 0),
		new Vector2I(-1, 0),
		new Vector2I(+1, -1),
		new Vector2I(0, -1),
		new Vector2I(+1, +1),
		new Vector2I(0, +1),
	};

	/// <summary>
	/// Main entry point. Returns a width x height elevation map.
	/// </summary>
	public static int[,] Generate(WorldGenerationProfile profile, RandomNumberGenerator rng)
	{
		int width = profile.Width;
		int height = profile.Height;

		int[,] elevation = new int[width, height];

		// Initialize: everything ocean (-1)
		for (int y = 0; y < height; y++)
		for (int x = 0; x < width; x++)
			elevation[x, y] = -1;

		// Total tiles & target land count (land = 100 - water%)
		int totalTiles = width * height;
		int landPercent = Mathf.Clamp(100 - profile.WaterPercent, 5, 95);
		int targetLandTiles = Mathf.Max(
			profile.Continents, 
			(int)MathF.Round(totalTiles * (landPercent / 100.0f))
		);

		// Place seeds
		List<ContinentSeed> seeds = PlaceSeeds(profile, rng);
		if (seeds.Count == 0)
		{
			GD.PushError("ContinentGenerator.Generate(): No seeds placed.");
			return elevation;
		}

		// Build noise used to shape the coasts
		FastNoiseLite noise = BuildNoise(profile);

		// Multi-source BFS growth
		int[,] owner = new int[width, height];
		for (int y = 0; y < height; y++)
		for (int x = 0; x < width; x++)
			owner[x, y] = -1;

		int[] continentLand = new int[seeds.Count];

		Queue<FrontierCell> queue = new Queue<FrontierCell>();

		// Initialize with seed centers
		for (int i = 0; i < seeds.Count; i++)
		{
			Vector2I c = seeds[i].Center;
			if (!IsInBounds(c, width, height))
				continue;

			if (owner[c.X, c.Y] != -1)
				continue;

			owner[c.X, c.Y] = i;
			elevation[c.X, c.Y] = 0;
			continentLand[i] = 1;
			queue.Enqueue(new FrontierCell(c, i, 0));
		}

		int landCount = 0;
		for (int i = 0; i < continentLand.Length; i++)
			landCount += continentLand[i];

		int maxDist = Mathf.Max(1, profile.MaxDistance);

		// Average desired continent share (used to bias sizes)
		float avgShare = targetLandTiles / (float)Math.Max(1, seeds.Count);

		while (queue.Count > 0 && landCount < targetLandTiles)
		{
			FrontierCell cell = queue.Dequeue();
			if (cell.Distance > maxDist)
				continue;

			foreach (Vector2I npos in GetNeighbors(cell.Pos, width, height))
			{
				if (owner[npos.X, npos.Y] != -1)
					continue;

				int seedIndex = cell.SeedIndex;
				ContinentSeed seed = seeds[seedIndex];

				// Distance from seed
				float distFromSeed = cell.Distance + 1;
				float distFactor = 1f - (distFromSeed / maxDist);
				if (distFactor <= 0f)
					continue;

				// Noise factor 0..1
				float n = noise.GetNoise2D(npos.X, npos.Y);
				float n01 = 0.5f * (n + 1f);

				// Base threshold around "coastline"
				float baseThresh = 0.48f;

				// Irregularity affects how much noise wiggles the coast
				float irr = Mathf.Clamp(profile.Irregularity, 0, 5);
				float irrBias = (irr - 2f) * 0.04f; // between about -0.08..+0.12
				float threshold = baseThresh + irrBias;

				// Random jitter scaling with irregularity
				float jitter = (rng.Randf() - 0.5f) * 0.2f * irr;

				// Seed size bias (bigger SizeWeight => more likely to keep growing)
				float desiredThis = avgShare * (seed.SizeWeight / (float)Math.Max(1, seed.SizeWeight));
				float overRatio = continentLand[seedIndex] / Math.Max(1f, desiredThis);
				float sizeFactor = overRatio > 1f ? 1f / overRatio : 1f;

				// Final score
				float score = distFactor + (n01 - threshold) + jitter;
				score *= sizeFactor;

				if (score <= 0f)
				{
					// Not land, but we still might enqueue as frontier for other seeds
					continue;
				}

				// Accept as land
				owner[npos.X, npos.Y] = seedIndex;
				elevation[npos.X, npos.Y] = 0;
				continentLand[seedIndex]++;
				landCount++;

				// Continue frontier expansion
				queue.Enqueue(new FrontierCell(npos, seedIndex, cell.Distance + 1));

				if (landCount >= targetLandTiles)
					break;
			}
		}

		// Smoothing passes: fill tiny water holes and remove tiny land specks
		SmoothCoastlines(elevation, width, height);

		return elevation;
	}

	// ----------------- Seed placement -----------------

	private static List<ContinentSeed> PlaceSeeds(WorldGenerationProfile profile, RandomNumberGenerator rng)
	{
		var seeds = new List<ContinentSeed>();

		int width = profile.Width;
		int height = profile.Height;

		int attempts = 0;
		int maxAttempts = profile.Continents * 50;

		// Target land per continent used to scale SizeWeight a bit more sensibly
		int totalTiles = width * height;
		int landPercent = Mathf.Clamp(100 - profile.WaterPercent, 5, 95);
		int targetLandTiles = (int)MathF.Round(totalTiles * (landPercent / 100.0f));
		int baseSizePerContinent = Math.Max(10, targetLandTiles / Math.Max(1, profile.Continents));

		while (seeds.Count < profile.Continents && attempts < maxAttempts)
		{
			attempts++;

			int x = rng.RandiRange(0, width - 1);
			int y = rng.RandiRange(0, height - 1);
			var candidate = new Vector2I(x, y);

			bool tooClose = false;
			foreach (var existing in seeds)
			{
				int dx = existing.Center.X - candidate.X;
				int dy = existing.Center.Y - candidate.Y;
				float dist = Mathf.Sqrt(dx * dx + dy * dy);

				if (dist < profile.MinDistance)
				{
					tooClose = true;
					break;
				}
			}

			if (tooClose)
				continue;

			// SizeWeight around baseSizePerContinent ± SizeVariance%
			float varianceFactor = profile.SizeVariance / 100.0f; // 0..1 expected
			float randFactor = 1f + (rng.Randf() * 2f - 1f) * varianceFactor; // (1 - var) .. (1 + var)
			int sizeWeight = Mathf.Max(5, (int)(baseSizePerContinent * randFactor));

			var seed = new ContinentSeed(candidate, sizeWeight, profile.Irregularity);
			seeds.Add(seed);
		}

		if (seeds.Count < profile.Continents)
		{
			GD.PushWarning(
				$"ContinentGenerator: Requested {profile.Continents} continents, " +
				$"but only placed {seeds.Count} with MinDistance={profile.MinDistance}."
			);
		}

		GD.Print($"Placed {seeds.Count} continent seeds:");
		foreach (var s in seeds)
			GD.Print($"  Seed at {s.Center} sizeWeight={s.SizeWeight}");

		return seeds;
	}

	// ----------------- Noise -----------------

	private static FastNoiseLite BuildNoise(WorldGenerationProfile profile)
	{
		var noise = new FastNoiseLite();

		// Godot noise seed is int, profile.Seed is long
		noise.Seed = (int)(profile.Seed & 0x7FFFFFFF);
		noise.NoiseType   = FastNoiseLite.NoiseTypeEnum.SimplexSmooth;
		noise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;

		noise.FractalOctaves = 4;
		noise.FractalLacunarity = 2.0f;
		noise.FractalGain = 0.5f;

		// Frequency tuned for "big shapes" on typical 25x25 to 100x100 maps
		float baseFreq = 0.07f; // you can tweak this later from UI if desired
		noise.Frequency = baseFreq;

		return noise;
	}

	// ----------------- Smoothing -----------------

	private static void SmoothCoastlines(int[,] elevation, int width, int height)
	{
		// Pass 1: fill tiny water holes (water surrounded by land)
		int[,] pass1 = (int[,])elevation.Clone();

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				if (elevation[x, y] != -1)
					continue;

				int landNeighbors = CountNeighbors(elevation, width, height, new Vector2I(x, y), 0);
				if (landNeighbors >= 5) // mostly land around
					pass1[x, y] = 0;
			}
		}

		Array.Copy(pass1, elevation, elevation.Length);

		// Pass 2: remove tiny land specks (land isolated in water)
		int[,] pass2 = (int[,])elevation.Clone();

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				if (elevation[x, y] != 0)
					continue;

				int landNeighbors = CountNeighbors(elevation, width, height, new Vector2I(x, y), 0);
				if (landNeighbors <= 2)
					pass2[x, y] = -1;
			}
		}

		Array.Copy(pass2, elevation, elevation.Length);
	}

	private static int CountNeighbors(int[,] map, int width, int height, Vector2I pos, int matchElevation)
	{
		int count = 0;
		foreach (Vector2I n in GetNeighbors(pos, width, height))
		{
			if (map[n.X, n.Y] == matchElevation)
				count++;
		}
		return count;
	}

	// ----------------- Helpers -----------------

	private static IEnumerable<Vector2I> GetNeighbors(Vector2I pos, int width, int height)
	{
		Vector2I[] offsets = (pos.Y & 1) == 0 ? EvenRowOffsets : OddRowOffsets;

		for (int i = 0; i < offsets.Length; i++)
		{
			Vector2I n = pos + offsets[i];
			if (IsInBounds(n, width, height))
				yield return n;
		}
	}

	private static bool IsInBounds(Vector2I p, int width, int height)
	{
		return p.X >= 0 && p.X < width && p.Y >= 0 && p.Y < height;
	}
}
