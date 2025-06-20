using UnityEngine;

public class Gun : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float shootInterval = 1.5f;

    private float shootTimer = 0f;

    void Update()
    {
        shootTimer -= Time.deltaTime;
    }

    public void TryShoot(Transform target)
    {
        if (shootTimer <= 0f)
        {
            shootTimer = shootInterval;

            if (bulletPrefab != null && firePoint != null)
            {
                GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            }
        }
    }
}
