using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float sprintSpeed = 7f;
    public float walkSpeed = 4f;
    public float sneakSpeed = 2f;
    public float rotationSpeed = 210f;

    public float sprintNoise = 15f;
    public float walkNoise = 7f;
    public float sneakNoise = 1f;

    private Vector3 lastPosition;

    void Update()
    {
        // Get input for movement
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = new Vector3(horizontal, 0, vertical).normalized;

        if (inputDirection.magnitude >= 0.1f)
        {
            // Determine movement type
            bool isSprinting = Input.GetKey(KeyCode.LeftShift);
            bool isSneaking = Input.GetKey(KeyCode.LeftControl);

            float moveSpeed = walkSpeed;
            float noiseLevel = walkNoise;

            if (isSprinting)
            {
                moveSpeed = sprintSpeed;
                noiseLevel = sprintNoise;
            }
            else if (isSneaking)
            {
                moveSpeed = sneakSpeed;
                noiseLevel = sneakNoise;
            }

            // Calculate movement direction relative to the player's facing direction
            float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + transform.eulerAngles.y;
            Vector3 moveDirection = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;

            // Move and rotate the player
            transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, targetAngle, 0), rotationSpeed * Time.deltaTime);

            // Create sound at intervals
            if (Vector3.Distance(transform.position, lastPosition) > 0.5f)
            {
                SoundManager.EmitNoise(transform.position, noiseLevel);
                lastPosition = transform.position;
            }
        }
    }
   
    public bool IsSprinting()
    {
        return Input.GetKey(KeyCode.LeftShift);
    }

    public bool IsSneaking()
    {
        return Input.GetKey(KeyCode.LeftControl);
    }

}
