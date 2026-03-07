using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Ami.BroAudio.Demo
{
    public class EventSystemAutoAdapter : MonoBehaviour
    {
        private void Awake()
        {
#if ENABLE_INPUT_SYSTEM
            if (GetComponent<InputSystemUIInputModule>() != null)
                return;

            var standalone = GetComponent<StandaloneInputModule>();
            if (standalone != null)
                Object.Destroy(standalone);

            gameObject.AddComponent<InputSystemUIInputModule>();
#else
            if (GetComponent<StandaloneInputModule>() != null)
                return;

            var inputSystemModule = GetComponent("InputSystemUIInputModule");
            if (inputSystemModule != null)
                Object.Destroy(inputSystemModule);

            gameObject.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
