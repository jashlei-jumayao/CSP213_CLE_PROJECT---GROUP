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
    public float interactRange = 3.5f;
    [Tooltip("Fine-tune the center point of the interaction sphere and Gizmo in local space.")]
    public Vector3 centerOffset = Vector3.zero;
    public float peekSpeed = 8f;

    [Header("Peek Camera Rotation")]
    [Tooltip("Base Euler angle offset added to Peek Position's rotation.")]
    public Vector3 peekRotationOffset = Vector3.zero;
    [Tooltip("Allow rotating the camera with the mouse while peeking.")]
    public bool allowMouseLookWhilePeeking = true;
    public float peekLookSensitivity = 2f;
    [Tooltip("Maximum left/right rotation angle (in degrees) from the peek center.")]
    public float maxPeekYaw = 60f;
    [Tooltip("Maximum up/down rotation angle (in degrees) from the peek center.")]
    public float maxPeekPitch = 30f;

    [Header("Audio")]
    [Tooltip("Sounds played at random when the player enters the chest.")]
    public AudioClip[] enterChestSounds;
    [Tooltip("Sounds played at random when the player exits the chest.")]
    public AudioClip[] exitChestSounds;
    [Range(0f, 1f)]
    public float chestInteractVolume = 1f;

    [Tooltip("Sounds played at random when the robot peeks out of the chest.")]
    public AudioClip[] peekOutSounds;
    [Tooltip("Sounds played at random when the robot ducks back inside the chest.")]
    public AudioClip[] peekInSounds;
    [Range(0f, 1f)]
    public float peekSoundVolume = 1f;

    [Tooltip("Sound played the moment overheating starts.")]
    public AudioClip overheatStartSound;
    [Range(0f, 1f)]
    public float overheatSoundVolume = 1f;
    [Tooltip("Loop the overheat sound while inside the chest.")]
    public bool loopOverheatSound = false;

    [Tooltip("Optional: Assign a dedicated AudioSource on the chest, or leave empty to auto-create one.")]
    public AudioSource chestAudioSource;

    [Header("Overheat Mechanics")]
    [Tooltip("Leave empty on Prefabs to auto-find an object named 'OverheatUI' or 'OverheatCanvas' in the scene.")]
    public GameObject overheatUI;
    public Slider overheatSlider;
    public Image overheatFillImage;
    public Gradient overheatGradient;
    [Tooltip("Show the Overheat UI immediately when entering the chest instead of waiting for Delay Before Overheat.")]
    public bool showUIImmediatelyOnEnter = true;
    public float delayBeforeOverheat = 5f;
    public float heatPerSecond = 15f;
    public float cooldownPerSecond = 10f;

    private float currentOverheat = 0f;
    private float timeHidden = 0f;

    private float currentPeekYaw = 0f;
    private float currentPeekPitch = 0f;

    private bool isPlayerInRange = false;
    private bool isHiding = false;
    private bool wasPeeking = false;
    private bool hasPlayedOverheatSound = false;
    private bool ownsOverheatUI = false;

    // Dedicated AudioSource for overheat
    private AudioSource overheatAudioSource;

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

    private void Start()
    {
        if (chestAudioSource == null)
        {
            chestAudioSource = GetComponent<AudioSource>();
            if (chestAudioSource == null)
            {
                chestAudioSource = gameObject.AddComponent<AudioSource>();
                chestAudioSource.playOnAwake = false;
                chestAudioSource.spatialBlend = 1f;
            }
        }

        overheatAudioSource = gameObject.AddComponent<AudioSource>();
        overheatAudioSource.playOnAwake = false;
        overheatAudioSource.spatialBlend = chestAudioSource.spatialBlend;

        FindAndCachePlayer();
        FindAndCacheOverheatUI();
    }

    private void Update()
    {
        // --- PROXIMITY CHECK (CENTERED ON CHEST) ---
        if (!isHiding)
        {
            if (playerRoot == null)
            {
                FindAndCachePlayer();
            }

            if (playerRoot != null)
            {
                float distance = Vector3.Distance(GetChestCenter(), playerRoot.transform.position);
                isPlayerInRange = distance <= interactRange;
            }
            else
            {
                isPlayerInRange = false;
            }
        }

        if (Input.GetKeyDown(interactKey))
        {
            // Voluntary exit when pressing interactKey (E)
            if (isHiding) ExitChest(true);
            else if (isPlayerInRange) EnterChest();
        }

        // --- OVERHEAT & COOLDOWN LOGIC ---
        if (isHiding)
        {
            timeHidden += Time.deltaTime;

            if (timeHidden >= delayBeforeOverheat)
            {
                if (!hasPlayedOverheatSound)
                {
                    hasPlayedOverheatSound = true;
                    PlayOverheatStartSound();
                }

                currentOverheat += heatPerSecond * Time.deltaTime;

                if (currentOverheat >= 100f)
                {
                    currentOverheat = 100f;
                    Debug.Log("Overheated! Kicked out of chest.");
                    // Forced exit: overheat sound keeps playing
                    ExitChest(false);
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

        // --- UI VISIBILITY MANAGEMENT (ONLY FOR THE CHEST IN USE) ---
        if (ownsOverheatUI && overheatUI != null)
        {
            bool shouldShowUI = showUIImmediatelyOnEnter
                ? (isHiding || currentOverheat > 0f)
                : (currentOverheat > 0f || (isHiding && timeHidden >= delayBeforeOverheat));

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

            if (!shouldShowUI && !isHiding)
            {
                ownsOverheatUI = false;
            }
        }

        // --- PEEKING & CAMERA MOVEMENT / ROTATION ---
        if (isHiding && playerCamera != null && hidePosition != null && peekPosition != null)
        {
            bool isPeeking = Input.GetKey(peekKey);

            // Trigger peek-out or peek-in sound only when the peeking state changes
            if (isPeeking != wasPeeking)
            {
                wasPeeking = isPeeking;
                PlayRandomChestSound(isPeeking ? peekOutSounds : peekInSounds, peekSoundVolume);
            }

            Transform targetTransform = isPeeking ? peekPosition : hidePosition;

            if (baseVisionLight != null)
                baseVisionLight.cullingMask = isPeeking ? baseLightMask : 0;

            if (cameraFlashlight != null)
                cameraFlashlight.cullingMask = isPeeking ? camLightMask : 0;

            playerCamera.position = Vector3.Lerp(playerCamera.position, targetTransform.position, Time.deltaTime * peekSpeed);

            Quaternion targetRotation;
            if (isPeeking)
            {
                if (allowMouseLookWhilePeeking)
                {
                    currentPeekYaw += Input.GetAxis("Mouse X") * peekLookSensitivity;
                    currentPeekPitch -= Input.GetAxis("Mouse Y") * peekLookSensitivity;

                    currentPeekYaw = Mathf.Clamp(currentPeekYaw, -maxPeekYaw, maxPeekYaw);
                    currentPeekPitch = Mathf.Clamp(currentPeekPitch, -maxPeekPitch, maxPeekPitch);
                }
                else
                {
                    currentPeekYaw = 0f;
                    currentPeekPitch = 0f;
                }

                Quaternion basePeekRot = peekPosition.rotation * Quaternion.Euler(peekRotationOffset);
                targetRotation = basePeekRot * Quaternion.Euler(currentPeekPitch, currentPeekYaw, 0f);
            }
            else
            {
                currentPeekYaw = Mathf.Lerp(currentPeekYaw, 0f, Time.deltaTime * peekSpeed);
                currentPeekPitch = Mathf.Lerp(currentPeekPitch, 0f, Time.deltaTime * peekSpeed);
                targetRotation = hidePosition.rotation;
            }

            playerCamera.rotation = Quaternion.Slerp(playerCamera.rotation, targetRotation, Time.deltaTime * peekSpeed);
        }
    }

    private void PlayRandomChestSound(AudioClip[] clips, float volume)
    {
        if (clips == null || clips.Length == 0 || chestAudioSource == null) return;

        AudioClip selectedClip = clips[Random.Range(0, clips.Length)];
        if (selectedClip != null)
        {
            chestAudioSource.PlayOneShot(selectedClip, volume);
        }
    }

    private void PlayOverheatStartSound()
    {
        if (overheatStartSound == null || overheatAudioSource == null) return;

        overheatAudioSource.clip = overheatStartSound;
        overheatAudioSource.volume = overheatSoundVolume;
        overheatAudioSource.loop = loopOverheatSound;
        overheatAudioSource.Play();
    }

    private void StopOverheatSound()
    {
        if (overheatAudioSource != null && overheatAudioSource.isPlaying)
        {
            overheatAudioSource.Stop();
        }
    }

    private Vector3 GetChestCenter()
    {
        Vector3 baseCenter = transform.position;

        Collider[] colliders = GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            Bounds combinedBounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                combinedBounds.Encapsulate(colliders[i].bounds);
            }
            baseCenter = combinedBounds.center;
        }
        else
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds combinedBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    combinedBounds.Encapsulate(renderers[i].bounds);
                }
                baseCenter = combinedBounds.center;
            }
        }

        return baseCenter + transform.TransformDirection(centerOffset);
    }

    private void FindAndCacheOverheatUI()
    {
        if (overheatUI == null)
        {
            Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (Transform t in allTransforms)
            {
                if (!t.gameObject.scene.IsValid()) continue;

                string lowerName = t.name.ToLower();
                if (lowerName.Contains("overheatui") || lowerName.Contains("overheatpanel") || lowerName.Contains("overheatcanvas"))
                {
                    overheatUI = t.gameObject;
                    break;
                }
            }
        }

        if (overheatUI != null)
        {
            if (overheatSlider == null)
            {
                overheatSlider = overheatUI.GetComponentInChildren<Slider>(true);
            }

            if (overheatFillImage == null && overheatSlider != null && overheatSlider.fillRect != null)
            {
                overheatFillImage = overheatSlider.fillRect.GetComponent<Image>();
            }
        }
        else
        {
            Debug.LogWarning($"[ChestHidingSpot] Could not find Overheat UI for {name}. Assign it in the Inspector or name the UI object 'OverheatUI'.");
        }
    }

    private void FindAndCachePlayer()
    {
        GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
        if (foundPlayer == null) return;

        playerRoot = foundPlayer.transform.root.gameObject;
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

    private void EnterChest()
    {
        if (hidePosition == null || peekPosition == null) return;

        if (overheatUI == null) FindAndCacheOverheatUI();

        MonsterAI[] allMonsters = UnityEngine.Object.FindObjectsByType<MonsterAI>(FindObjectsInactive.Exclude);
        MonsterVisionAI[] allVisionMonsters = UnityEngine.Object.FindObjectsByType<MonsterVisionAI>(FindObjectsInactive.Exclude);

        isHiding = true;
        wasPeeking = false;
        hasPlayedOverheatSound = false;
        ownsOverheatUI = true;
        currentPeekYaw = 0f;
        currentPeekPitch = 0f;

        PlayRandomChestSound(enterChestSounds, chestInteractVolume);

        if (currentOverheat > 0f)
        {
            timeHidden = delayBeforeOverheat;
        }
        else
        {
            timeHidden = 0f;
        }

        if (overheatUI != null && showUIImmediatelyOnEnter)
        {
            overheatUI.SetActive(true);
            if (overheatSlider != null)
            {
                overheatSlider.value = currentOverheat;
                if (overheatFillImage != null)
                {
                    overheatFillImage.color = overheatGradient.Evaluate(currentOverheat / 100f);
                }
            }
        }

        if (playerCamera != null)
        {
            originalCamParent = playerCamera.parent;
            originalCamLocalPos = playerCamera.localPosition;
            originalCamLocalRot = playerCamera.localRotation;

            playerCamera.SetParent(transform);
            playerCamera.position = hidePosition.position;
            playerCamera.rotation = hidePosition.rotation;
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
        if (cameraScript != null) cameraScript.enabled = false;

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.isKinematic = true;
        }

        Vector3 chestCenter = GetChestCenter();
        foreach (MonsterAI monster in allMonsters)
        {
            monster.InvestigateHiddenPlayer(chestCenter);
        }

        foreach (MonsterVisionAI monster in allVisionMonsters)
        {
            monster.InvestigateHiddenPlayer(chestCenter);
        }
    }

    private void ExitChest(bool voluntaryExit)
    {
        if (voluntaryExit)
        {
            StopOverheatSound();
        }
        else if (overheatAudioSource != null)
        {
            // Ensure it finishes playing its current run instead of looping forever
            overheatAudioSource.loop = false;
        }

        PlayRandomChestSound(exitChestSounds, chestInteractVolume);

        isHiding = false;
        wasPeeking = false;
        hasPlayedOverheatSound = false;
        currentPeekYaw = 0f;
        currentPeekPitch = 0f;

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

    private void OnDrawGizmosSelected()
    {
        Vector3 center = GetChestCenter();
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center, interactRange);
        Gizmos.DrawSphere(center, 0.1f);
    }
}