using UnityEngine;
using PointerEventData = UnityEngine.EventSystems.PointerEventData;

namespace Ami.BroAudio.UI
{
    public class PlayAudioOnPointerEvent : MonoBehaviour
    {
        [SerializeField] private UiPointerEventType _toRespondTo;
        [SerializeField] private GameObject _eventResponder;
        [SerializeField] private SoundSource _playsAudio;

        protected virtual void Awake()
        {
            _events = _eventResponder.GetOrAddComponent<UiPointerEvents>();
        }

        private UiPointerEvents _events;

        protected virtual void OnEnable()
        {
            ToggleSubs(true);
        }

        protected virtual void ToggleSubs(bool on)
        {
            if (on)
            {
                ListenForEvents();
            }
            else
            {
                UNlistenForEvents();
            }
        }

        protected virtual void ListenForEvents()
        {
            if ((_toRespondTo & UiPointerEventType.Up) == UiPointerEventType.Up)
            {
                _events.PointerUp += OnPointerEventTriggered;
            }

            if ((_toRespondTo & UiPointerEventType.Down) == UiPointerEventType.Down)
            {
                _events.PointerDown += OnPointerEventTriggered;
            }

            if ((_toRespondTo & UiPointerEventType.Click) == UiPointerEventType.Click)
            {
                _events.PointerClick += OnPointerEventTriggered;
            }

            //////

            if ((_toRespondTo & UiPointerEventType.Enter) == UiPointerEventType.Enter)
            {
                _events.PointerEnter += OnPointerEventTriggered;
            }

            if ((_toRespondTo & UiPointerEventType.Exit) == UiPointerEventType.Exit)
            {
                _events.PointerExit += OnPointerEventTriggered;
            }

            //////

            if ((_toRespondTo & UiPointerEventType.BeginDrag) == UiPointerEventType.BeginDrag)
            {
                _events.BeginDrag += OnPointerEventTriggered;
            }

            if ((_toRespondTo & UiPointerEventType.Drag) == UiPointerEventType.Drag)
            {
                _events.Drag += OnPointerEventTriggered;
            }

            if ((_toRespondTo & UiPointerEventType.EndDrag) == UiPointerEventType.EndDrag)
            {
                _events.EndDrag += OnPointerEventTriggered;
            }

            if ((_toRespondTo & UiPointerEventType.Drop) == UiPointerEventType.Drop)
            {
                _events.Drop += OnPointerEventTriggered;
            }
        }

        protected virtual void OnPointerEventTriggered(PointerEventData eventData)
        {
            _playsAudio.Play();
        }

        protected virtual void OnDisable()
        {
            ToggleSubs(false);
        }

        protected virtual void UNlistenForEvents()
        {
            _events.PointerUp -= OnPointerEventTriggered;
            _events.PointerDown -= OnPointerEventTriggered;
            _events.PointerClick -= OnPointerEventTriggered;

            _events.PointerEnter -= OnPointerEventTriggered;
            _events.PointerExit -= OnPointerEventTriggered;

            _events.BeginDrag -= OnPointerEventTriggered;
            _events.Drag -= OnPointerEventTriggered;
            _events.EndDrag -= OnPointerEventTriggered;
            _events.Drop -= OnPointerEventTriggered;
        }

        protected virtual void OnValidate()
        {
            if (_eventResponder == null)
            {
                _eventResponder = this.gameObject;
            }

            if (_playsAudio == null)
            {
                _playsAudio = this.GetComponent<SoundSource>();
            }
        }
    }
}