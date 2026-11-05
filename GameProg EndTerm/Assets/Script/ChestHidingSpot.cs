using UnityEngine;

public class ChestHidingSpot : MonoBehaviour
{
    [Header("Positions (Create Empty Child Objects)")]
    public Transform hidePosition;  // Safe spot deep inside chest (rotated facing South)
    public Transform peekPosition;  // Slightly elevated spot (rotated facing South)

    [Header("Controls & Settings")]
    public KeyCode interactKey = KeyCode.E;
    public KeyCode peekKey = KeyCode.Space;
    public float peekSpeed = 8f;

    private bool isPlayerInRange = false;
    private bool isHiding = false;

    // Dynamically grabbed references
    private GameObject playerRoot;
    private GameObject robotBody;
    private Transform playerCamera;
    private Transform originalCamParent;
    private Rigidbody playerRb;

    // Scripts to disable while hiding
    private MonoBehaviour movementScript;
    private PlayerNoiseEmitter noiseScript;
    private MonoBehaviour cameraScript;

    // Memory to return camera to exact head position
    private Vector3 originalCamLocalPos;
    private Quaternion originalCamLocalRot;

    private void Update()
    {
        // 1. Enter / Exit Logic
        if (Input.GetKeyDown(interactKey))
        {
            if (isHiding)
            {
                ExitChest();
            }
            else if (isPlayerInRange)
            {
                EnterChest();
            }
        }

        // 2. Peeking & Rotation Logic
        if (isHiding && playerCamera != null && hidePosition != null && peekPosition != null)
        {
            Transform targetTransform = Input.GetKey(peekKey) ? peekPosition : hidePosition;

            playerCamera.position = Vector3.Lerp(
                playerCamera.position,
                targetTransform.position,
                Time.deltaTime * peekSpeed
            );

            playerCamera.rotation = Quaternion.Slerp(
                playerCamera.rotation,
                targetTransform.rotation,
                Time.deltaTime * peekSpeed
            );
        }
    }

    private void EnterChest()
    {
        if (hidePosition == null || peekPosition == null)
        {
            Debug.LogError("ChestHidingSpot: Please assign HidePosition and PeekPosition in the Inspector!");
            return;
        }

        isHiding = true;

        // 1. Turn off visual body mesh
        if (robotBody != null) robotBody.SetActive(false);

        // 2. Temporarily detach camera and move to chest
        if (playerCamera != null)
        {
            originalCamParent = playerCamera.parent;
            originalCamLocalPos = playerCamera.localPosition;
            originalCamLocalRot = playerCamera.localRotation;

            playerCamera.SetParent(transform);
            playerCamera.position = hidePosition.position;
            playerCamera.rotation = hidePosition.rotation;
        }

        // 3. Disable control scripts
        if (movementScript != null) movementScript.enabled = false;
        if (noiseScript != null) noiseScript.enabled = false;
        if (cameraScript != null) cameraScript.enabled = false;

        // 4. Freeze Rigidbody to prevent collider overlap from pushing player upward
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.isKinematic = true;
        }
    }

    private void ExitChest()
    {
        isHiding = false;

        // 1. Turn visual body mesh back on
        if (robotBody != null) robotBody.SetActive(true);

        // 2. Re-attach camera back to robot head
        if (playerCamera != null)
        {
            playerCamera.SetParent(originalCamParent);
            playerCamera.localPosition = originalCamLocalPos;
            playerCamera.localRotation = originalCamLocalRot;
        }

        // 3. Unfreeze Rigidbody physics
        if (playerRb != null)
        {
            playerRb.isKinematic = false;
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        // 4. Re-enable control scripts
        if (movementScript != null) movementScript.enabled = true;
        if (noiseScript != null) noiseScript.enabled = true;
        if (cameraScript != null) cameraScript.enabled = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Transform rootTransform = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform.root;

        if (rootTransform.CompareTag("Player") || other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerRoot = rootTransform.gameObject;
            playerRb = playerRoot.GetComponent<Rigidbody>();

            movementScript = playerRoot.GetComponent<WheeledRobotMovement>();
            noiseScript = playerRoot.GetComponent<PlayerNoiseEmitter>();

            Transform bodyTransform = playerRoot.transform.Find("kalibot body");
            if (bodyTransform == null)
            {
                foreach (Transform child in playerRoot.transform)
                {
                    if (child.name.ToLower().Contains("kalibot") || child.name.ToLower().Contains("body"))
                    {
                        bodyTransform = child;
                        break;
                    }
                }
            }
            if (bodyTransform != null) robotBody = bodyTransform.gameObject;

            Camera cam = playerRoot.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                playerCamera = cam.transform;
                cameraScript = cam.GetComponent<RobotHeadCamera>();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Transform rootTransform = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform.root;

        if (rootTransform.gameObject == playerRoot || other.CompareTag("Player"))
        {
            isPlayerInRange = false;
        }
    }
}