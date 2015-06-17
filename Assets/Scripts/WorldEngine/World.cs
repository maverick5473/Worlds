﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

public class TerrainCell {

	[XmlIgnore]
	public World World;
	
	[XmlAttribute]
	public int Longitude;
	[XmlAttribute]
	public int Latitude;

	[XmlAttribute]
	public float Altitude;
	[XmlAttribute]
	public float Rainfall;
	[XmlAttribute]
	public float Temperature;

	public List<Biome> Biomes = new List<Biome>();
	public List<float> BiomePresences = new List<float>();
}

[XmlRoot]
public class World {

	public const int NumContinents = 7;
	public const float ContinentMinWidthFactor = 2.5f;
	public const float ContinentMaxWidthFactor = 6f;

	public const float MinPossibleAltitude = -15000;
	public const float MaxPossibleAltitude = 15000;
	public const float MountainRangeMixFactor = 0.075f;
	public const float MountainRangeWidthFactor = 25f;
	public const float TerrainNoiseFactor1 = 0.15f;
	public const float TerrainNoiseFactor2 = 0.15f;
	public const float TerrainNoiseFactor3 = 0.1f;
	public const float TerrainNoiseFactor4 = 2.5f;
	
	public const float MinPossibleRainfall = 0;
	public const float MaxPossibleRainfall = 7500;
	public const float RainfallDrynessOffsetFactor = 0.005f;
	
	public const float MinPossibleTemperature = -35;
	public const float MaxPossibleTemperature = 30;
	public const float TemperatureAltitudeFactor = 1f;
	
	public float MaxAltitude = MinPossibleAltitude;
	public float MinAltitude = MaxPossibleAltitude;
	
	public float MaxRainfall = MinPossibleRainfall;
	public float MinRainfall = MaxPossibleRainfall;

	public float MaxTemperature = MinPossibleTemperature;
	public float MinTemperature = MaxPossibleTemperature;
	
	[XmlAttribute]
	public int Width { get; private set; }
	[XmlAttribute]
	public int Height { get; private set; }
	
	[XmlAttribute]
	public int Seed { get; private set; }

	public TerrainCell[][] Terrain;

	private Vector2[] _continentOffsets;
	private float[] _continentWidths;
	private float[] _continentHeights;

	private bool _initialized = false;

	public World () {
	}

	public World (int width, int height, int seed) {
	
		Width = width;
		Height = height;
		Seed = seed;

		Terrain = new TerrainCell[width][];

		for (int i = 0; i < width; i++)
		{
			TerrainCell[] column = new TerrainCell[height];

			for (int j = 0; j < height; j++)
			{
				TerrainCell cell = new TerrainCell();
				cell.World = this;
				cell.Longitude = i;
				cell.Latitude = j;

				column[j] = cell;
			}

			Terrain[i] = column;
		}

		_continentOffsets = new Vector2[NumContinents];
		_continentHeights = new float[NumContinents];
		_continentWidths = new float[NumContinents];
	}

	public void FinalizeInit () {
		
		for (int i = 0; i < Width; i++)
		{
			for (int j = 0; j < Height; j++)
			{
				TerrainCell cell = Terrain[i][j];
				cell.World = this;
			}
		}
	}

	public void Initialize () {

		if (_initialized)
			return;

		_initialized = true;

		Random.seed = Seed;
	}

	public void Generate () {

		GenerateTerrainAltitude ();
		GenerateTerrainRainfall ();
		GenerateTerrainTemperature ();
		GenerateTerrainBiome ();
	}

	private void GenerateContinents () {

		for (int i = 0; i < NumContinents; i++)
		{
			_continentOffsets[i] = new Vector2(
				Mathf.Repeat(Random.Range(Width*i*2/5, Width*(i + 2)*2/5), Width),
				Random.Range(Height * 1f/6f, Height * 5f/6f));
			_continentWidths[i] = Random.Range(ContinentMinWidthFactor, ContinentMaxWidthFactor);
			_continentHeights[i] = Random.Range(ContinentMinWidthFactor, ContinentMaxWidthFactor);
		}
	}
	
