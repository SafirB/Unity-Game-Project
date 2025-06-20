using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;



public class AdaptivePatroller : MonoBehaviour
{
    // FSM states
    public enum GuardState { Patrolling, Suspicious, Aware, Hearing }
    private GuardState currentState = GuardState.Patrolling;

    // Patrol Set variables
    [System.Serializable]
    public class PatrolSet
    {
        public Transform[] patrolPoints;
    }

    public PatrolSet[] patrolSets;
    private int currentSetIndex = 0;
    private int currentPatrolIndex = 0;

    // NavMeshagent reference
    private NavMeshAgent agent;

    // SuspicionIndicator reference
    public TextMeshProUGUI suspicionIndicator;

    // Gun script reference
    private Gun gun;

    // Guard FOV script reference
    private GuardFOV guardFOV;

    // Suspicion system Variables
    public float suspicionMeter = 0f;
    public float suspicionInvestigateThreshold = 50f;
    public float suspicionChaseThreshold = 100f;
    public float suspicionIncreaseFromSight = 20f;
    public float suspicionIncreaseFromSound = 25f;
    public float suspicionDecayRate = 10f;
    private Vector3 lastKnownPlayerPosition;
    private bool seesPlayer = false;
    public float searchDuration = 4f;
    public float searchTurnSpeed = 100f;

    // On Hearing Something Variables
    private float soundReactionTimer = 0f;
    public float soundReactionTime = 2.5f;
    private Vector3 noiseDirection;

    // Search routine variables
    public int searchPointsToVisit = 3;
    public float searchRadius = 4f;
    private int currentSearchPoint = 0;
    private bool isSearching = false;

    // High alert variables
    private float normalSpeed;
    public float highAlertSpeedMultiplier = 1.5f;



    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        normalSpeed = agent.speed;
        guardFOV = GetComponent<GuardFOV>();
        gun = GetComponentInChildren<Gun>();


