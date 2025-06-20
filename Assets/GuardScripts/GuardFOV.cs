using System.Collections;
using UnityEngine;

public class GuardFOV : MonoBehaviour
{
    public float radius;
    [Range(0, 360)]
    public float angle;

    public GameObject playerRef;

    public LayerMask targetMask;
    public LayerMask obstacleMask;

    public bool canSeePlayer;

    private UIManager uiManager;
    public float alertIncreaseOnSight = 20f;
    public float alertIncreaseOnHearing = 10f;

    private void Start()
    {
        playerRef = GameObject.FindGameObjectWithTag("Player");
        uiManager = FindObjectOfType<UIManager>();
        StartCoroutine(FOVRoutine());
    }

    private IEnumerator FOVRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.2f);

            while (true)
        {
            yield return wait;
            FieldOfViewCheck();
        }
    }

    public void OnSeePlayer()
    {

    }

    public void OnHearPlayer()
    {

    }


    private void FieldOfViewCheck()
    {
        Collider[] rangeChecks = Physics.OverlapSphere(transform.position, radius, targetMask);

        if (rangeChecks.Length != 0)
        {
            Transform target = rangeChecks[0].transform;
            Vector3 targetDirection = (target.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, targetDirection) < angle / 2)
            {
                float distanceToTarget = Vector3.Distance(transform.position, target.position);

                if (!Physics.Raycast(transform.position, targetDirection, distanceToTarget, obstacleMask))
                    canSeePlayer = true;
                else
                    canSeePlayer = false;
            }
            else
                canSeePlayer = false;
        }
        else if (canSeePlayer)
            canSeePlayer = false;
    }
        
}