	private float GetContinentModifier (int x, int y) {

		float maxValue = 0;

		float betaFactor = Mathf.Sin(Mathf.PI * y / Height);

		for (int i = 0; i < NumContinents; i++)
		{
			Vector2 continentOffset = _continentOffsets[i];
			float contX = continentOffset.x;
			float contY = continentOffset.y;

			float distX = Mathf.Min(Mathf.Abs(contX - x), Mathf.Abs(Width + contX - x));
			distX = Mathf.Min(distX, Mathf.Abs(contX - x - Width));
			distX *= betaFactor;

			float distY = Mathf.Abs(contY - y);
			
			float continentWidth = _continentWidths[i];
			float continentHeight = _continentHeights[i];

			float dist = new Vector2(distX*continentWidth, distY*continentHeight).magnitude;

			float value = Mathf.Max(0, 1f - dist/((float)Width));

			maxValue = Mathf.Max(maxValue, value);
		}

		return maxValue;
	}

	private void GenerateTerrainAltitude () {

		GenerateContinents();
		
		int sizeX = Width;
		int sizeY = Height;

		float radius1 = 0.5f;
		float radius2 = 4f;
		float radius3 = 4f;
		float radius4 = 8f;
		float radius5 = 16f;

		Vector3 offset1 = GenerateRandomOffsetVector();
		Vector3 offset2 = GenerateRandomOffsetVector();
		Vector3 offset3 = GenerateRandomOffsetVector();
		Vector3 offset4 = GenerateRandomOffsetVector();
		Vector3 offset5 = GenerateRandomOffsetVector();
		
		for (int i = 0; i < sizeX; i++)
		{
			float beta = (i / (float)sizeX) * Mathf.PI * 2;
			
			for (int j = 0; j < sizeY; j++)
			{
				float alpha = (j / (float)sizeY) * Mathf.PI;

				float value1 = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius1, offset1);
				float value2 = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius2, offset2);
				float value3 = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius3, offset3);
				float value4 = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius4, offset4);
				float value5 = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius5, offset5);

				float valueA = GetContinentModifier(i, j);
				valueA = MathUtility.MixValues(valueA, value3, TerrainNoiseFactor1);
				valueA = valueA * MathUtility.MixValues(1, value4, TerrainNoiseFactor2);
				valueA = valueA * MathUtility.MixValues(1, value5, TerrainNoiseFactor3);

				float valueB = (valueA * 0.04f) + 0.48f;
				
				float valueC = MathUtility.MixValues(value1, value2, MountainRangeMixFactor);
				valueC = GetMountainRangeNoiseFromRandomNoise(valueC);
				valueC = MathUtility.MixValues(valueC, value3, TerrainNoiseFactor1 * TerrainNoiseFactor4);
				valueC = valueC * MathUtility.MixValues(1, value4, TerrainNoiseFactor2 * TerrainNoiseFactor4);
				valueC = valueC * MathUtility.MixValues(1, value5, TerrainNoiseFactor3 * TerrainNoiseFactor4);
				
				float valueD = MathUtility.MixValues(valueA, valueC, 0.25f);
				valueD = MathUtility.MixValues(valueD, valueC, 0.1f);
				valueD = MathUtility.MixValues(valueD, valueB, 0.1f);

				float altitude = CalculateAltitude(valueD);
				Terrain[i][j].Altitude = altitude;

				if (altitude > MaxAltitude) MaxAltitude = altitude;
				if (altitude < MinAltitude) MinAltitude = altitude;
			}
		}
	}

	private Vector3 GenerateRandomOffsetVector () {
	
		return Random.insideUnitSphere * 1000;
	}

	private float GetRandomNoiseFromPolarCoordinates (float alpha, float beta, float radius, Vector3 offset) {

		Vector3 pos = MathUtility.GetCartesianCoordinates(alpha,beta,radius) + offset;
		
		return PerlinNoise.GetValue(pos.x, pos.y, pos.z);
	}
	
	private float GetMountainRangeNoiseFromRandomNoise(float noise) {

		noise = (noise * 2) - 1;
		
		float value1 = -Mathf.Exp(-Mathf.Pow(noise*MountainRangeWidthFactor + 1, 2));
		float value2 = Mathf.Exp(-Mathf.Pow(noise*MountainRangeWidthFactor - 1, 2));
		float value3 = -Mathf.Exp(-Mathf.Pow(noise*MountainRangeWidthFactor + MountainRangeWidthFactor/2f, 2));
		float value4 = Mathf.Exp(-Mathf.Pow(noise*MountainRangeWidthFactor - MountainRangeWidthFactor/2f, 2));
		
		float value = (value1 + value2 + value3 + value4 + 1) / 2f;
		
		return value;
	}

	private float CalculateAltitude (float value) {
	
		float span = MaxPossibleAltitude - MinPossibleAltitude;

		return (value * span) + MinPossibleAltitude;
	}
	
	private void GenerateTerrainRainfall () {
		
		int sizeX = Width;
		int sizeY = Height;
		
		float radius = 2f;
		
		Vector3 offset = GenerateRandomOffsetVector();
		
		for (int i = 0; i < sizeX; i++)
		{
			float beta = (i / (float)sizeX) * Mathf.PI * 2;
			
			for (int j = 0; j < sizeY; j++)
			{
				TerrainCell cell = Terrain[i][j];
				
				float alpha = (j / (float)sizeY) * Mathf.PI;
				
				float value = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius, offset);

				float latitudeFactor = (alpha * 0.9f) + (value * 0.2f * Mathf.PI) - 0.1f;
				float latitudeModifier1 = (1.5f * Mathf.Sin(latitudeFactor)) - 0.5f;
				float latitudeModifier2 = Mathf.Cos(latitudeFactor);

				int offCellX = (Width + i + (int)Mathf.Floor(latitudeModifier2 * Width/20f)) % Width;
				int offCellX2 = (Width + i + (int)Mathf.Floor(latitudeModifier2 * Width/10f)) % Width;

				TerrainCell offCell = Terrain[offCellX][j];
				TerrainCell offCell2 = Terrain[offCellX2][j];

				float altitude = Mathf.Max(0, cell.Altitude);
				float offAltitude = Mathf.Max(0, offCell.Altitude);
				float offAltitude2 = Mathf.Max(0, offCell2.Altitude);

				float altitudeModifier = (altitude - (offAltitude * 1.5f) - (offAltitude2 * 1.5f)) / MaxPossibleAltitude;
				float rainfallValue = MathUtility.MixValues(latitudeModifier1, altitudeModifier, 0.63f);
				//rainfallValue = MathUtility.MixValues(rainfallValue * rainfallValue, rainfallValue, 0.3f);

				float rainfall = Mathf.Min(MaxPossibleRainfall, CalculateRainfall(rainfallValue));
				cell.Rainfall = rainfall;

				if (rainfall > MaxRainfall) MaxRainfall = rainfall;
				if (rainfall < MinRainfall) MinRainfall = rainfall;
			}
		}
	}
	
	private void GenerateTerrainTemperature () {
		
		int sizeX = Width;
		int sizeY = Height;
		
		float radius = 2f;
		
		Vector3 offset = GenerateRandomOffsetVector();
		
		for (int i = 0; i < sizeX; i++)
		{
			float beta = (i / (float)sizeX) * Mathf.PI * 2;
			
			for (int j = 0; j < sizeY; j++)
			{
				TerrainCell cell = Terrain[i][j];
				
				float alpha = (j / (float)sizeY) * Mathf.PI;
				
				float value = GetRandomNoiseFromPolarCoordinates(alpha, beta, radius, offset);

				float latitudeModifier = (alpha * 0.9f) + (value * 0.1f * Mathf.PI);
				
				float altitudeFactor = Mathf.Max(0, (cell.Altitude / MaxPossibleAltitude) * TemperatureAltitudeFactor);
				
				float temperature = CalculateTemperature(Mathf.Sin(latitudeModifier) - altitudeFactor);

				cell.Temperature = temperature;
				
				if (temperature > MaxTemperature) MaxTemperature = temperature;
				if (temperature < MinTemperature) MinTemperature = temperature;
			}
		}
	}

	private void GenerateTerrainBiome () {
		
		int sizeX = Width;
		int sizeY = Height;
		
		for (int i = 0; i < sizeX; i++) {

			for (int j = 0; j < sizeY; j++) {

				TerrainCell cell = Terrain [i] [j];

				float totalPresence = 0;

				Dictionary<Biome, float> biomePresences = new Dictionary<Biome, float> ();

				foreach (Biome biome in Biome.Biomes) {

					float presence = GetBiomePresence (cell, biome);

					if (presence <= 0) continue;

					biomePresences.Add(biome, presence);

					totalPresence += presence;
				}

				foreach (Biome biome in Biome.Biomes)
				{
					float presence = 0;

					if (biomePresences.TryGetValue(biome, out presence))
					{
						cell.Biomes.Add(biome);
						cell.BiomePresences.Add(presence/totalPresence);
					}
				}
			}
		}
	}

	private float GetBiomePresence (TerrainCell cell, Biome biome) {

		float presence = 1f;

		// Altitude

		float altitudeSpan = biome.MaxAltitude - biome.MinAltitude;

		float altitudeDiff = cell.Altitude - biome.MinAltitude;

		if (altitudeDiff < 0)
			return -1f;

		float altitudeFactor = altitudeDiff / altitudeSpan;
		
		if (altitudeFactor > 1)
			return -1f;

		if (altitudeFactor > 0.5f)
			altitudeFactor = 1f - altitudeFactor;

		presence *= altitudeFactor;

		// Rainfall
		
		float rainfallSpan = biome.MaxRainfall - biome.MinRainfall;
		
		float rainfallDiff = cell.Rainfall - biome.MinRainfall;
		
		if (rainfallDiff < 0)
			return -1f;
		
		float rainfallFactor = rainfallDiff / rainfallSpan;
		
		if (rainfallFactor > 1)
			return -1f;
		
		if (rainfallFactor > 0.5f)
			rainfallFactor = 1f - rainfallFactor;
		
		presence *= rainfallFactor;
		
		// Temperature
		
		float temperatureSpan = biome.MaxTemperature - biome.MinTemperature;
		
		float temperatureDiff = cell.Temperature - biome.MinTemperature;
		
		if (temperatureDiff < 0)
			return -1f;
		
		float temperatureFactor = temperatureDiff / temperatureSpan;
		
		if (temperatureFactor > 1)
			return -1f;
		
		if (temperatureFactor > 0.5f)
			temperatureFactor = 1f - temperatureFactor;
		
		presence *= temperatureFactor;

		return presence;
	}
	
	private float CalculateRainfall (float value) {

		float drynessOffset = MaxPossibleRainfall * RainfallDrynessOffsetFactor;
		
		float span = MaxPossibleRainfall - MinPossibleRainfall + drynessOffset;

		float rainfall = (value * span) + MinPossibleRainfall - drynessOffset;

		rainfall = Mathf.Clamp(rainfall, 0, MaxPossibleRainfall);
		
		return rainfall;
	}
	
	private float CalculateTemperature (float value) {
		
		float span = MaxPossibleTemperature - MinPossibleTemperature;
		
		float temperature = (value * span) + MinPossibleTemperature;
		
		temperature = Mathf.Clamp(temperature, MinPossibleTemperature, MaxPossibleTemperature);
		
		return temperature;
	}
}
