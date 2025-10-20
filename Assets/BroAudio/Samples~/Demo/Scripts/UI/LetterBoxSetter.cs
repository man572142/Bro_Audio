using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ami.BroAudio.Demo
{
	public class LetterBoxSetter : MonoBehaviour
	{
		//public event Action OnRollFinished;
		[SerializeField] RectTransform _canvasRectTransform = null;
		[SerializeField] RectTransform _letterBoxTop = null;
		[SerializeField] RectTransform _letterBoxBottom = null;
		[SerializeField] float _targetRatio = default;
		[SerializeField] float _minimumBoxHeight = 70f;
		private float _boxHeight = 0f;

		private void Start()
		{
			float screenWidth = _canvasRectTransform.rect.width;
			float screenHeight = _canvasRectTransform.rect.height;

			_boxHeight = (screenHeight - screenWidth / _targetRatio) * 0.5f;
			_boxHeight = Mathf.Max(_minimumBoxHeight, _boxHeight);

			_letterBoxTop.sizeDelta = new Vector2(screenWidth, _boxHeight);
			_letterBoxTop.anchoredPosition = new Vector2(0f, _boxHeight);

			_letterBoxBottom.sizeDelta = new Vector2(screenWidth, _boxHeight);
			_letterBoxBottom.anchoredPosition = new Vector2(0f, -_boxHeight);

		}

		//public void Roll(float duration, bool isRollIn)
		//{
		//	float screenWidth = Screen.width;
		//	float screenHeight = Screen.height;
		//	if(_targetRatio <= screenWidth / screenHeight )
		//	{
		//		Debug.LogError("Letterbox's target ratio should be greater than the current screen aspect ratio");
		//		return;
		//	}

		//	_barHeight = (screenHeight - screenWidth / _targetRatio) * 0.5f;

		//	_letterBoxTop.sizeDelta = new Vector2(screenWidth, _barHeight);
		//	_letterBoxTop.anchoredPosition = new Vector2(0f, _barHeight);

		//	_letterBoxBottom.sizeDelta = new Vector2(screenWidth, _barHeight);
		//	_letterBoxBottom.anchoredPosition = new Vector2(0f, -_barHeight);

		//	StartCoroutine(Rolling(duration, isRollIn));
		//}

		//private IEnumerator Rolling(float duration, bool isIn)
		//{
		//	float time = isIn ? 0f : duration;
		//	Func<bool> condition = () => isIn ? time <= duration : time >= 0f;
		//	while (condition.Invoke())
		//	{
		//		_letterBoxTop.anchoredPosition = new Vector2(0f, Mathf.Lerp(_barHeight, 0f, time / duration));
		//		_letterBoxBottom.anchoredPosition = new Vector2(0f, Mathf.Lerp(-_barHeight, 0f, time / duration));

		//		yield return null;
		//		time += isIn? Time.deltaTime : -Time.deltaTime;
		//	}
		//	OnRollFinished?.Invoke();
		//}
	}
}
