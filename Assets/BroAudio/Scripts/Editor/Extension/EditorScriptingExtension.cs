using UnityEditor;
using UnityEngine;
using System.Linq;
using Ami.BroAudio.Editor;
using static Ami.BroAudio.Editor.Setting.GlobalSettingEditorWindow;
using System;

namespace Ami.Extension
{
	public static class EditorScriptingExtension
	{
		public struct MultiLabel
		{
			public string Main;
			public string Left;
			public string Right;
		}

		public struct TabView
		{
			public string Label;
			public Action<Rect> OnDrawContent;
		}

		public static Rect GetRectAndIterateLine(IEditorDrawLineCounter drawer, Rect position)
		{
			Rect newRect = new Rect(position.x, position.y + drawer.SingleLineSpace * drawer.DrawLineCount + drawer.Offset, position.width, EditorGUIUtility.singleLineHeight);
			drawer.DrawLineCount++;

			return newRect;
		}

		public static Rect GetNextLineRect(IEditorDrawLineCounter drawer, Rect position)
		{
            Rect newRect = new Rect(position.x, position.y + drawer.SingleLineSpace * drawer.DrawLineCount + drawer.Offset, position.width, EditorGUIUtility.singleLineHeight);
            return newRect;
        }

		public static void SplitRectHorizontal(Rect origin, float firstRatio, float gap, out Rect rect1, out Rect rect2)
		{
			float halfGap = gap * 0.5f;
			rect1 = new Rect(origin.x, origin.y, origin.width * firstRatio - halfGap, origin.height);
			rect2 = new Rect(rect1.xMax + gap, origin.y, origin.width * (1 - firstRatio) - halfGap, origin.height);
		}

		public static void SplitRectHorizontal(Rect origin, int count, float gap, out Rect[] results)
		{
			results = SplitHorizontal(origin, count, gap, index => 
			{
				float ratio = 1f / count;
				if (index == count - 1)
				{
					return 1 - (ratio * (count - 1));
				}
				else
				{
					return ratio;
				}
			});
		}

		public static void SplitRectHorizontal(Rect origin,float gap, out Rect[] results,params float[] ratios)
		{
			results = null;
			if (ratios.Sum() != 1)
			{
				Debug.LogError("[Editor] Split ratio's sum should be 1");
				return;
			}

			results = SplitHorizontal(origin,ratios.Length,gap, index => ratios[index]);
		}

		private static Rect[] SplitHorizontal(Rect origin, int count, float gap, Func<int, float> getRatio)
		{
			Rect[] results = new Rect[count];
			for (int i = 0; i < count; i++)
			{
				float offsetWidth = i == 0 || i == count - 1 ? gap : gap * 0.5f;
				float newWidth = origin.width * getRatio.Invoke(i) - offsetWidth;
				float newX = i > 0 ? results[i - 1].xMax + gap : origin.x;
				results[i] = new Rect(newX, origin.y, newWidth, origin.height);
			}

			return results;
		}

		public static void SplitRectVertical(Rect origin, float firstRatio, float gap, out Rect rect1, out Rect rect2)
		{
			float halfGap = gap * 0.5f;
			rect1 = new Rect(origin.x, origin.y, origin.width, origin.height * firstRatio - halfGap);
			rect2 = new Rect(origin.x, rect1.yMax + gap, origin.width, origin.height * (1 - firstRatio) - halfGap);
		}

		public static void SplitRectVertical(Rect origin, float gap, out Rect[] outputRects, params float[] ratios)
		{
			if (ratios.Sum() != 1)
			{
				Debug.LogError("[Editor] Split ratio's sum should be 1");
				outputRects = null;
				return;
			}

			Rect[] results = new Rect[ratios.Length];

			for (int i = 0; i < results.Length; i++)
			{
				float offsetHeight = i == 0 || i == results.Length - 1 ? gap : gap * 0.5f;
				float newHeight = origin.height * ratios[i] - offsetHeight;
				float newY = i > 0 ? results[i - 1].yMax + gap : origin.y;
				results[i] = new Rect(origin.x,newY, origin.width,newHeight);
			}
			outputRects = results;
		}

		public static Rect DissolveHorizontal(this Rect origin,float dissolveRatio)
		{
			return new Rect(origin.xMin + dissolveRatio * origin.width, origin.y, (1 - dissolveRatio) * origin.width, origin.height);
		}

		public static Rect DeScope(this Rect inScopeRect, Rect scope, Vector2 offset = default)
		{
			Rect rect = new Rect(inScopeRect.position.DeScope(scope, offset), inScopeRect.size);
			rect.xMax = rect.xMax > scope.xMax ? scope.xMax : rect.xMax;
			rect.yMax = rect.yMax > scope.yMax ? scope.yMax : rect.yMax;
			return rect;
		}

		public static Vector3 DeScope(this Vector3 inScopePos, Rect scope, Vector3 offset = default)
		{
			return DeScope(inScopePos.ToVector2(),scope,offset.ToVector2());
		}

