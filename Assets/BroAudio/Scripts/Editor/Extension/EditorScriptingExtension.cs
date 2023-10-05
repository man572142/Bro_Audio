using UnityEditor;
using UnityEngine;
using System.Linq;
using Ami.BroAudio.Editor;

namespace Ami.Extension
{
	public static class EditorScriptingExtension
	{
		/// <summary>
		/// 取得目前繪製的那一行的Rect，取完自動迭代至下行 (執行順序將會決定繪製的位置)
		/// </summary>
		/// <param name="drawer"></param>
		/// <param name="position"></param>
		/// <returns></returns>
		public static Rect GetRectAndIterateLine(IEditorDrawLineCounter drawer, Rect position)
		{
			Rect newRect = new Rect(position.x, position.y + drawer.SingleLineSpace * drawer.DrawLineCount, position.width, EditorGUIUtility.singleLineHeight);
			drawer.DrawLineCount++;

			return newRect;
		}

		/// <summary>
		/// 將Rect依指定比例水平拆分為兩個Rect
		/// </summary>
		/// <param name="origin">原始Rect</param>
		/// <param name="firstRatio">第一個Rect的比例</param>
		/// <param name="gap">兩者的間隔</param>
		/// <param name="rect1">輸出的第一個Rect</param>
		/// <param name="rect2">輸出的第二個Recr</param>
		public static void SplitRectHorizontal(Rect origin, float firstRatio, float gap, out Rect rect1, out Rect rect2)
		{
			float halfGap = gap * 0.5f;
			rect1 = new Rect(origin.x, origin.y, origin.width * firstRatio - halfGap, origin.height);
			rect2 = new Rect(rect1.xMax + gap, origin.y, origin.width * (1 - firstRatio) - halfGap, origin.height);
		}

		/// <summary>
		/// 將Rect依指定比例水平拆分為相應數量的Rect
		/// </summary>
		/// <param name="origin"></param>
		/// <param name="allRatio"></param>
		/// <param name="gap"></param>
		/// <param name="outputRects"></param>
		/// <returns></returns>
		public static bool TrySplitRectHorizontal(Rect origin, float[] allRatio, float gap, out Rect[] outputRects)
		{
			if (allRatio.Sum() != 1)
			{
				Debug.LogError("[Editor] Split ratio's sum should be 1");
				outputRects = null;
				return false;
			}

			Rect[] results = new Rect[allRatio.Length];

			for(int i = 0; i < results.Length;i++)
			{
				float offsetWidth = i == 0 || i == results.Length - 1 ? gap : gap * 0.5f;
				float newWidth = origin.width * allRatio[i] - offsetWidth;
				float newX = i > 0 ? results[i - 1].xMax + gap : origin.x;
				results[i] = new Rect(newX, origin.y, newWidth , origin.height);
			}
			outputRects = results;
			return true;
		}

		/// <summary>
		/// 將Rect依指定比例垂直拆分為兩個Rect
		/// </summary>
		/// <param name="origin"></param>
		/// <param name="firstRatio"></param>
		/// <param name="gap"></param>
		/// <param name="rect1"></param>
		/// <param name="rect2"></param>
		public static void SplitRectVertical(Rect origin, float firstRatio, float gap, out Rect rect1, out Rect rect2)
		{
			float halfGap = gap * 0.5f;
			rect1 = new Rect(origin.x, origin.y, origin.width, origin.height * firstRatio - halfGap);
			rect2 = new Rect(origin.x, rect1.yMax + gap, origin.width, origin.height * (1 - firstRatio) - halfGap);
		}

		/// <summary>
		/// 將Rect依指定比例水平拆分為相應數量的Rect
		/// </summary>
		/// <param name="origin"></param>
		/// <param name="allRatio"></param>
		/// <param name="gap"></param>
		/// <param name="outputRects"></param>
		/// <returns></returns>
		public static bool TrySplitRectVertical(Rect origin, float[] allRatio, float gap, out Rect[] outputRects)
		{
			if (allRatio.Sum() != 1)
			{
				Debug.LogError("[Editor] Split ratio's sum should be 1");
				outputRects = null;
				return false;
			}

			Rect[] results = new Rect[allRatio.Length];

			for (int i = 0; i < results.Length; i++)
			{
				float offsetHeight = i == 0 || i == results.Length - 1 ? gap : gap * 0.5f;
				float newHeight = origin.height * allRatio[i] - offsetHeight;
				float newY = i > 0 ? results[i - 1].yMax + gap : origin.y;
				results[i] = new Rect(origin.x,newY, origin.width,newHeight);
			}
			outputRects = results;
			return true;
		}

		/// <summary>
		/// 在原始Rect當中的指定比例位置顯示新的Rect(必須比原始Rect晚繪製)
		/// </summary>
		/// <param name="origin"></param>
		/// <param name="dissolveRatio"></param>
		/// <returns></returns>
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

		/// <summary>
		/// 支援RichText的HelpBox
		/// </summary>
		/// <param name="position">繪製位置</param>
		/// <param name="message">訊息內容</param>
		/// <param name="messageType">訊息類型</param>
		public static void RichTextHelpBox(Rect position,string message, MessageType messageType)
		{
			RichTextHelpBox(position,message, GetIconName(messageType));
		}

		/// <summary>
		/// 支援RichText的HelpBox
		/// </summary>
		/// <param name="message">訊息內容</param>
		/// <param name="messageType">訊息類型</param>
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

		/// <summary>
		/// 支援RichText及自訂icon的HelpBox
		/// </summary>
		/// <param name="position">繪製位置</param>
		/// <param name="message">訊息內容</param>
		/// <param name="icon">Unity內建Icon名稱</param>
		public static void RichTextHelpBox(Rect position,string message, string icon)
		{
			GUIContent content = GetRichTextContent(message, icon);
			EditorGUI.LabelField(position, content, GUIStyleHelper.RichTextHelpBox);
		}

		/// <summary>
		/// 支援RichText及自訂icon的HelpBox
		/// </summary>
		/// <param name="message">訊息內容</param>
		/// <param name="icon">Unity內建Icon名稱</param>
		public static void RichTextHelpBox(string message, string icon)
		{
			GUIContent content = GetRichTextContent(message, icon);
			EditorGUILayout.LabelField(content, GUIStyleHelper.RichTextHelpBox);
		}

		private static GUIContent GetRichTextContent(string message, string icon)
		{
			return string.IsNullOrEmpty(icon) ? new GUIContent(message) : new GUIContent(message, EditorGUIUtility.IconContent(icon).image);
		}

		/// <summary>
		/// 取得Property自動生成BackingField的名稱
		/// </summary>
		/// <param name="propertyName"></param>
		/// <returns></returns>
		public static string GetBackingFieldName(string propertyName)
		{
			return $"<{propertyName}>k__BackingField";
		}

		/// <summary>
		/// 取得Porperty的Field名稱(命名規則:_camelCase)
		/// </summary>
		/// <param name="propertyName"></param>
		/// <returns></returns>
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

		public static bool GUIClipContains(this Rect scope,Rect guiClip ,Vector2 position)
		{
			float offsetX = scope.xMin - guiClip.xMin;
			float offsetY = scope.yMin - guiClip.yMin;

			Rect rect = new Rect(offsetX, offsetY, scope.width, scope.height);
			return rect.Contains(position);
		}

	}
}
