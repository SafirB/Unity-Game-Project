using System.Collections;
using UnityEngine;

public class SecurityCamera : MonoBehaviour
{
    // Rotation settings
    public float rotationSpeed = 120f;
    public float leftRotationLimit = -45f;
    public float rightRotationLimit = 45f;

    private bool rotatingRight = true;
    private float initialYRotation;

    void Start()
    {
        // So it stays in the rotation I set
        initialYRotation = transform.eulerAngles.y;
        StartCoroutine(RotateCamera());
    }

    // Infinite Loop for camera rotation
    IEnumerator RotateCamera()
    {
        while (true)
        {
            float targetAngle = rotatingRight ? initialYRotation + rightRotationLimit : initialYRotation + leftRotationLimit;
            yield return StartCoroutine(TurnToAngle(targetAngle));
            rotatingRight = !rotatingRight;
            yield return new WaitForSeconds(1f);
        }
    }

    // Rotates to target angle
    IEnumerator TurnToAngle(float targetAngle)
    {
        float currentAngle = transform.eulerAngles.y;
        while (Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle)) > 1f)
        {
            currentAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
            transform.eulerAngles = new Vector3(0, currentAngle, 0);
            yield return null;
        }
    }
}
