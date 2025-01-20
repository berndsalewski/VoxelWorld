namespace VoxelWorld.Editor
{
    using System;
    using UnityEditor;
	using UnityEngine;
	using Voxelworld;

    public class MultiRangeSlider
	{
		private const float HANDLE_WIDTH = 5f;
		private Rect _sliderRect;
		private float _totalRangeMin;
		private float _totalRangeMax;

		private int _draggingHandleIndex = -1; 

		private bool _isMouseDown;
		private string _title;

		private BiomeLevels _biomeLevels;

		public MultiRangeSlider(string assetPath, Rect sliderRect, float min, float max, int numSubRanges, string title = "")
		{
			_sliderRect = sliderRect;
			_title = title;
			_totalRangeMin = min;
			_totalRangeMax = max;

			_biomeLevels = Utils.GetOrCreateScriptableObject<BiomeLevels>(assetPath);
			if(_biomeLevels.RangeValues == null)
			{
				_biomeLevels.RangeValues = new float[numSubRanges + 1];
				for(int i = 0; i <= numSubRanges; i++)
				{
					_biomeLevels.RangeValues[i] = _totalRangeMin + (i * ((_totalRangeMax - _totalRangeMin) / numSubRanges));
				}
			}
		}

		public void Draw()
		{
			EditorGUI.DrawRect(_sliderRect, new Color(0.2240566f, 0.4947043f, 0.5f));

			EditorGUI.LabelField(new Rect(_sliderRect.x, _sliderRect.y-40, _sliderRect.width,20), _title, EditorStyles.boldLabel);
			
			// draw rects for the ranges
			for(int i = 0; i < _biomeLevels.RangeValues.Length - 1; i++)
			{
				float start = Mathf.InverseLerp(_totalRangeMin,_totalRangeMax,_biomeLevels.RangeValues[i]);
				float end = Mathf.InverseLerp(_totalRangeMin,_totalRangeMax,_biomeLevels.RangeValues[i + 1]);

				Rect subRangeRect = new Rect(
					Mathf.Lerp(_sliderRect.x, _sliderRect.xMax, start),
					_sliderRect.y + 5,
					Mathf.Lerp(_sliderRect.x, _sliderRect.xMax, end) - Mathf.Lerp(_sliderRect.x, _sliderRect.xMax, start),
					_sliderRect.height - 10
				);

				EditorGUI.DrawRect(subRangeRect, Color.Lerp(Color.blue,Color.green, i / (_biomeLevels.RangeValues.Length-1f)));
			}

			// draggable handles 
			for(int i = 1; i < _biomeLevels.RangeValues.Length - 1; i++)
			{
				float normalized = Mathf.InverseLerp(_totalRangeMin,_totalRangeMax,_biomeLevels.RangeValues[i]);
				float handlePos = Mathf.Lerp(_sliderRect.x, _sliderRect.xMax, normalized);

				Rect handleRect = new Rect(handlePos - HANDLE_WIDTH / 2,_sliderRect.y,HANDLE_WIDTH, _sliderRect.height);

				// handle input
				if(Event.current.type == EventType.MouseDown && handleRect.Contains(Event.current.mousePosition))
				{
					//start dragging
					_isMouseDown = true;
					_draggingHandleIndex = i; 
					Event.current.Use();
				}
				else if(Event.current.type == EventType.MouseUp)
				{
					_isMouseDown = false;
				}
				
				if(_isMouseDown && _draggingHandleIndex == i)
				{
					EditorGUI.DrawRect(handleRect, new Color(0.6f,0.1f,0.1f));
				}
				else
				{
					EditorGUI.DrawRect(handleRect, new Color(0.7f,0.2f,0.2f));
				}

				EditorGUI.LabelField(
					new Rect(handleRect.x,handleRect.y-12,50,10),
					_biomeLevels.RangeValues[i].ToString("0.00"), 
					new GUIStyle(EditorStyles.label));
			}

			// handle dragging
			if(_draggingHandleIndex != -1)
			{
				if(Event.current.type == EventType.MouseDrag)
				{
					float mousePosNormalized = Mathf.InverseLerp(_sliderRect.x,_sliderRect.xMax,Event.current.mousePosition.x);
					_biomeLevels.RangeValues[_draggingHandleIndex] = Mathf.Clamp(
						 (float)Math.Round(Mathf.Lerp(_totalRangeMin,_totalRangeMax,mousePosNormalized),2),
						_biomeLevels.RangeValues[_draggingHandleIndex - 1],
						_biomeLevels.RangeValues[_draggingHandleIndex + 1]);
					Event.current.Use();
				}
				else if(Event.current.type == EventType.MouseUp)
				{
					// stop dragging
					_draggingHandleIndex = -1; 
					Event.current.Use();
				}
			}
		}
	}
}
