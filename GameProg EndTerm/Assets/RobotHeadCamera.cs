using UnityEngine;

public class RobotHeadCamera : MonoBehaviour
{
    public Transform robotChassis;       // Reference to the Robot Body
    public float cameraHeight = 1.4f;    // Eye-level height
    public float mouseSensitivity = 2f;

    [Header("Head Angle Limits")]
    [Tooltip("Head yaw range relative to body (90 = 180 total look arc)")]
    public float maxHeadYaw = 90f;       // 180 degrees total (-90° left to +90° right)
    public float pitchLimit = 75f;       // Max look up / down angle

    private float headPitch = 0f;        // Up / Down angle
    private float headYaw = 0f;          // Left / Right relative to body angle

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

        // --- Read Mouse Input ---
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // --- Clamp Head Rotation relative to Chassis ---
        headYaw = Mathf.Clamp(headYaw + mouseX, -maxHeadYaw, maxHeadYaw);
        headPitch = Mathf.Clamp(headPitch - mouseY, -pitchLimit, pitchLimit);

        // --- Position Camera at head level ---
        transform.position = robotChassis.position + Vector3.up * cameraHeight;

        // --- Combine body orientation with head local angles ---
        Quaternion bodyRotation = Quaternion.Euler(0f, robotChassis.eulerAngles.y, 0f);
        Quaternion headLocalRotation = Quaternion.Euler(headPitch, headYaw, 0f);

        transform.rotation = bodyRotation * headLocalRotation;
    }
}