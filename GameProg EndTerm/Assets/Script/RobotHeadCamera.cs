using UnityEngine;

public class RobotHeadCamera : MonoBehaviour
{
    public Transform robotChassis;
    public float mouseSensitivity = 2f;

    [Header("Head Angle Limits")]
    [Tooltip("Head yaw range relative to body (90 = 180 total look arc)")]
    public float maxHeadYaw = 90f;
    public float pitchLimit = 75f;

    private float headPitch = 0f;
    private float headYaw = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (robotChassis == null && transform.parent != null)
        {
            robotChassis = transform.parent;
        }
    }

    void Update()
    {
        if (robotChassis == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        headYaw = Mathf.Clamp(headYaw + mouseX, -maxHeadYaw, maxHeadYaw);
        headPitch = Mathf.Clamp(headPitch - mouseY, -pitchLimit, pitchLimit);

        Quaternion bodyRotation = Quaternion.Euler(0f, robotChassis.eulerAngles.y, 0f);
        Quaternion headLocalRotation = Quaternion.Euler(headPitch, headYaw, 0f);

        transform.rotation = bodyRotation * headLocalRotation;
    }
}