        if (patrolSets.Length > 0 && patrolSets[currentSetIndex].patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolSets[currentSetIndex].patrolPoints[currentPatrolIndex].position);
        }

        SoundManager.NoiseMade += OnNoiseHeard;
    }
    
    void OnDestroy()
    {
        SoundManager.NoiseMade -= OnNoiseHeard;
    }

    void Update()
    {
        seesPlayer = guardFOV.canSeePlayer;
        UIManager.LastKnownPlayerPosition = guardFOV.playerRef.transform.position;

        if (UIManager.Instance != null)
        {
            if (UIManager.Instance.isHighAlert)
            {
                agent.speed = normalSpeed * highAlertSpeedMultiplier;
            }
            else
            {
                agent.speed = normalSpeed;
            }
        }


        if (suspicionIndicator != null)
        {
            if (currentState == GuardState.Aware)
            {
                suspicionIndicator.text = "!";
                suspicionIndicator.color = Color.red;
            }
            else if (currentState == GuardState.Suspicious)
            {
                suspicionIndicator.text = "?";
                suspicionIndicator.color = Color.yellow;
            }
            else
            {
                suspicionIndicator.text = "";
            }
        }


        if (currentState == GuardState.Patrolling || currentState == GuardState.Suspicious)
        {
            HandleSuspicion();
        }


        switch (currentState)
        {
            case GuardState.Patrolling:
                Patrol();
                break;
            case GuardState.Suspicious:
                SuspiciousLookaround();
                break;
            case GuardState.Aware:
                ChasePlayer();
                break;
            case GuardState.Hearing:
                HearingBehavior();
                break;
        }
    }




    void HandleSuspicion()
    {
        if (seesPlayer)
        {
            suspicionMeter += suspicionIncreaseFromSight * Time.deltaTime;
            lastKnownPlayerPosition = guardFOV.playerRef.transform.position;
        }
        else if (currentState == GuardState.Patrolling)
        {
            suspicionMeter -= suspicionDecayRate * Time.deltaTime;
        }


        suspicionMeter = Mathf.Clamp(suspicionMeter, 0f, suspicionChaseThreshold);

        if (suspicionMeter >= suspicionChaseThreshold && currentState != GuardState.Aware)
        {
            suspicionMeter = 0f;
            ChangeState(GuardState.Aware);
        }
        else if (suspicionMeter >= suspicionInvestigateThreshold && currentState == GuardState.Patrolling)
        {
            ChangeState(GuardState.Suspicious);
        }
        else if (suspicionMeter <= 0f && currentState == GuardState.Suspicious)
        {
            ChangeState(GuardState.Patrolling);
        }
    }


    void Patrol()
    {
        agent.isStopped = false;

        if (patrolSets.Length == 0 || patrolSets[currentSetIndex].patrolPoints.Length == 0)
            return;

        Transform[] currentSet = patrolSets[currentSetIndex].patrolPoints;

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % currentSet.Length;
            agent.SetDestination(currentSet[currentPatrolIndex].position);
        }
    }


    void SuspiciousLookaround()
    {

        if (suspicionIndicator != null)
        {
            suspicionIndicator.text = "?";
            suspicionIndicator.color = Color.yellow;
        }

        if (seesPlayer)
        {
            suspicionMeter = 0f;
            ChangeState(GuardState.Aware);
            return;
        }

        if (!isSearching)
        {
            agent.SetDestination(lastKnownPlayerPosition);

            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                StartCoroutine(SearchRoutine());
                isSearching = true;
            }
        }
    }


    IEnumerator SearchRoutine()
    {
        isSearching = true;
        agent.isStopped = false;
        currentSearchPoint = 0;

        while (currentSearchPoint < searchPointsToVisit)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-searchRadius, searchRadius),
                0,
                Random.Range(-searchRadius, searchRadius)
            );

            Vector3 searchTarget = lastKnownPlayerPosition + randomOffset;
            agent.SetDestination(searchTarget);

            // Wait for guard to arrive at the search point
            while (agent.pathPending || agent.remainingDistance > 0.5f)
            {
                if (guardFOV.canSeePlayer)
                {
                    suspicionMeter = 0f;
                    isSearching = false;
                    ChangeState(GuardState.Aware);
                    yield break;
                }

                yield return null;
            }

            // Rotate/look around at the search point
            float lookDuration = 1.5f;
            float elapsed = 0f;

            while (elapsed < lookDuration)
            {
                if (guardFOV.canSeePlayer)
                {
                    suspicionMeter = 0f;
                    isSearching = false;
                    ChangeState(GuardState.Aware);
                    yield break;
                }

                float rotationDirection = Mathf.Sin(Time.time * 2f);
                transform.Rotate(0f, rotationDirection * searchTurnSpeed * Time.deltaTime, 0f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            currentSearchPoint++;
        }

        // Done searching
        isSearching = false;
        suspicionMeter = 0f;
        ChangeState(GuardState.Patrolling);
    }

    void ChasePlayer()
    {
        suspicionMeter = suspicionChaseThreshold;
        agent.isStopped = false;

        if (suspicionIndicator != null)
        {
            suspicionIndicator.text = "!";
            suspicionIndicator.color = Color.red;
        }

        if (seesPlayer && guardFOV.playerRef != null)
        {
            lastKnownPlayerPosition = guardFOV.playerRef.transform.position;

            float distanceToPlayer = Vector3.Distance(transform.position, lastKnownPlayerPosition);

            if (distanceToPlayer > 10f) // Maintain distance
            {
                agent.SetDestination(lastKnownPlayerPosition);
            }
            else
            {
                agent.isStopped = true;
                transform.LookAt(new Vector3(lastKnownPlayerPosition.x, transform.position.y, lastKnownPlayerPosition.z));
            }

            if (gun != null)
            {
                gun.TryShoot(guardFOV.playerRef.transform);
            }


            if (UIManager.Instance != null)
            {
                UIManager.Instance.IncreaseAlert(10f * Time.deltaTime);
            }
        }
        else
        {
            agent.SetDestination(lastKnownPlayerPosition);

            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                suspicionMeter = suspicionInvestigateThreshold;
                SwitchToClosestPatrolSet();
                ChangeState(GuardState.Suspicious);
            }
        }

    }


    void SwitchToClosestPatrolSet()
    {
        float closestDistance = Mathf.Infinity;
        int bestSet = currentSetIndex;

        for (int i = 0; i < patrolSets.Length; i++)
        {
            if (patrolSets[i].patrolPoints.Length == 0) continue;

            float distance = Vector3.Distance(transform.position, patrolSets[i].patrolPoints[0].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestSet = i;
            }
        }

        if (bestSet != currentSetIndex)
        {
            currentSetIndex = bestSet;
            currentPatrolIndex = 0;
        }
    }


    void ChangeState(GuardState newState)
    {
        if (suspicionIndicator != null)
        {
            suspicionIndicator.text = "";
        }

        if (currentState != newState)
        {
            Debug.Log($"[AdaptivePatroller] Switched to state: {newState}");
            currentState = newState;
        }
    }

    void HearingBehavior()
    {
        agent.isStopped = true;

        if (suspicionIndicator != null)
        {
            suspicionIndicator.text = "?";
            suspicionIndicator.color = Color.yellow;
        }

        if (seesPlayer)
        {
            suspicionMeter = 0f;
            ChangeState(GuardState.Aware);
            return;
        }

        if (noiseDirection != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(noiseDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, 360 * Time.deltaTime);
        }

        soundReactionTimer -= Time.deltaTime;

        if (soundReactionTimer <= 0f)
        {
            agent.isStopped = false;

            if (suspicionMeter >= suspicionChaseThreshold)
            {
                suspicionMeter = 0f;
                ChangeState(GuardState.Aware);
            }
            else if (suspicionMeter >= suspicionInvestigateThreshold)
            {
                ChangeState(GuardState.Suspicious);
            }
            else
            {
                ChangeState(GuardState.Patrolling);
            }
        }
    }


    public void OnNoiseHeard(Vector3 noisePosition, float noiseRadius)
    {
        if (currentState == GuardState.Aware || currentState == GuardState.Suspicious || currentState == GuardState.Hearing)
            return;


        if (Vector3.Distance(transform.position, noisePosition) <= noiseRadius)
        {
            Debug.Log("[AdaptivePatroller] Heard a noise. Adding suspicion.");
            suspicionMeter += suspicionIncreaseFromSound;
            lastKnownPlayerPosition = noisePosition;

            if (currentState == GuardState.Patrolling)
            {
                noiseDirection = (noisePosition - transform.position).normalized;
                noiseDirection.y = 0f;

                soundReactionTimer = soundReactionTime;
                ChangeState(GuardState.Hearing);
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.IncreaseAlert(5f);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (currentState == GuardState.Aware && other.CompareTag("Player"))
        {
            Debug.Log("Player caught!");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowGameOver();
            }
        }
    }

    public void InvestigateFromAlert(Vector3 position)
    {
        if (currentState != GuardState.Aware)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-2f, 2f),
                0f,
                Random.Range(-2f, 2f)
            );

            Vector3 adjustedTarget = position + randomOffset;

            if (NavMesh.SamplePosition(adjustedTarget, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                agent.SetDestination(position); // fallback
            }

            lastKnownPlayerPosition = agent.destination;
            ChangeState(GuardState.Suspicious);
        }
    }

    public void InvestigateCamera(Vector3 position)
    {
        if (currentState != GuardState.Aware)
        {
            Vector3 offset = new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
            Vector3 finalPos = position + offset;

            if (NavMesh.SamplePosition(finalPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
            else
                agent.SetDestination(position);

            lastKnownPlayerPosition = agent.destination;
            ChangeState(GuardState.Suspicious);
        }
    }

}
