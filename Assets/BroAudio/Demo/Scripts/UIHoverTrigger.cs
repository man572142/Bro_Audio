using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ami.BroAudio.Demo
{
    public class UIHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] SoundSource _soundSource = default;
        [SerializeField] Image _handleIcon = null;
        [SerializeField] Color _hoverColor = default;

        private Color _originalColor = default;

        private void Start()
        {
            _originalColor = _handleIcon.color;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _soundSource.Play();
            _handleIcon.color = _hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _handleIcon.color = _originalColor;
        }
    } 
}
