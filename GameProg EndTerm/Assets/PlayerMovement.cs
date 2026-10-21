using UnityEngine;

public class PlayerMovementTutorial : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;   // Drag Main Camera here

    [Header("Movement Settings")]
    public float moveSpeed = 7f;

    private float verticalInput;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        // W moves forward, S moves backward
        verticalInput = 0f;
        if (Input.GetKey(KeyCode.W)) verticalInput += 1f;
        if (Input.GetKey(KeyCode.S)) verticalInput -= 1f;
    }

    private void FixedUpdate()
    {
        if (cameraTransform == null) return;

        // --- Get Forward Direction directly from Camera ---
        Vector3 moveDirection = cameraTransform.forward * verticalInput;

        // Keep movement strictly horizontal so W/S doesn't fly into the air when looking up
        moveDirection.y = 0f;
        moveDirection.Normalize();

        // Apply movement force
        rb.AddForce(moveDirection * moveSpeed * 10f, ForceMode.Force);
    }
}