		public static Vector2 DeScope(this Vector2 inScopePos, Rect scope, Vector2 offset = default)
		{
			return new Vector2(inScopePos.x + scope.x + offset.x, inScopePos.y + scope.y + offset.y);
		}

		public static Rect Scoping(this Rect originRect, Rect scope, Vector2 offset = default)
		{
			Rect rect = new Rect(originRect.position.Scoping(scope, offset), originRect.size);
			rect.xMax = rect.xMax > scope.xMax ? scope.xMax : rect.xMax;
			rect.yMax = rect.yMax > scope.yMax ? scope.yMax : rect.yMax;
			return rect;
		}

		public static Vector3 Scoping(this Vector3 originPos, Rect scope, Vector3 offset = default)
		{
			return Scoping(originPos.ToVector2(), scope, offset.ToVector2());
		}

		public static Vector2 Scoping(this Vector2 originPos, Rect scope, Vector2 offset = default)
		{
			return new Vector2(originPos.x - scope.x + offset.x, originPos.y - scope.y + offset.y);
		}

		private static Vector2 ToVector2(this Vector3 vector) => vector;

		public static bool TryGetPropertyObject<T>(this SerializedProperty sourceProperty, string propertyPath, out T newProperty) where T : class
		{
			newProperty = null;
			if (sourceProperty == null)
			{
				return false;
			}

			newProperty = sourceProperty.FindPropertyRelative(propertyPath)?.objectReferenceValue as T;
			return newProperty != null;
		}

		public static void RichTextHelpBox(Rect position,string message, MessageType messageType)
		{
			RichTextHelpBox(position,message, GetIconName(messageType));
		}

		public static void RichTextHelpBox(string message, MessageType messageType)
		{
			RichTextHelpBox(message, GetIconName(messageType));
		}

		private static string GetIconName(MessageType messageType)
		{
			switch (messageType)
			{
				case MessageType.Info:
					return IconConstant.InfoMessage;
				case MessageType.Warning:
					return IconConstant.WarningMessage;
				case MessageType.Error:
					return IconConstant.ErrorMessage;
				default:
					return string.Empty;
			}
		}

		public static void RichTextHelpBox(Rect position,string message, string icon)
		{
			GUIContent content = GetRichTextContent(message, icon);
			EditorGUI.LabelField(position, content, GUIStyleHelper.RichTextHelpBox);
		}

		public static void RichTextHelpBox(string message, string icon)
		{
			GUIContent content = GetRichTextContent(message, icon);
			EditorGUILayout.LabelField(content, GUIStyleHelper.RichTextHelpBox);
		}

		private static GUIContent GetRichTextContent(string message, string icon)
		{
			return string.IsNullOrEmpty(icon) ? new GUIContent(message) : new GUIContent(message, EditorGUIUtility.IconContent(icon).image);
		}

		public static string GetBackingFieldName(string propertyName)
		{
			return $"<{propertyName}>k__BackingField";
		}

		public static string GetFieldName(string propertyName)
		{
			if(!string.IsNullOrEmpty(propertyName) && propertyName.Length > 0)
			{
				if (char.IsUpper(propertyName[0]))
				{
					propertyName = propertyName.Replace(propertyName[0], propertyName[0].ToLower());
				}
				return $"_{propertyName}";

			}
			return propertyName;
		}

		public static void DrawToggleGroup(Rect totalPosition, GUIContent label,SerializedProperty[] toggles, bool isAllowSwitchOff = true, int toggleCountPerLine  = 4)
		{
			if (toggles == null)
			{
				return;
			}

			Rect suffixRect = EditorGUI.PrefixLabel(totalPosition, label);
			float space = suffixRect.width / toggleCountPerLine;
			Rect toggleRect = new Rect(suffixRect);
			toggleRect.width = space;

			SerializedProperty currentActiveToggle = null;
			foreach(var toggle in toggles)
			{
				if(toggle.boolValue)
				{
					currentActiveToggle = toggle;
				}
			}

			for(int i = 0; i < toggles.Length;i++)
			{
				var toggle = toggles[i];
				if (EditorGUI.ToggleLeft(toggleRect, toggle.displayName, toggle.boolValue))
				{
					if(toggle != currentActiveToggle)
					{
						if(currentActiveToggle != null)
						{
							currentActiveToggle.boolValue = false;
						}
						currentActiveToggle = toggle;
					}
					toggle.boolValue = true;
				}
				else if (!isAllowSwitchOff && currentActiveToggle == null)
				{
					toggles[0].boolValue = true;
				}
				else
				{
					toggle.boolValue = false;
				}

				
				toggleRect.x += space;
				toggleRect.y += (i / toggleCountPerLine) * EditorGUIUtility.singleLineHeight;
			}
		}

