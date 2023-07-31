using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CassetteTape : MonoBehaviour
{
    public enum Side { A, B, }

	[SerializeField] RectTransform _sideLabel = null;
	[SerializeField] Text _sideText = null;

	[SerializeField] Vector3 _posA = Vector3.zero;
	[SerializeField] Vector3 _posB = Vector3.zero;

    public Side CurrentSide { get; private set; }

    public void Flip()
	{
		if (CurrentSide == Side.A)
		{
			CurrentSide = Side.B;
			_sideLabel.anchoredPosition = _posB;
		}
		else
		{
			CurrentSide = Side.A;
			_sideLabel.anchoredPosition = _posA;
		}
		_sideText.text = CurrentSide.ToString();
	}
}
