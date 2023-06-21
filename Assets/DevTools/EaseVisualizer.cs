using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EaseVisualizer : MonoBehaviour
{
	[SerializeField] RectTransform _background = null;
    [SerializeField] RectTransform _dotImage = null;
    bool isStart = false;

	const float _duration = 5f;
	public void SetY(float value)
	{
		float norY = value / (AudioConstant.MaxFrequence - AudioConstant.MinFrequence);

		float step = _background.rect.width / (_duration / Time.deltaTime);
		_dotImage.anchoredPosition += Vector2.right * step;
		_dotImage.anchoredPosition = new Vector2(_dotImage.anchoredPosition.x, _background.rect.width * (1 - norY));
	}

}
