using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class Hunting : MonoBehaviour
{
    public enum AIState { Wandering, Scanning, Chasing }
    public AIState currentState = AIState.Wandering;

    [Header("Movement Settings")]
    public float wanderRadius = 10f;
    public float wanderInterval = 5f;
    public float scanInterval = 2f;

    [Header("Chase Settings")]
    public float chaseSpeed = 4f;
    public float normalSpeed = 2f;
    public float detectionRadius = 15f;
    public float loseSightMultiplier = 2f; // how far before AI gives up

    [Header("Boids (Hybrid) Settings")]
    [Tooltip("How far the AI considers other AI as neighbors.")]
    public float neighborRadius = 5f;                // your requested 5 meters
    [Tooltip("Separation (avoid crowding) strength.")]
    public float separationWeight = 3f;              // strong influence
    [Tooltip("Alignment (match velocity) weight.")]
    public float alignmentWeight = 2.5f;
    [Tooltip("Cohesion (move toward group center) weight.")]
    public float cohesionWeight = 1.8f;
    [Tooltip("Maximum magnitude of combined steering force (units/sec).")]
    public float maxSteer = 6f;

    [Header("Detection / Communication")]
    [Tooltip("If this AI spots the player, alert other AI within this radius.")]
    public float alertRadius = 20f;

    [Header("Debug")]
    public bool drawDebugNeighbors = false;

    private NavMeshAgent agent;
    private Transform player;

    private float wanderTimer;
    private float scanTimer;
    private float forgetTimer;

    // Local state
    private Vector3 lastSteer = Vector3.zero;
    private bool hasAlertedNearby = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = normalSpeed;

        GameObject obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null)
            player = obj.transform;
        else
            Debug.LogError($"{name}: Player with tag 'Player' not found.");

        wanderTimer = wanderInterval;
        scanTimer = scanInterval;
        forgetTimer = 0f;
    }

    void Update()
    {
        // Basic state machine
        switch (currentState)
        {
            case AIState.Wandering: HandleWandering(); break;
            case AIState.Scanning: HandleScanning(); break;
            case AIState.Chasing: HandleChasing(); break;
        }

        // Always apply boids-style steering blended with agent movement
        Vector3 steering = ComputeBoidsSteering();

        // Cap steering
        if (steering.magnitude > maxSteer)
            steering = steering.normalized * maxSteer;

        // Apply steering as a displacement on the agent (in world space)
        agent.Move(steering * Time.deltaTime);

        // Optional debug draw
        if (drawDebugNeighbors)
            DebugDrawNeighbors();
    }

    // --------------------------
    // STATES
    // --------------------------
    void HandleWandering()
    {
        agent.speed = normalSpeed;

        wanderTimer -= Time.deltaTime;
        if (!agent.pathPending && agent.remainingDistance <= 0.5f)
            wanderTimer = 0f;

        if (wanderTimer <= 0f)
        {
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius);
            agent.SetDestination(newPos);
            wanderTimer = wanderInterval;
        }

        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0f)
        {
            currentState = AIState.Scanning;
            scanTimer = scanInterval;
        }
    }

    void HandleScanning()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= detectionRadius)
        {
            Debug.Log($"{name} spotted the player!");

            // Alert teammates once
            if (!hasAlertedNearby)
            {
                hasAlertedNearby = true;
                AI_Communication.AlertNearbyAI(transform.position, alertRadius);
                Debug.Log($"{name} → Alerted nearby AI (radius {alertRadius}).");
            }

            currentState = AIState.Chasing;
            agent.speed = chaseSpeed;
            forgetTimer = 0f;
            return;
        }

        // not found, go back to wander
        currentState = AIState.Wandering;
    }

    void HandleChasing()
    {
        if (player == null) return;

        agent.SetDestination(player.position);

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > detectionRadius * loseSightMultiplier)
        {
            // Give up after too far away from player
            currentState = AIState.Wandering;
            agent.speed = normalSpeed;
            hasAlertedNearby = false; // reset for next time
        }
    }

    // --------------------------
    // BOIDS (HYBRID) - separation, alignment, cohesion
    // --------------------------
    Vector3 ComputeBoidsSteering()
    {
        // Gather neighbors
        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        int count = 0;

        // Use AI_Communication.allAI as the group list (only AIs with component)
        foreach (AI_Communication other in AI_Communication.allAI)
        {
            if (other == null || other.gameObject == this.gameObject) continue;

            float d = Vector3.Distance(transform.position, other.transform.position);
            if (d <= neighborRadius && d > 0f)
            {
                // Separation: away from close neighbors (strongest when very close)
                Vector3 away = (transform.position - other.transform.position).normalized;
                separation += away / d; // inverse by distance (closer -> stronger)

                // Alignment: match agent velocity (use other agent's velocity if available)
                NavMeshAgent otherAgent = other.GetComponent<NavMeshAgent>();
                if (otherAgent != null)
                    alignment += otherAgent.velocity;

                // Cohesion: steer toward group's average position
                cohesion += other.transform.position;

                count++;
            }
        }

        if (count == 0)
            return Vector3.zero;

        // Finalize components
        separation = separation.normalized * separationWeight;
        alignment = (alignment / count).normalized * alignmentWeight;
        cohesion = ((cohesion / count) - transform.position).normalized * cohesionWeight;

        // Combine weights (strong separation influence as requested)
        Vector3 steering = separation + alignment + cohesion;

        // Slightly bias steering toward current agent forward to avoid abrupt perpendicular pushes
        // (blend with forward to help NavMesh path following)
        steering = Vector3.Lerp(Vector3.zero, steering, 1f); // you can tune lerp if you want less influence

        lastSteer = steering;
        return steering;
    }

    // --------------------------
    // External alert API (called by AI_Communication)
    // --------------------------
    public void ReceiveExternalAlert(Vector3 alertLocation)
    {
        Debug.Log($"{name} → Received communication alert (source: {alertLocation})");

        // Immediately switch to chasing and head to alert source (but still apply boids)
        currentState = AIState.Chasing;
        agent.speed = chaseSpeed;
        hasAlertedNearby = true;
        agent.SetDestination(alertLocation);
        forgetTimer = 0f;
    }

    // --------------------------
    // UTILS
    // --------------------------
    public static Vector3 RandomNavSphere(Vector3 origin, float dist)
    {
        Vector3 randDir = Random.insideUnitSphere * dist + origin;
        NavMesh.SamplePosition(randDir, out NavMeshHit navHit, dist, NavMesh.AllAreas);
        return navHit.position;
    }

    void DebugDrawNeighbors()
    {
        // draw neighbor radius
        Debug.DrawLine(transform.position, transform.position + Vector3.up * 0.2f, Color.yellow);
        foreach (AI_Communication other in AI_Communication.allAI)
        {
            if (other == null) continue;
            Debug.DrawLine(transform.position, other.transform.position, Color.gray);
            float d = Vector3.Distance(transform.position, other.transform.position);
            if (d < neighborRadius)
                Debug.DrawRay(transform.position, (other.transform.position - transform.position).normalized * d, Color.cyan);
        }
    }
}
