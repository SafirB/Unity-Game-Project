using UnityEngine;
using UnityEngine.AI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public class InterestPoint
{
    public Vector3 position;
    public float weight;
    public float lastVisitedTime;

    public InterestPoint(Vector3 pos)
    {
        position = pos;
        weight = 1f;
        lastVisitedTime = -999f;
    }
}



public class AdaptiveGuard : MonoBehaviour
{
    public enum GuardState { Patrolling, Hearing, Suspicious, Aware, Searching }
    private GuardState currentState = GuardState.Patrolling;

    // Interest Points
    public float interestCooldown = 10f;
    [Range(0f, 1f)]
    public float chanceToUseInterestPoints = 0.7f; // 70% chance

    // Script References
    private GuardFOV guardFOV;
    private NavMeshAgent agent;
    public TextMeshProUGUI suspicionIndicator;

    // Suspicion Variables
    public float suspicionMeter = 0f;
    public float suspicionInvestigateThreshold = 50f;
    public float suspicionChaseThreshold = 100f;
    public float suspicionIncreaseFromSight = 20f;
    public float suspicionDecayRate = 10f;
    private Vector3 lastKnownPlayerPosition;
    public float suspicionIncreaseFromSound = 25f;

    // Sound variables
    public float soundReactionTime = 2f;
    private Vector3 lastHeardPosition;
    private float soundReactionTimer = 0f;

    private bool seesPlayer = false;
    
    private float normalSpeed;

    // Search Variables
    private bool isSearching = false;
    public float searchDuration = 4f;
    public float searchTurnSpeed = 100f;
    public int searchPointsToVisit = 3;
    public float searchRadius = 4f;
    private int currentSearchPoint = 0;

    private Vector3 noiseDirection;


    // Roaming Variables
    public float roamRadius = 15f;
    public float roamCooldown = 3f;

    private float roamTimer = 0f;
    private Vector3 currentRoamTarget;

    // Interest Points List
    private List<InterestPoint> interestPoints = new List<InterestPoint>();


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        guardFOV = GetComponent<GuardFOV>();
        normalSpeed = agent.speed;

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

        // Suspicion UI Icon
        if (suspicionIndicator != null)
        {
            if (currentState == GuardState.Aware)
            {
                suspicionIndicator.text = "!";
                suspicionIndicator.color = Color.red;
            }
            else if (currentState == GuardState.Suspicious || currentState == GuardState.Searching)
            {
                suspicionIndicator.text = "?";
                suspicionIndicator.color = Color.yellow;
            }
            else
            {
                suspicionIndicator.text = "";
            }
        }

        if (UIManager.Instance != null && UIManager.Instance.isHighAlert)
        {
            agent.speed = normalSpeed * 1.5f;
        }
        else
        {
            agent.speed = normalSpeed;
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
            case GuardState.Hearing:
                HearingBehavior();
                break;
            case GuardState.Suspicious:
                SuspiciousLookaround();
                break;
            case GuardState.Aware:
                ChasePlayer();
                break;
            case GuardState.Searching:
                SearchRoutine();
                break;
        }
    }

    void Patrol()
    {
        roamTimer -= Time.deltaTime;

        if (agent.pathPending || agent.remainingDistance > 0.5f)
            return;

        if (roamTimer <= 0f)
        {
            Vector3 targetPos;

            if (interestPoints.Count > 0 && Random.value < chanceToUseInterestPoints)
            {
                float totalWeight = 0f;
                float currentTime = Time.time;

                foreach (var point in interestPoints)
                {
                    if (currentTime - point.lastVisitedTime >= interestCooldown)
                    {
                        totalWeight += point.weight;
                    }
                }

                float randomWeight = Random.Range(0f, totalWeight);
                float runningSum = 0f;

                foreach (var point in interestPoints)
                {
                    if (currentTime - point.lastVisitedTime >= interestCooldown)
                    {
                        runningSum += point.weight;
                        if (randomWeight <= runningSum)
                        {
                            point.lastVisitedTime = currentTime;
                            targetPos = point.position;
                            goto SetRoamTarget;
                        }
                    }
                }
            }


            Vector3 randomDirection = Random.insideUnitSphere * roamRadius;
            randomDirection += transform.position;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit fallbackHit, roamRadius, NavMesh.AllAreas))
            {
                targetPos = fallbackHit.position;
            }
            else
            {
                roamTimer = roamCooldown;
                return;
            }

        SetRoamTarget:
            agent.SetDestination(targetPos);
            roamTimer = roamCooldown;
        }
    }


    void ChasePlayer()
    {
        suspicionMeter = suspicionChaseThreshold;
        agent.isStopped = false;

        if (seesPlayer && guardFOV.playerRef != null)
        {
            lastKnownPlayerPosition = guardFOV.playerRef.transform.position;
            agent.SetDestination(lastKnownPlayerPosition);

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
                ChangeState(GuardState.Suspicious);
            }
        }

    }


    void SuspiciousLookaround()
    {
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

            // Wait until the guard arrives at the search point
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

            // Simulate scanning at this search point
            float rotateTime = 1.5f;
            float elapsed = 0f;

            while (elapsed < rotateTime)
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

        // Finished searching
        isSearching = false;
        suspicionMeter = 0f;
        ChangeState(GuardState.Patrolling);
    }



    void HearingBehavior()
    {
        agent.isStopped = true;

        if (seesPlayer)
        {
            suspicionMeter = 0f;
            ChangeState(GuardState.Aware);
            return;
        }

        if (lastHeardPosition != Vector3.zero)
        {
            noiseDirection = lastHeardPosition - transform.position;
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


    void ChangeState(GuardState newState)
    {
        if (currentState != newState)
        {
            Debug.Log($"[AdaptiveGuard] Switched to state: {newState}");
            currentState = newState;
        }
    }


    public void OnNoiseHeard(Vector3 position, float radius)
    {
        if (currentState == GuardState.Hearing || currentState == GuardState.Suspicious || currentState == GuardState.Aware)
            return;

        if (Vector3.Distance(transform.position, position) <= radius)
        {
            lastHeardPosition = position;
            lastKnownPlayerPosition = position;
            soundReactionTimer = soundReactionTime;

            suspicionMeter += suspicionIncreaseFromSound;

            ChangeState(GuardState.Hearing);

            if (UIManager.Instance != null && UIManager.Instance.isHighAlert)
            {
                ChangeState(GuardState.Suspicious);
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.IncreaseAlert(5f);
            }

            RememberInterestZone(position);
        }
    }


    void RememberInterestZone(Vector3 pos)
    {
        float mergeDistance = 5f;

        foreach (var point in interestPoints)
        {
            if (Vector3.Distance(point.position, pos) < mergeDistance)
            {
                point.weight += 1f;
                return;
            }
        }

        interestPoints.Add(new InterestPoint(pos));
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        foreach (var point in interestPoints)
        {
            Gizmos.DrawWireSphere(point.position, 0.5f + point.weight * 0.1f);
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowGameOver();
            }
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
