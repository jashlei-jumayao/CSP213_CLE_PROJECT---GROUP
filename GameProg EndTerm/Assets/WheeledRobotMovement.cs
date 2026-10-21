using UnityEngine;

public class WheeledRobotMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float turnSpeed = 120f;        // Rotation speed for A / D keys

    private float verticalInput;
    private float turnInput;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Prevent physics forces from tipping or knocking over the robot
        rb.freezeRotation = true;
    }

    private void Update()
    {
        // --- Read W / S for Forward / Backward ---
        verticalInput = 0f;
        if (Input.GetKey(KeyCode.W)) verticalInput += 1f;
        if (Input.GetKey(KeyCode.S)) verticalInput -= 1f;

        // --- Read A / D for Turning Chassis Left / Right ---
        turnInput = 0f;
        if (Input.GetKey(KeyCode.D)) turnInput += 1f;
        if (Input.GetKey(KeyCode.A)) turnInput -= 1f;

        // --- Rotate Chassis with A and D ---
        float rotationAmount = turnInput * turnSpeed * Time.deltaTime;
        transform.Rotate(0f, rotationAmount, 0f);
    }

    private void FixedUpdate()
    {
        // --- Move Forward / Backward along Chassis direction with W / S ---
        Vector3 moveDirection = transform.forward * verticalInput;
        rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
    }
}