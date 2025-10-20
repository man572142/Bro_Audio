using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
	public class MaterialOffsetMover : MonoBehaviour
	{
		[SerializeField] Renderer _renderer = null;
		[SerializeField] Vector2 _maxOffsetPerSeconds = Vector2.zero;
		[SerializeField] float _perlinNoiseSpeed = 1f;
		[SerializeField] float _minPerlinNoiseFactor = 0f;

		private void Start()
		{
			StartCoroutine(KeepMovingTillingOffset());
		}

		private IEnumerator KeepMovingTillingOffset()
		{
			float x = 0f;
			while (true)
			{
				yield return null;
				x += _perlinNoiseSpeed * Time.deltaTime;
				float noise = Mathf.Clamp(Mathf.PerlinNoise(x, x), _minPerlinNoiseFactor, 1f);
				_renderer.material.mainTextureOffset += _maxOffsetPerSeconds * noise * Time.deltaTime;
			}
		}
	} 
}
