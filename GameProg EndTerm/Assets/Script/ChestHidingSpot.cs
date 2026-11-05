using UnityEngine;
using UnityEngine.UI;

public class ChestHidingSpot : MonoBehaviour
{
    [Header("Positions")]
    public Transform hidePosition;
    public Transform peekPosition;

    [Header("Controls & Settings")]
    public KeyCode interactKey = KeyCode.E;
    public KeyCode peekKey = KeyCode.Space;
    public float peekSpeed = 8f;

    [Header("Overheat Mechanics")]
    public GameObject overheatUI;
    public Slider overheatSlider;
    public Image overheatFillImage;
    public Gradient overheatGradient;
    public float delayBeforeOverheat = 5f;
    public float heatPerSecond = 15f;
    public float cooldownPerSecond = 10f;

    private float currentOverheat = 0f;
    private float timeHidden = 0f;

    private bool isPlayerInRange = false;
    private bool isHiding = false;

    // References
    private GameObject playerRoot;
    private GameObject robotBody;
    private Transform playerCamera;
    private Transform originalCamParent;
    private Rigidbody playerRb;

    // --- THE FIX: Culling Mask Variables ---
    private Light baseVisionLight;
    private int baseLightMask;

    private Light cameraFlashlight;
    private int camLightMask;
    // ---------------------------------------

    private MonoBehaviour movementScript;
    private PlayerNoiseEmitter noiseScript;
    private MonoBehaviour cameraScript;

    private Vector3 originalCamLocalPos;
    private Quaternion originalCamLocalRot;

    private void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            if (isHiding) ExitChest();
            else if (isPlayerInRange) EnterChest();
        }

        // --- OVERHEAT & COOLDOWN LOGIC ---
        if (isHiding)
        {
            timeHidden += Time.deltaTime;

            if (timeHidden >= delayBeforeOverheat)
            {
                currentOverheat += heatPerSecond * Time.deltaTime;

                if (currentOverheat >= 100f)
                {
                    currentOverheat = 100f;
                    Debug.Log("Overheated! Kicked out of chest.");
                    ExitChest();
                }
            }
        }
        else
        {
            if (currentOverheat > 0f)
            {
                currentOverheat -= cooldownPerSecond * Time.deltaTime;
                if (currentOverheat < 0f) currentOverheat = 0f;
            }
        }

        // --- UI VISIBILITY MANAGEMENT ---
        if (overheatUI != null)
        {
            bool shouldShowUI = currentOverheat > 0f || (isHiding && timeHidden >= delayBeforeOverheat);

            if (overheatUI.activeSelf != shouldShowUI)
            {
                overheatUI.SetActive(shouldShowUI);
            }

            if (shouldShowUI && overheatSlider != null)
            {
                overheatSlider.value = currentOverheat;

                if (overheatFillImage != null)
                {
                    overheatFillImage.color = overheatGradient.Evaluate(currentOverheat / 100f);
                }
            }
        }

        // --- PEEKING & CAMERA MOVEMENT ---
        if (isHiding && playerCamera != null && hidePosition != null && peekPosition != null)
        {
            bool isPeeking = Input.GetKey(peekKey);
            Transform targetTransform = isPeeking ? peekPosition : hidePosition;

            // THE FIX: Change what the light hits (Culling Mask) instead of turning the power off.
            // This never touches '.enabled', meaning your custom F key script will never break!
            if (baseVisionLight != null)
                baseVisionLight.cullingMask = isPeeking ? baseLightMask : 0;

            if (cameraFlashlight != null)
                cameraFlashlight.cullingMask = isPeeking ? camLightMask : 0;

            playerCamera.position = Vector3.Lerp(playerCamera.position, targetTransform.position, Time.deltaTime * peekSpeed);
        }
    }

    private void EnterChest()
    {
        if (hidePosition == null || peekPosition == null) return;
        isHiding = true;

        if (currentOverheat > 0f)
        {
            timeHidden = delayBeforeOverheat;
        }
        else
        {
            timeHidden = 0f;
        }

        if (robotBody != null) robotBody.SetActive(false);

        if (playerCamera != null)
        {
            originalCamParent = playerCamera.parent;
            originalCamLocalPos = playerCamera.localPosition;
            originalCamLocalRot = playerCamera.localRotation;

            playerCamera.SetParent(transform);
            playerCamera.position = hidePosition.position;
        }

        // THE FIX: Save the original masks and set them to 0 (Nothing)
        if (baseVisionLight != null)
        {
            baseLightMask = baseVisionLight.cullingMask;
            baseVisionLight.cullingMask = 0;
        }

        if (cameraFlashlight != null)
        {
            camLightMask = cameraFlashlight.cullingMask;
            cameraFlashlight.cullingMask = 0;
        }

        if (movementScript != null) movementScript.enabled = false;
        if (noiseScript != null) noiseScript.enabled = false;
        if (cameraScript != null) cameraScript.enabled = true;

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

        if (robotBody != null) robotBody.SetActive(true);

        if (playerCamera != null)
        {
            playerCamera.SetParent(originalCamParent);
            playerCamera.localPosition = originalCamLocalPos;
            playerCamera.localRotation = originalCamLocalRot;
        }

        // THE FIX: Restore the masks to normal when you leave
        if (baseVisionLight != null) baseVisionLight.cullingMask = baseLightMask;
        if (cameraFlashlight != null) cameraFlashlight.cullingMask = camLightMask;

        if (playerRb != null) playerRb.isKinematic = false;
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

            Transform[] allChildren = playerRoot.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in allChildren)
            {
                if (child.name.ToLower().Contains("kalibot") || child.name.ToLower().Contains("body"))
                {
                    robotBody = child.gameObject;
                    break;
                }
            }

            Camera cam = playerRoot.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                playerCamera = cam.transform;
                cameraScript = cam.GetComponent<RobotHeadCamera>();

                cameraFlashlight = playerCamera.GetComponentInChildren<Light>(true);
            }

            Transform baseLightTrans = playerRoot.transform.Find("BaseVisionLight");
            if (baseLightTrans != null)
            {
                baseVisionLight = baseLightTrans.GetComponent<Light>();
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