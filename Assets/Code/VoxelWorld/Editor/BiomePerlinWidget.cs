namespace VoxelWorld.Editor
{
    using UnityEngine;
    using UnityEditor;
    using Voxelworld;

    public class BiomePerlinWidget
    {
        private readonly string _assetPath;
        private Color[] _texData;
        private readonly Texture2D _tex;

        private const int TEX_WIDTH = 100;
        private const int TEX_HEIGHT = 100;

        private const int WIDTH = 250;
        private const int HEIGHT = 420;

        private BiomePerlinSettings _perlinConfig;

        private Editor _editor;
        private static GUIStyle _groupStyle;
        private static GUIStyle _textStyle;

        private int _xPos;
        private int _yPos;
        private string _title;

		private readonly Color _color = new(0.5f,0.3f,0.7f,0.5f);

        public BiomePerlinWidget(string assetPath, int x, int y, string title)
        {
            _assetPath = assetPath;

            _perlinConfig = Utils.GetOrCreateScriptableObject<BiomePerlinSettings>(_assetPath);

            _tex = new Texture2D(TEX_WIDTH,TEX_HEIGHT);
            _texData = new Color[TEX_WIDTH * TEX_HEIGHT];

            _textStyle = new GUIStyle();
			_textStyle.alignment = TextAnchor.MiddleCenter;
			_textStyle.fontSize = 16;
			_textStyle.fontStyle = FontStyle.Normal;
			_textStyle.wordWrap = false;
			_textStyle.normal.textColor = Color.white;
			_textStyle.hover.textColor = Color.green;

			_groupStyle = new GUIStyle();
			_groupStyle.fontSize = 14;
			_groupStyle.fontStyle = FontStyle.Bold;
			_groupStyle.normal.textColor = new Color(0.8f,0.8f,0.8f);
			_groupStyle.padding = new RectOffset(5,5,5,5);

            _xPos = x;
            _yPos = y;
            _title = title;

            UpdateTexture();
        }

        public void Draw()
		{
			GUI.BeginGroup(new Rect(_xPos,_yPos,WIDTH,HEIGHT), _groupStyle);
				EditorGUI.DrawRect(new Rect(0,0,WIDTH,75),_color);
				GUI.Label(new Rect(0,0,WIDTH,20), _title, _groupStyle);
				
				if(_perlinConfig && !_editor)
				{
					_editor = Editor.CreateEditor(_perlinConfig);
				}

				if(_editor && _perlinConfig)
				{
					GUILayout.BeginArea(new Rect(5,30,WIDTH - 10,105));
						EditorGUI.BeginChangeCheck();
							SerializedObject obj = _editor.serializedObject;
							SerializedProperty property = obj.GetIterator();
							EditorGUIUtility.labelWidth = 60;
							while(property.NextVisible(true))
							{
								if(property.displayName == "Script") continue;
								EditorGUILayout.PropertyField(property, true);
							}
							obj.ApplyModifiedProperties();
							EditorGUIUtility.labelWidth = 0;
						if(EditorGUI.EndChangeCheck())
						{
							UpdateTexture();
						}
					GUILayout.EndArea();
				}

				GUI.DrawTexture(new Rect(0,75,WIDTH,WIDTH), _tex);
			GUI.EndGroup();
		}

        public void UpdateTexture()
		{
			GetPerlin2D(ref _texData);
			_tex.SetPixels(_texData);
			_tex.Apply();
		}

        public void Destroy()
        {
            if (_editor != null)
			{
				Object.DestroyImmediate(_editor);
			}
        }

        private void GetPerlin2D(ref Color[] textData)
		{
			for (int y = 0; y < TEX_WIDTH; y++)
			{
				for (int x = 0; x < TEX_HEIGHT; x++)
				{
					float noise = MeshUtils.fBM(x, y, _perlinConfig.octaves, _perlinConfig.scale, 1f,0f) / _perlinConfig.octaves;
					textData[y * TEX_WIDTH + x] = new Color(noise, noise, noise);
				}
			}
		}	
    }
}
