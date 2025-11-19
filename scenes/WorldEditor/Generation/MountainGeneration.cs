using Godot;
using System;
using System.Collections.Generic;


public static class MountainGenerator
{
	// Hex offsets for odd/even rows
	private static readonly Vector2I[] Even =
	{
		new(+1, 0), new(0, +1), new(-1, +1),
		new(-1, 0), new(-1, -1), new(0, -1)
	};

	private static readonly Vector2I[] Odd =
	{
		new(+1, 0), new(+1, +1), new(0, +1),
		new(-1, 0), new(0, -1), new(+1, -1)
	};

	// -------------------------------------------------------------
	// ENTRY
	// -------------------------------------------------------------
	public static void Generate(
		WorldGenerationProfile profile,
		RandomNumberGenerator rng,
		int[,] map)
	{
		if (profile.TallMountains <= 0 && profile.MountainPercent <= 0)
			return;

		var tallPeaks = PlaceTallPeaks(profile, rng, map);
		GrowRidges(profile, rng, map, tallPeaks);
		ApplyFoothills(profile, map);
	}

	// -------------------------------------------------------------
	// 1) Tall Mountain Peaks (+3)
	// 7% chance to spawn in water
	// -------------------------------------------------------------
	private static List<Vector2I> PlaceTallPeaks(
		WorldGenerationProfile profile,
		RandomNumberGenerator rng,
		int[,] map)
	{
		int width = profile.Width;
		int height = profile.Height;

		int target = profile.TallMountains;
		List<Vector2I> peaks = new(target);

		int maxAttempts = target * 80;
		int attempts = 0;

		int minDist = Math.Max(3, (width + height) / (target + 4));

		while (peaks.Count < target && attempts < maxAttempts)
		{
			attempts++;

			int x = rng.RandiRange(0, width - 1);
			int y = rng.RandiRange(0, height - 1);

			bool allowWaterSeed = rng.Randf() < 0.07f;  // 7% water spawn chance
			bool onLand = map[x, y] >= 0;
			bool valid = onLand || allowWaterSeed;

			if (!valid)
				continue;

			Vector2I cand = new(x, y);

			bool tooClose = false;
			foreach (var p in peaks)
			{
				if (cand.DistanceTo(p) < minDist)
				{
					tooClose = true;
					break;
				}
			}
			if (tooClose)
				continue;

			map[x, y] = 3;
			peaks.Add(cand);
		}

		GD.Print($"MountainGenerator: Tall peaks placed = {peaks.Count}");
		return peaks;
	}

	// -------------------------------------------------------------
	// 2) Ridge Growth (+2)
	// Ridges follow a global orientation (LOTR-style)
	// They also may extend slightly into water (soft fade)
	// -------------------------------------------------------------
	private static void GrowRidges(
		WorldGenerationProfile profile,
		RandomNumberGenerator rng,
		int[,] map,
		List<Vector2I> peaks)
	{
		if (peaks.Count == 0)
			return;

		int width = profile.Width;
		int height = profile.Height;

		int totalTiles = width * height;
		int target = (int)(totalTiles * (profile.MountainPercent / 100f));

		int current = CountElevations(map, 2);

		int[][] axisSets =
		{
			new [] { 0, 3 }, // E-W
			new [] { 1, 4 }, // SE-NW
			new [] { 2, 5 }  // SW-NE
		};

		int[] globalAxis = axisSets[rng.RandiRange(0, axisSets.Length - 1)];

		foreach (var peak in peaks)
		{
			int branches = rng.RandiRange(2, 3);

			for (int b = 0; b < branches; b++)
			{
				if (current >= target)
					return;

				int dir = globalAxis[rng.RandiRange(0, globalAxis.Length - 1)];
				int length = GetRidgeLength(profile, rng);

				Vector2I pos = peak;

				for (int i = 0; i < length; i++)
				{
					if (rng.Randf() < GetTurnChance(profile.Irregularity))
						dir = RotateDir(dir, rng);

					Vector2I next = Step(pos, dir);
					if (!InBounds(next, width, height))
						break;

					int elev = map[next.X, next.Y];

					// --------------------------------------
					// Soft water fade rule (“1.5 rule”)
					// --------------------------------------
					bool shallowWater = elev == -1;
					bool deepWater = elev <= -2;

					if (deepWater)
						break; // too deep

					if (shallowWater)
					{
						// Place a reduced-strength ridge footprint in water
						map[next.X, next.Y] = 1; // shallowified ridge
						current++;
						break;
					}

					// Normal land ridge
					if (elev < 2)
					{
						map[next.X, next.Y] = 2;
						current++;
					}

					pos = next;

					if (current >= target)
						break;
				}
			}
		}
	}

	private static int GetRidgeLength(WorldGenerationProfile profile, RandomNumberGenerator rng)
	{
		int baseLen = (profile.Width + profile.Height) / 2;
		float density = Mathf.Clamp(profile.MountainPercent / 100f, 0.1f, 1f);

		int minL = Mathf.Max(6, (int)(baseLen * 0.20f * density));
		int maxL = Mathf.Max(minL + 3, (int)(baseLen * 0.50f * density));

		return rng.RandiRange(minL, maxL);
	}

	private static float GetTurnChance(int irr)
	{
		irr = Mathf.Clamp(irr, 0, 5);
		return 0.02f + irr * 0.04f; // 2% → 22%
	}

	private static int RotateDir(int dir, RandomNumberGenerator rng)
	{
		int d = rng.RandiRange(0, 1) == 0 ? -1 : 1;
		dir = (dir + d + 6) % 6;
		return dir;
	}

	private static Vector2I Step(Vector2I p, int dir)
	{
		bool odd = (p.Y & 1) == 1;
		var offsets = odd ? Odd : Even;
		var d = offsets[dir];
		return new(p.X + d.X, p.Y + d.Y);
	}

	private static bool InBounds(Vector2I p, int w, int h)
		=> p.X >= 0 && p.X < w && p.Y >= 0 && p.Y < h;

	private static int CountElevations(int[,] map, int min)
	{
		int w = map.GetLength(0);
		int h = map.GetLength(1);
		int c = 0;

		for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
				if (map[x, y] >= min)
					c++;

		return c;
	}

	// -------------------------------------------------------------
	// 3) Foothills (+1)
	// -------------------------------------------------------------
	private static void ApplyFoothills(WorldGenerationProfile profile, int[,] map)
	{
		int width = profile.Width;
		int height = profile.Height;

		List<Vector2I> newFoothills = new();

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				if (map[x, y] != 2)
					continue;

				bool odd = (y & 1) == 1;
				var offs = odd ? Odd : Even;

				for (int i = 0; i < 6; i++)
				{
					Vector2I n = new(x + offs[i].X, y + offs[i].Y);
					if (!InBounds(n, width, height))
						continue;

					if (map[n.X, n.Y] == 0)  // land
						newFoothills.Add(n);
				}
			}
		}

		foreach (var p in newFoothills)
			map[p.X, p.Y] = 1;
	}
}
