using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;  

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

        verticalInput = 0f;
        if (Input.GetKey(KeyCode.W)) verticalInput += 1f;
        if (Input.GetKey(KeyCode.S)) verticalInput -= 1f;
    }

    private void FixedUpdate()
    {
        if (cameraTransform == null) return;


        Vector3 moveDirection = cameraTransform.forward * verticalInput;

      
        moveDirection.y = 0f;
        moveDirection.Normalize();


        rb.AddForce(moveDirection * moveSpeed * 10f, ForceMode.Force);
    }
}