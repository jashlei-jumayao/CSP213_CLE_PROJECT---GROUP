using UnityEngine;

public class WheeledRobotMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 2f;
    public float turnSpeed = 120f;
    public float stoppingDamping = 10f;

    private float verticalInput;
    private float turnInput;
    private bool isSprinting;

    private Rigidbody rb;
    private RobotEnergy energySystem;
    private bool isEnsnared = false;
    private float originalSpeed;
    private Coroutine ensnareRoutine;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        energySystem = GetComponent<RobotEnergy>();
    }

    private void Update()
    {
        verticalInput = 0f;
        if (Input.GetKey(KeyCode.W)) verticalInput += 1f;
        if (Input.GetKey(KeyCode.S)) verticalInput -= 1f;

        turnInput = 0f;
        if (Input.GetKey(KeyCode.D)) turnInput += 1f;
        if (Input.GetKey(KeyCode.A)) turnInput -= 1f;

        isSprinting = Input.GetKey(KeyCode.LeftShift);

        bool isTryingToMove = Mathf.Abs(verticalInput) > 0.01f || Mathf.Abs(turnInput) > 0.01f;

        if (energySystem != null && isTryingToMove)
        {
            if (energySystem.HasEnergy())
            {
                float drainAmount = (isSprinting && Mathf.Abs(verticalInput) > 0.01f) ? sprintMultiplier : 1f;
                energySystem.ConsumeEnergy(drainAmount);
            }
            else
            {
                verticalInput = 0f;
                turnInput = 0f;
            }
        }

        float rotationAmount = turnInput * turnSpeed * Time.deltaTime;
        transform.Rotate(0f, rotationAmount, 0f);
    }

    private void FixedUpdate()
    {
        if (Mathf.Abs(verticalInput) > 0.01f)
        {
            float targetSpeed = isSprinting ? (moveSpeed * sprintMultiplier) : moveSpeed;
            Vector3 targetVelocity = transform.forward * verticalInput * targetSpeed;

            rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
        }
        else
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Vector3 dampedVelocity = Vector3.Lerp(horizontalVelocity, Vector3.zero, stoppingDamping * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector3(dampedVelocity.x, rb.linearVelocity.y, dampedVelocity.z);
        }
    }

    public void ApplyEnsnare(float slowMultiplier, float duration)
    {
        if (ensnareRoutine != null) StopCoroutine(ensnareRoutine);
        ensnareRoutine = StartCoroutine(EnsnareRoutine(slowMultiplier, duration));
    }

    private System.Collections.IEnumerator EnsnareRoutine(float slowMultiplier, float duration)
    {
        if (!isEnsnared)
        {
            originalSpeed = moveSpeed;
            isEnsnared = true;
        }

        moveSpeed = originalSpeed * slowMultiplier;
        Debug.Log("Player Ensnared! Speed reduced.");

        yield return new WaitForSeconds(duration);

        moveSpeed = originalSpeed;
        isEnsnared = false;
        Debug.Log("Player broke free from ensnare.");
    }
}