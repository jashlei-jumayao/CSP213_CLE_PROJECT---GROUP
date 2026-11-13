using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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

    // Culling Mask Variables
    private Light baseVisionLight;
    private int baseLightMask;

    private Light cameraFlashlight;
    private int camLightMask;

    // Variables to remember where the light belongs
    private Transform originalBaseLightParent;
    private Vector3 originalBaseLightLocalPos;
    private Quaternion originalBaseLightLocalRot;

    // References to re-enable components properly
    private MonoBehaviour movementScript;
    private PlayerNoiseEmitter noiseScript;
    private MonoBehaviour cameraScript;

    private Vector3 originalCamLocalPos;
    private Quaternion originalCamLocalRot;

    // Tracks the exact colliders that need to be turned back on
    private List<Collider> collidersToRestore = new List<Collider>();

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

            if (baseVisionLight != null)
                baseVisionLight.cullingMask = isPeeking ? baseLightMask : 0;

            if (cameraFlashlight != null)
                cameraFlashlight.cullingMask = isPeeking ? camLightMask : 0;

            playerCamera.position = Vector3.Lerp(playerCamera.position, targetTransform.position, Time.deltaTime * peekSpeed);
        }
    }

    private void EnterChest()
    {
        // FIX: Removed obsolete FindObjectsSortMode parameter to clear compilation warnings[cite: 1]
        MonsterAI[] allMonsters = UnityEngine.Object.FindObjectsByType<MonsterAI>(FindObjectsInactive.Exclude);
        MonsterVisionAI[] allVisionMonsters = UnityEngine.Object.FindObjectsByType<MonsterVisionAI>(FindObjectsInactive.Exclude);

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

        if (playerCamera != null)
        {
            originalCamParent = playerCamera.parent;
            originalCamLocalPos = playerCamera.localPosition;
            originalCamLocalRot = playerCamera.localRotation;

            playerCamera.SetParent(transform);
            playerCamera.position = hidePosition.position;
        }

        if (baseVisionLight != null)
        {
            originalBaseLightParent = baseVisionLight.transform.parent;
            originalBaseLightLocalPos = baseVisionLight.transform.localPosition;
            originalBaseLightLocalRot = baseVisionLight.transform.localRotation;

            if (playerCamera != null)
            {
                baseVisionLight.transform.SetParent(playerCamera);
                baseVisionLight.transform.localPosition = Vector3.zero;
            }

            baseLightMask = baseVisionLight.cullingMask;
            baseVisionLight.cullingMask = 0;
        }

        if (cameraFlashlight != null)
        {
            camLightMask = cameraFlashlight.cullingMask;
            cameraFlashlight.cullingMask = 0;
        }

        collidersToRestore.Clear();
        if (playerRoot != null)
        {
            Collider[] playerColliders = playerRoot.GetComponentsInChildren<Collider>();
            foreach (Collider col in playerColliders)
            {
                if (col.enabled)
                {
                    collidersToRestore.Add(col);
                    col.enabled = false;
                }
            }
        }

        if (robotBody != null) robotBody.SetActive(false);

        if (movementScript != null) movementScript.enabled = false;
        if (noiseScript != null) noiseScript.enabled = false;
        if (cameraScript != null) cameraScript.enabled = false; // Disabled so mouse look doesn't break peeking lerp

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.isKinematic = true;
        }

        foreach (MonsterAI monster in allMonsters)
        {
            monster.InvestigateHiddenPlayer(transform.position);
        }

        foreach (MonsterVisionAI monster in allVisionMonsters)
        {
            monster.InvestigateHiddenPlayer(transform.position);
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

        if (baseVisionLight != null && originalBaseLightParent != null)
        {
            baseVisionLight.transform.SetParent(originalBaseLightParent);
            baseVisionLight.transform.localPosition = originalBaseLightLocalPos;
            baseVisionLight.transform.localRotation = originalBaseLightLocalRot;

            baseVisionLight.cullingMask = baseLightMask;
        }

        if (cameraFlashlight != null) cameraFlashlight.cullingMask = camLightMask;

        foreach (Collider col in collidersToRestore)
        {
            if (col != null) col.enabled = true;
        }
        collidersToRestore.Clear();

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

            Light[] allLights = playerRoot.GetComponentsInChildren<Light>(true);
            foreach (Light l in allLights)
            {
                if (l.gameObject.name.Contains("BaseVisionLight"))
                {
                    baseVisionLight = l;
                    break;
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isHiding) return; // Prevents trigger exit from firing when player colliders turn off

        Transform rootTransform = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform.root;
        if (playerRoot != null && (rootTransform.gameObject == playerRoot || other.CompareTag("Player")))
        {
            isPlayerInRange = false;
        }
    }
}