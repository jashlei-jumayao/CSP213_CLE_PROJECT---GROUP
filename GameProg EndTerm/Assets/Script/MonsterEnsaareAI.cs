using UnityEngine;
using UnityEngine.AI;

public class MonsterEnsareAI : MonoBehaviour
{
    public enum MonsterState { Wandering, Investigating, Sweeping }

    [Header("Movement Speeds")]
    public float wanderSpeed = 2.0f;
    public float suspicionSpeed = 3.5f;
    public float huntSpeed = 7.0f;

    [Header("Wander Settings")]
    public float wanderRadius = 15f;
    public float wanderWaitTime = 2f;

    [Header("Vision Settings")]
    public float viewRadius = 15f;
    [Range(0f, 360f)]
    public float viewAngle = 90f;
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;
    public float sightLoseTimeout = 5f;

    [Header("Area Sweep Settings")]
    public int sweepCount = 3;
    public float sweepRadius = 5f;
    public float sweepWaitTime = 1f;

    private NavMeshAgent agent;
    private MonsterState currentState = MonsterState.Wandering;

    private Transform targetPlayer;
    private Vector3 lastSeenPosition;
    private float lastSeenTime;
    private float waitTimer;
    private int sweepsRemaining;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            targetPlayer = playerObj.transform;
        }

        ReturnToWandering();
    }

    private void Update()
    {
        CheckForPlayerVision();

        switch (currentState)
        {
            case MonsterState.Wandering:
                HandleWandering();
                break;
            case MonsterState.Investigating:
                HandleInvestigating();
                break;
            case MonsterState.Sweeping:
                HandleSweeping();
                break;
        }
    }

    private void CheckForPlayerVision()
    {
        if (targetPlayer == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
        if (distanceToPlayer <= viewRadius)
        {
            Vector3 dirToPlayer = (targetPlayer.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2f)
            {
                if (!Physics.Raycast(transform.position + Vector3.up, dirToPlayer, distanceToPlayer, obstacleLayer))
                {
                    // Player is spotted! Trigger the hunt. (No ensnare here anymore!)
                    SpotPlayer(targetPlayer.position);
                }
            }
        }
    }

    private void SpotPlayer(Vector3 playerPosition)
    {
        lastSeenPosition = playerPosition;
        lastSeenTime = Time.time;
        currentState = MonsterState.Investigating;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(lastSeenPosition);
            agent.speed = huntSpeed;
        }
    }

    private void HandleWandering()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= wanderWaitTime)
            {
                PickRandomDestination(wanderRadius);
                waitTimer = 0f;
            }
        }
    }

    private void HandleInvestigating()
    {
        if (targetPlayer != null && Vector3.Distance(transform.position, targetPlayer.position) <= viewRadius)
        {
            Vector3 dirToPlayer = (targetPlayer.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dirToPlayer) < viewAngle / 2f)
            {
                if (!Physics.Raycast(transform.position + Vector3.up, dirToPlayer, Vector3.Distance(transform.position, targetPlayer.position), obstacleLayer))
                {
                    lastSeenPosition = targetPlayer.position;
                    lastSeenTime = Time.time;
                    agent.SetDestination(lastSeenPosition);
                    agent.speed = huntSpeed;
                    return;
                }
            }
        }

        float timeSinceLostSight = Time.time - lastSeenTime;
        if (timeSinceLostSight >= sightLoseTimeout)
        {
            ReturnToWandering();
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            StartSweeping();
        }
    }

    private void StartSweeping()
    {
        currentState = MonsterState.Sweeping;
        sweepsRemaining = sweepCount;
        waitTimer = 0f;
        agent.speed = suspicionSpeed;
        PickRandomDestination(sweepRadius);
    }

    private void HandleSweeping()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= sweepWaitTime)
            {
                sweepsRemaining--;

                if (sweepsRemaining > 0)
                {
                    PickRandomDestination(sweepRadius);
                    waitTimer = 0f;
                }
                else
                {
                    ReturnToWandering();
                }
            }
        }
    }

    private void ReturnToWandering()
    {
        currentState = MonsterState.Wandering;
        waitTimer = 0f;
        if (agent != null) agent.speed = wanderSpeed;
        PickRandomDestination(wanderRadius);
    }

    private void PickRandomDestination(float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        Vector3 searchCenter = (currentState == MonsterState.Sweeping) ? lastSeenPosition : transform.position;
        randomDirection += searchCenter;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}