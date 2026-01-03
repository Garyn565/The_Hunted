using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class Hunting : MonoBehaviour
{
    public enum AIState { Wandering, Scanning, Chasing, Following }
    public AIState currentState = AIState.Wandering;

    [Header("Movement Settings")]
    public float wanderRadius = 10f;
    public float wanderInterval = 5f;
    public float scanInterval = 2f;

    [Header("Chase Settings")]
    public float chaseSpeed = 4f;
    public float normalSpeed = 2f;
    public float detectionRadius = 15f;
    public float loseSightMultiplier = 2f;

    [Header("Boids (Hybrid) Settings")]
    public float neighborRadius = 5f;
    public float separationWeight = 3f;
    public float alignmentWeight = 2.5f;
    public float cohesionWeight = 1.8f;
    public float maxSteer = 6f;

    [Header("Communication")]
    public float alertRadius = 20f;

    [Header("Debug")]
    public bool drawDebugNeighbors = false;

    private NavMeshAgent agent;
    private Transform player;

    private float wanderTimer;
    private float scanTimer;

    // FOLLOWER MODE: Who to follow?
    private Transform leaderToFollow = null;

    // State control
    private bool hasAlertedNearby = false;
    private Vector3 cachedSteering;

    public float boidsTimer { get; private set; }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = normalSpeed;

        GameObject obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null)
            player = obj.transform;

        wanderTimer = wanderInterval;
        scanTimer = scanInterval;
    }

    // Called by AI_Communication (leader)
    public void BeginLeaderHunt(Vector3 enemyPos)
    {
        leaderToFollow = null;       // I'm the leader
        currentState = AIState.Chasing;

        hasAlertedNearby = true;
        agent.speed = chaseSpeed;

        agent.SetDestination(enemyPos);
    }

    // Called by AI_Communication (followers)
    public void BeginFollowerHunt(Transform leaderTransform)
    {
        // In HandleChasing and HandleFollowing, when reverting to wandering:
        var comm = GetComponent<AI_Communication>();
        if (comm != null)
        {
            if (comm.groupLeader != null && comm.groupLeader.groupMembers.Contains(comm))
                comm.groupLeader.groupMembers.Remove(comm);
            comm.groupLeader = null;
        }
        if (leaderTransform == null)
            return;

        leaderToFollow = leaderTransform;
        currentState = AIState.Following;
    }

    void Update()
    {
        switch (currentState)
        {
            case AIState.Wandering: HandleWandering(); break;
            case AIState.Scanning: HandleScanning(); break;
            case AIState.Chasing: HandleChasing(); break;
            case AIState.Following: HandleFollowing(); break;
        }

        boidsTimer -= Time.deltaTime;
        if (boidsTimer <= 0f)
        {
            cachedSteering = ComputeBoidsSteering();
            boidsTimer = 0.2f; // recalculate every 0.2 seconds
        }

        Vector3 steering = cachedSteering;
        if (steering.magnitude > maxSteer)
            steering = steering.normalized * maxSteer;

        agent.Move(steering * Time.deltaTime);

        if (drawDebugNeighbors)
            DebugDrawNeighbors();
    }

    // ============================================================
    //  STATE: WANDERING
    // ============================================================
    void HandleWandering()
    {
        agent.speed = normalSpeed;

        wanderTimer -= Time.deltaTime;
        if (!agent.pathPending && agent.remainingDistance <= 0.5f)
            wanderTimer = 0f;

        if (wanderTimer <= 0f)
        {
            Vector3 dest = RandomNavSphere(transform.position, wanderRadius);
            agent.SetDestination(dest);
            wanderTimer = wanderInterval;
        }

        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0f)
        {
            currentState = AIState.Scanning;
            scanTimer = scanInterval;
        }
    }

    // ============================================================
    //  STATE: SCANNING
    // ============================================================
    void HandleScanning()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= detectionRadius)
        {
            // *** AI spotted the player — call communication system ***
            if (!hasAlertedNearby)
            {
                hasAlertedNearby = true;

                // THIS AI becomes the leader if none exist
                GetComponent<AI_Communication>().SendAlert(player.position);
            }

            // If this AI also becomes leader → chasing logic picks it up
            currentState = AIState.Chasing;
            agent.speed = chaseSpeed;
            return;
        }

        currentState = AIState.Wandering;
    }

    // ============================================================
    //  STATE: CHASING
    // ============================================================
    void HandleChasing()
    {
        if (player != null)
        {
            float dist = Vector3.Distance(transform.position, player.position);

            if (dist > detectionRadius * loseSightMultiplier)
            {
                currentState = AIState.Wandering;
                agent.speed = normalSpeed;

                hasAlertedNearby = false;
                leaderToFollow = null;

                // Clear group membership
                var comm = GetComponent<AI_Communication>();
                if (comm != null)
                {
                    if (comm.groupLeader != null && comm.groupLeader.groupMembers.Contains(comm))
                        comm.groupLeader.groupMembers.Remove(comm);
                    comm.groupLeader = null;
                }
            }

            if (!hasAlertedNearby)
            {
                hasAlertedNearby = true;
                GetComponent<AI_Communication>().SendAlert(player.position);
            }

            // LEADER MODE — only the leader does real pathfinding
            agent.SetDestination(player.position);
        }
    }

    // ============================================================
    //  BOIDS STEERING
    // ============================================================
    Vector3 ComputeBoidsSteering()
    {
        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;

        int count = 0;

        foreach (AI_Communication other in AI_Communication.allAI)
        {
            if (other == null || other.gameObject == this.gameObject)
                continue;

            float d = Vector3.Distance(transform.position, other.transform.position);
            if (d <= neighborRadius)
            {
                // Separation
                Vector3 away = (transform.position - other.transform.position).normalized / d;
                separation += away;

                // Alignment
                NavMeshAgent oa = other.GetComponent<NavMeshAgent>();
                if (oa != null)
                    alignment += oa.velocity;

                // Cohesion
                cohesion += other.transform.position;

                count++;
            }
        }

        if (count == 0)
            return Vector3.zero;

        separation = separation.normalized * separationWeight;
        alignment = (alignment / count).normalized * alignmentWeight;
        cohesion = ((cohesion / count) - transform.position).normalized * cohesionWeight;

        return separation + alignment + cohesion;
    }

    // ============================================================
    //  UTILITY
    // ============================================================
    public static Vector3 RandomNavSphere(Vector3 origin, float dist)
    {
        Vector3 rand = Random.insideUnitSphere * dist + origin;
        NavMesh.SamplePosition(rand, out NavMeshHit navHit, dist, NavMesh.AllAreas);
        return navHit.position;
    }

    void DebugDrawNeighbors()
    {
        foreach (AI_Communication other in AI_Communication.allAI)
        {
            if (other == null) continue;
            Debug.DrawLine(transform.position, other.transform.position, Color.gray);
        }
    }
    // Fix for CS8641, CS1003, CS1525, CS1026, CS1002 errors in the HandleFollowing method.

    void HandleFollowing()
    {
        // Ensure the leader is valid
        if (leaderToFollow != null)
        {
            // Follow the leader
            Vector3 dir = (leaderToFollow.position - transform.position).normalized;
            agent.Move(dir * agent.speed * Time.deltaTime);
        }
        else
        {
            // If leader is lost, revert to wandering
            currentState = AIState.Wandering;

            hasAlertedNearby = false; // Reset alert status

            // Clear group membership
            var comm = GetComponent<AI_Communication>();
            if (comm != null)
            {
                if (comm.groupLeader != null && comm.groupLeader.groupMembers.Contains(comm))
                    comm.groupLeader.groupMembers.Remove(comm);
                comm.groupLeader = null;
            }
        }
    }
}
