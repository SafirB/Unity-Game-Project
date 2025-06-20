using UnityEngine;
using System.Collections;


public class SpotlightDetection : MonoBehaviour
{
    public LayerMask playerMask;
    public LayerMask obstacleMask;

    private Light spotLight;
    private bool onCooldown = false;
    public float detectionCooldown = 5f;


    void Start()
    {
        spotLight = GetComponent<Light>();
    }

    void Update()
    {
        DetectPlayer();
    }

    void DetectPlayer()
    {
        if (onCooldown || spotLight == null) return;

        float range = spotLight.range;
        float halfAngle = spotLight.spotAngle / 2;

        Collider[] players = Physics.OverlapSphere(transform.position, range, playerMask);
        foreach (Collider player in players)
        {
            Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

            if (angleToPlayer < halfAngle)
            {
                if (!Physics.Raycast(transform.position, directionToPlayer, out RaycastHit hit, range, obstacleMask))
                {
                    Debug.Log("[Camera] Player detected.");
                    AlertGuards(player.transform.position);
                    StartCoroutine(DetectionCooldown());
                    return;
                }
            }
        }
    }

    IEnumerator DetectionCooldown()
    {
        onCooldown = true;
        yield return new WaitForSeconds(detectionCooldown);
        onCooldown = false;
    }


    void AlertGuards(Vector3 playerPosition)
    {
        UIManager.LastKnownPlayerPosition = playerPosition;

        AdaptiveGuard[] adaptiveGuards = FindObjectsOfType<AdaptiveGuard>();
        AdaptivePatroller[] adaptivePatrollers = FindObjectsOfType<AdaptivePatroller>();
        Patroller[] patrollers = FindObjectsOfType<Patroller>();

        foreach (var guard in adaptiveGuards)
            guard.InvestigateCamera(playerPosition);

        foreach (var guard in adaptivePatrollers)
            guard.InvestigateCamera(playerPosition);

        foreach (var guard in patrollers)
            guard.InvestigateCamera(playerPosition);
    }
}
