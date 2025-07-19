using UnityEngine;
using UnityEngine.UI;

namespace Ami.BroAudio.Demo
{
    public class CannonUI : MonoBehaviour
    {
        [SerializeField] Cannon _controller = null;

        [SerializeField] Text _velocityApiTextComponent = null;
        [SerializeField] Text _chainedPlayModeTextComponent = null;
        [SerializeField] Slider _progressBar = null;
        [SerializeField] Image _fillImage = null;
        [SerializeField] Gradient _fillGradient = null;

        private string _velocityApiFormat = null;
        private string _chainedPlayModeFormat = null;

        private void Awake ()
        {
            _controller.OnForceChanged += UpdateForceUI;
            _controller.OnCoolDownFinished += OpenUI;
            _controller.OnChainedPlayModeStageChanged += UpdateChainedModeStage;

            _progressBar.maxValue = _controller.MaxForce;
            _progressBar.value = _progressBar.minValue;
            _velocityApiFormat = _velocityApiTextComponent.text;
            _chainedPlayModeFormat = _chainedPlayModeTextComponent.text;
            UpdateForceUI(0f);
            UpdateChainedModeStage(PlaybackStage.None);
        }

        private void UpdateChainedModeStage(PlaybackStage stage)
        {
            _chainedPlayModeTextComponent.text = string.Format(_chainedPlayModeFormat, stage.ToString());
        }

        private void OnDestroy()
        {
            _controller.OnForceChanged -= UpdateForceUI;
            _controller.OnCoolDownFinished -= OpenUI;
            _controller.OnChainedPlayModeStageChanged -= UpdateChainedModeStage;
        }

        private void UpdateForceUI(float force)
        {
            _velocityApiTextComponent.text = string.Format(_velocityApiFormat, (int)force);
            _progressBar.value = force;

            _fillImage.color = _fillGradient.Evaluate(force / _controller.MaxForce);
        }

        private void OpenUI()
        {
            gameObject.SetActive(true);
        }
    }
}