		public static float DrawLogarithmicSlider_Horizontal(Rect position,float currentValue, float leftValue, float rightValue)
		{
			const float min = 0.0001f;
			if (leftValue <= 0f)
			{
				//Debug.LogWarning($"The left value of the LogarithmicSlider should be greater than 0. It has been set to the default value of {min}");
				leftValue = Mathf.Max(min, leftValue);
			}

			currentValue = currentValue == 0 ? leftValue : currentValue;
			float logValue = Mathf.Log10(currentValue);
			float logLeftValue = Mathf.Log10(leftValue);
			float logRightValue = Mathf.Log10(rightValue);

			float logResult = GUI.HorizontalSlider(position, logValue, logLeftValue, logRightValue);

			return Mathf.Pow(10,logResult);
		}

		public static int FontSizeToPixels(int fontSize)
		{
			// 16px = 12pt.
			return Mathf.RoundToInt(fontSize * (16f / 12f));
		}

		public static float Draw2SidesLabelSlider(MultiLabel labels,float value,float leftValue, float rightValue, params GUILayoutOption[] options)
		{
			float resultValue = EditorGUILayout.Slider(labels.Main, value, leftValue, rightValue, options);
			Rect lastRect = GUILayoutUtility.GetLastRect();
			float offsetY = 7f;
			float rightWordLength = 30f; // todo: calculate by word length?
			Rect leftRect = new Rect(EditorGUIUtility.labelWidth, lastRect.y + offsetY, EditorGUIUtility.fieldWidth, lastRect.height);
			Rect rightRect = new Rect(lastRect.xMax - EditorGUIUtility.fieldWidth - rightWordLength, lastRect.y + offsetY, EditorGUIUtility.fieldWidth, lastRect.height);

			GUIStyle lowerLeftMiniLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
			lowerLeftMiniLabel.alignment = TextAnchor.LowerLeft;

			EditorGUI.LabelField(leftRect, labels.Left, lowerLeftMiniLabel);
			EditorGUI.LabelField(rightRect, labels.Right, lowerLeftMiniLabel);

			return resultValue;
		}

        public static int DrawTabsView(Rect position,int selectedTabIndex,float labelTabHeight, GUIContent[] labels, float[] ratios)
        {
			if(Event.current.type == EventType.Repaint)
			{
                GUIStyle frameBox = "FrameBox";
                frameBox.Draw(position, false, false, false, false);
			}

			// draw tab label
			Rect tabRect = new Rect(position);
			tabRect.height = labelTabHeight;
            SplitRectHorizontal(tabRect, 0f, out Rect[] tabRects, ratios);
            for (int i = 0; i < tabRects.Length; i++)
            {
                bool oldState = selectedTabIndex == i;
                bool newState = GUI.Toggle(tabRects[i], oldState, labels[i], GetTabStyle(i, tabRects.Length));
                if (newState != oldState && newState)
                {
                    selectedTabIndex = i;
                }
            }
			return selectedTabIndex;

            GUIStyle GetTabStyle(int i, int length)
            {
				if(length == 1)
				{
					return "Tab onlyOne";
                }
                else if (i == 0)
                {
                    return "Tab first";
                }
                else if (i == length - 1)
                {
                    return "Tab last";
                }
                else
                {
                    return "Tab middle";
                }
            }
        }

		public static void DrawMultiFloatField(Rect position,GUIContent title, GUIContent[] labels,float[] values, float totalFieldWidth = 100f, float gap = 10f)
		{
			if (labels == null || values == null || labels.Length != values.Length)
			{
				Debug.LogError("[Editor] Draw Multi-Float field failed. labels and values cannot be null, and their length should be equal");
				return;
			}

			Rect suffixRect = EditorGUI.PrefixLabel(position, title);

			for(int i = 0; i < values.Length;i++)
			{
				Rect rect = new Rect(suffixRect);
				rect.x += i== 0? 0 : (totalFieldWidth + gap) * i;
				rect.width = totalFieldWidth;
				EditorGUIUtility.labelWidth = totalFieldWidth * 0.4f;
				values[i] = EditorGUI.FloatField(rect, labels[i], values[i]);
				EditorGUIUtility.labelWidth = 0f;
			}
		}
		
		public static void DrawEditorRulerHorizontal(Rect position,float unitWidth = 10f)
		{
			if (Event.current.type == EventType.Repaint)
			{
				int unitCount = Mathf.CeilToInt(position.width / unitWidth);
				for (int i = 0; i < unitCount; i++)
				{
					Rect rect = new Rect(position.x + i * unitWidth, position.y, unitWidth, EditorGUIUtility.singleLineHeight);
					EditorGUI.DrawRect(rect, GetColor(i));
					EditorGUI.LabelField(rect, new GUIContent(string.Empty, ((i + 1) * unitWidth).ToString()));
				}
			}
			
			Color GetColor(int index)
			{
				if(index % 2 == 0)
				{
					return new Color(0.4f, 0.4f, 0.4f,0.7f);
				}
				else
				{
					return new Color(0.7f, 0.7f, 0.7f, 0.8f);
				}
			}
		}
	}
}
