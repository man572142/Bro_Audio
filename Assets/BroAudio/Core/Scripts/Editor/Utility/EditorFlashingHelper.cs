using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.Extension
{
	public class EditorFlashingHelper : EditorUpdateHelper
	{
		private Ease _ease = default;
		private float _flashInterval = 0f;
		private bool _isReverse = false;
		private float _currentTime = 0f;

		protected override float UpdateInterval => 0.05f; // 20 FPS

		public Color OriginalColor { get; private set; }
		public Color DisplayColor { get; private set; }
		public bool IsUpdating { get; private set; }

		public EditorFlashingHelper(Color color, float flashInterval,Ease ease = Ease.Linear)
		{
			OriginalColor = color;
			_flashInterval = flashInterval;
			_ease = ease;
		}

		public override void Start()
		{
			if(IsUpdating)
			{
				return;
			}

			base.Start();
			_currentTime = 0f;

			IsUpdating = true;
		}

		public override void End()
		{
			base.End();
			_currentTime = 0f;

			IsUpdating = false;
		}

		protected override void Update()
		{
			if(_currentTime <= _flashInterval && _currentTime >= 0f)
			{
				DisplayColor = Color.Lerp(GetTransparent(OriginalColor), OriginalColor, (_currentTime / _flashInterval).SetEase(_ease));

				if(_isReverse)
				{
					_currentTime -= UpdateInterval;
				}
				else
				{
					_currentTime += UpdateInterval;
				}
			}
			else
			{
				_currentTime = Mathf.Clamp(_currentTime,0f,_flashInterval);
				_isReverse = !_isReverse;
			}
			base.Update();
		}

		private Color GetTransparent(Color color)
		{
			return new Color(color.r, color.g, color.b, 0f);
		}
	}
}