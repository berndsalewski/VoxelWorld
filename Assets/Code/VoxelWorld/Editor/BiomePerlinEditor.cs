namespace Voxelworld.Editor
{
	using UnityEngine;
	using UnityEditor;
    using VoxelWorld.Editor;
    using System;

    public class BiomePerlinEditor : EditorWindow
	{
		private const string BIOME_TEMPERATURE_SETTINGS_PATH = "Assets/Configs/Biome_Temperature.asset";
		private BiomePerlinWidget _temperatureWidget;
		private const string BIOME_HUMIDITY_SETTINGS_PATH = "Assets/Configs/Biome_Humidity.asset";
		private BiomePerlinWidget _humidityWidget;
		private const string BIOME_CONTINENTALNESS_SETTINGS_PATH = "Assets/Configs/Biome_Continentalness.asset";
		private BiomePerlinWidget _continentalnessWidget;
		private const string BIOME_EROSION_SETTINGS_PATH = "Assets/Configs/Biome_Erosion.asset";
		private BiomePerlinWidget _erosionWidget;

		private const string BIOME_LEVEL_TEMPERATURE_PATH = "Assets/Configs/Biome_Levels_Temperature.asset";
		private MultiRangeSlider _biomeLevelsTemperatureSlider;

		private const string BIOME_LEVEL_HUMIDITY_PATH = "Assets/Configs/Biome_Levels_Humidity.asset";
		private MultiRangeSlider _biomeLevelsHumiditySlider;

		private Texture2D _biomeOutput;
		private Color[] _biomeOutputPixels;

		[MenuItem("Tools/Biome Perlin Editor")]
		static public void ShowWindow()
		{
			GetWindow<BiomePerlinEditor>("Biome Perlin Editor");
		}

		private void OnEnable()
		{
			_temperatureWidget = new BiomePerlinWidget(BIOME_TEMPERATURE_SETTINGS_PATH, 5,5, "Temperature");
			_humidityWidget = new BiomePerlinWidget(BIOME_HUMIDITY_SETTINGS_PATH, 260,5, "Humidity");
			_continentalnessWidget = new BiomePerlinWidget(BIOME_CONTINENTALNESS_SETTINGS_PATH, 515,5, "Continentalness");
			_erosionWidget = new BiomePerlinWidget(BIOME_EROSION_SETTINGS_PATH, 770,5, "Erosion");
			
			_biomeLevelsTemperatureSlider = new MultiRangeSlider(BIOME_LEVEL_TEMPERATURE_PATH, new Rect(5f,450f,300,30), -1f, 1f, 5, "Temperature Zones");
			_biomeLevelsHumiditySlider = new MultiRangeSlider(BIOME_LEVEL_HUMIDITY_PATH, new Rect(310f,450f,300,30), -1f, 1f, 5, "Humidity Zones");

			_biomeOutput = new (200,200);	
		}

		Color col;
		private void OnGUI()
		{	
			_temperatureWidget.Draw();
			_humidityWidget.Draw();
			_continentalnessWidget.Draw();
			_erosionWidget.Draw();
			_biomeLevelsTemperatureSlider.Draw();
			_biomeLevelsHumiditySlider.Draw();			

			//TODO move to own mini tool
			col = EditorGUI.ColorField(new Rect(5,500,120,20),col);
			if(GUI.Button(new Rect(5,525,120,20),"Copy to Clipboard"))
			{
				EditorGUIUtility.systemCopyBuffer = $"{col.r}f, {col.g}f, {col.b}f, {col.a}f";
			}

			for(int x = 0; x < 200; x++)
			{
				for (int y = 0; y < 200; y++)
				{
					GetBiomeAt(x,y);
				}
			}
		}

        private void GetBiomeAt(int x, int y)
        {
            throw new NotImplementedException();
        }

        private void OnDisable()
		{
			_temperatureWidget.Destroy();
			_humidityWidget.Destroy();
			_continentalnessWidget.Destroy();
			_erosionWidget.Destroy();
		}

		
	}
}