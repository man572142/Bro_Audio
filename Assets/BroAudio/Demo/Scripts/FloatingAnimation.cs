using Ami.Extension;
using UnityEngine;

public class FloatingAnimation : MonoBehaviour
{
    [SerializeField] private Vector3 _maxMovement;
    [SerializeField] private float _speed;
    [SerializeField] private float _delayTime;
    [SerializeField] private Ease _ease;

    private float _deltaTime;
    private bool _hasStarted = false;
    private Vector3 _startPosition;
    private bool _isReturning = false;

    void Start()
    {
        _startPosition = transform.localPosition;
    }

    void Update()
    {
        if (!_hasStarted)
        {
            _deltaTime += Time.deltaTime;
            if (_deltaTime < _delayTime)
            {
                return;
            }
            else
            {
                _hasStarted = true;
                _deltaTime = 0f;
            }
        }

        _deltaTime += Time.deltaTime * _speed;
        float t = Mathf.Clamp01(_deltaTime);

        Vector3 target = !_isReturning
            ? Vector3.Lerp(_startPosition, _startPosition + _maxMovement, t.SetEase(_ease))
            : Vector3.Lerp(_startPosition + _maxMovement, _startPosition, t.SetEase(_ease));

        transform.localPosition = target;

        if (t >= 1f)
        {
            _isReturning = !_isReturning;
            _deltaTime = 0f;
        }
    }
}
