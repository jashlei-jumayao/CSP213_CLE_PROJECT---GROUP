using UnityEngine;

public class WheeledRobotMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 2f;   // How much faster the robot goes when sprinting
    public float turnSpeed = 120f;
    public float stoppingDamping = 10f;

    private float verticalInput;
    private float turnInput;
    private bool isSprinting;             // Tracks if Shift is held

    private Rigidbody rb;
    private RobotEnergy energySystem;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        energySystem = GetComponent<RobotEnergy>();
    }

    private void Update()
    {
        // --- Read Inputs ---
        verticalInput = 0f;
        if (Input.GetKey(KeyCode.W)) verticalInput += 1f;
        if (Input.GetKey(KeyCode.S)) verticalInput -= 1f;

        turnInput = 0f;
        if (Input.GetKey(KeyCode.D)) turnInput += 1f;
        if (Input.GetKey(KeyCode.A)) turnInput -= 1f;

        // Check if holding Left Shift to sprint
        isSprinting = Input.GetKey(KeyCode.LeftShift);

        // --- Energy Check ---
        bool isTryingToMove = Mathf.Abs(verticalInput) > 0.01f || Mathf.Abs(turnInput) > 0.01f;

        if (energySystem != null && isTryingToMove)
        {
            if (energySystem.HasEnergy())
            {
                // If moving forward/backward AND sprinting, double the energy drain
                float drainAmount = (isSprinting && Mathf.Abs(verticalInput) > 0.01f) ? sprintMultiplier : 1f;
                energySystem.ConsumeEnergy(drainAmount);
            }
            else
            {
                // OUT OF ENERGY
                verticalInput = 0f;
                turnInput = 0f;
            }
        }

        // --- Rotate Chassis ---
        float rotationAmount = turnInput * turnSpeed * Time.deltaTime;
        transform.Rotate(0f, rotationAmount, 0f);
    }

    private void FixedUpdate()
    {
        // --- Move or Brake ---
        if (Mathf.Abs(verticalInput) > 0.01f)
        {
            // Apply sprint multiplier to the movement speed if Shift is held
            float currentSpeed = isSprinting ? (moveSpeed * sprintMultiplier) : moveSpeed;

            Vector3 moveDirection = transform.forward * verticalInput;
            rb.AddForce(moveDirection.normalized * currentSpeed * 10f, ForceMode.Force);
        }
        else
        {
            // Brake
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Vector3 dampedVelocity = Vector3.Lerp(horizontalVelocity, Vector3.zero, stoppingDamping * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector3(dampedVelocity.x, rb.linearVelocity.y, dampedVelocity.z);
        }
    }
}