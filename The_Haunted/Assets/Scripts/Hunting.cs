using UnityEngine;
using UnityEngine.AI;

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

    private NavMeshAgent agent;
    private Transform player;

    private float wanderTimer;
    private float scanTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = normalSpeed;

        player = GameObject.FindGameObjectWithTag("Player").transform;

        wanderTimer = wanderInterval;
        scanTimer = scanInterval;
    }

    void Update()
    {
        switch (currentState)
        {
            case AIState.Wandering:
                HandleWandering();
                break;

            case AIState.Scanning:
                HandleScanning();
                break;

            case AIState.Chasing:
                HandleChasing();
                break;
        }
    }

    // ===========================================================
    // WANDERING
    // ===========================================================
    void HandleWandering()
    {
        wanderTimer -= Time.deltaTime;

        if (!agent.pathPending && agent.remainingDistance <= 0.5f)
        {
            wanderTimer = 0;
        }

        if (wanderTimer <= 0)
        {
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius);
            agent.SetDestination(newPos);
            wanderTimer = wanderInterval;
        }

        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0)
        {
            currentState = AIState.Scanning;
            scanTimer = scanInterval;
        }
    }

    // ===========================================================
    // SCANNING
    // ===========================================================
    void HandleScanning()
    {
        // Check distance to player
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= detectionRadius)
        {
            Debug.Log($"{name} spotted the player!");

            // ALERT ALL OTHER AI
            AI_Communication.AlertNearbyAI(transform.position, GetComponent<AI_Communication>().alertRadius);

            currentState = AIState.Chasing;
            agent.speed = chaseSpeed;
            return;
        }

        currentState = AIState.Wandering;
    }

    // ===========================================================
    // CHASING
    // ===========================================================
    void HandleChasing()
    {
        if (player == null)
            return;

        agent.SetDestination(player.position);

        float dist = Vector3.Distance(transform.position, player.position);

        // Lost the player?
        if (dist > detectionRadius * 2)
        {
            agent.speed = normalSpeed;
            currentState = AIState.Wandering;
        }
    }

    // ===========================================================
    // COMMUNICATION — GET ALERTED BY OTHER AI
    // ===========================================================
    public void ReceiveExternalAlert(Vector3 alertLocation)
    {
        Debug.Log($"{name} → Received communication alert!");

        currentState = AIState.Chasing;
        agent.speed = chaseSpeed;
        agent.SetDestination(alertLocation);
    }

    // ===========================================================
    // UTILS — RANDOM NAV POSITION
    // ===========================================================
    public static Vector3 RandomNavSphere(Vector3 origin, float dist)
    {
        Vector3 randDir = Random.insideUnitSphere * dist + origin;
        NavMesh.SamplePosition(randDir, out NavMeshHit navHit, dist, NavMesh.AllAreas);
        return navHit.position;
    }
}
