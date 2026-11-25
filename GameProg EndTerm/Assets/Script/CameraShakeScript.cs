using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private float shakeTimer = 0f;
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0f;
    private Vector3 lastShakeOffset = Vector3.zero;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    public void Shake(float duration, float magnitude)
    {
        // Keep the stronger shake if one is already running
        if (shakeTimer > 0f && magnitude < shakeMagnitude) return;

        shakeDuration = duration;
        shakeTimer = duration;
        shakeMagnitude = magnitude;
    }

    private void LateUpdate()
    {
        if (lastShakeOffset != Vector3.zero)
        {
            transform.localPosition -= lastShakeOffset;
            lastShakeOffset = Vector3.zero;
        }

        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float damper = shakeDuration > 0f ? Mathf.Clamp01(shakeTimer / shakeDuration) : 0f;
            float currentStrength = shakeMagnitude * damper;

            lastShakeOffset = new Vector3(
                Random.Range(-1f, 1f) * currentStrength,
                Random.Range(-1f, 1f) * currentStrength,
                0f
            );

            transform.localPosition += lastShakeOffset;
        }
    }
}