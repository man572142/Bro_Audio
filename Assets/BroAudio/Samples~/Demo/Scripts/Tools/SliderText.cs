using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ami.BroAudio.Demo
{
	[RequireComponent(typeof(Slider))]
	public class SliderText : MonoBehaviour
	{
		[SerializeField] Slider _slider = null;
		[SerializeField] Text _valueText = null;
		[SerializeField] bool _isPercentage = false;

		private void Start()
		{
			_slider.onValueChanged.AddListener(SetText);
			SetText(_slider.value);
		}

		private void OnDestroy()
		{
			_slider.onValueChanged.RemoveListener(SetText);
		}

		private void SetText(float value)
		{
			if(_valueText)
			{
				_valueText.text = _isPercentage ? $"{Mathf.RoundToInt(value * 100)}%" : value.ToString();
			}
		}
	}
}