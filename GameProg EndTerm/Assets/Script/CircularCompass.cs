using UnityEngine;
using UnityEngine.UI;

public class CircularCompass : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Automatically assigned by WheeledRobotMovement, or drag your Robot root Transform here.")]
    public Transform target;
    [Tooltip("Assign the RawImage component displaying your circular compass texture.")]
    public RawImage compassRawImage;

    [Header("Alignment & Smoothing")]
    [Tooltip("180 flips South to North at the top of the dial.")]
    [Range(-180f, 180f)]
    public float northOffset = 180f;

    [Tooltip("Smooths dial rotation (0 = instant, 15 = smooth and responsive).")]
    public float smoothSpeed = 15f;

    private float currentZAngle;

    private void Start()
    {
        if (compassRawImage == null)
        {
            compassRawImage = GetComponent<RawImage>();
        }

        if (target != null)
        {
            currentZAngle = target.eulerAngles.y + northOffset;
        }
    }

    public void SetTarget(Transform robotTransform)
    {
        target = robotTransform;
        if (target != null)
        {
            currentZAngle = target.eulerAngles.y + northOffset;
        }
    }

    private void LateUpdate()
    {
        if (target == null || compassRawImage == null) return;

        // Turning right (+Y) rotates the dial counter-clockwise (+Z) so East moves to the top
        float desiredZAngle = target.eulerAngles.y + northOffset;

        currentZAngle = (smoothSpeed > 0f)
            ? Mathf.LerpAngle(currentZAngle, desiredZAngle, smoothSpeed * Time.deltaTime)
            : desiredZAngle;

        compassRawImage.rectTransform.localEulerAngles = new Vector3(0f, 0f, currentZAngle);
    }
}