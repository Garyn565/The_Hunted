using UnityEngine;
using UnityEngine.AI;

public class Hunting : MonoBehaviour
{
    [Header("Detection Settings")]
    public float scanRadius = 15f;
    [Range(1, 180)] public float fieldOfView = 90f;
    public float eyeHeight = 1.6f;
    public int rayCount = 7;
    public LayerMask detectionMask;

    [Header("Lock-On Settings")]
    public float lockOnTime = 3f;          // Time needed to lock on
    public float memoryTime = 1.5f;        // Forgive brief visibility loss
    private float visibleTimer = 0f;
    private float invisibleTimer = 0f;

    [Header("Wander Settings")]
    public float wanderRadius = 10f;
    public float wanderInterval = 3f;

    [Header("Chase Settings")]
    public float chaseSpeed = 5f;
    public float wanderSpeed = 2f;
    public float chaseForgetTime = 5f;

    private float forgetTimer = 0f;
    private float wanderTimer = 0f;

    private NavMeshAgent agent;
    private Transform player;

    private enum AIState { Searching, LockingOn, Chasing }
    private AIState currentState = AIState.Searching;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        GameObject obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null)
            player = obj.transform;
        else
            Debug.LogWarning($"{name} could not find Player tagged object.");
    }

    void Update()
    {
        if (player == null) return;

        switch (currentState)
        {
            case AIState.Searching:
                HandleSearching();
                break;
            case AIState.LockingOn:
                HandleLockingOn();
                break;
            case AIState.Chasing:
                HandleChasing();
                break;
        }
    }

    // ------------------------------------------------------------
    // SEARCH STATE
    // ------------------------------------------------------------
    void HandleSearching()
    {
        agent.speed = wanderSpeed;

        wanderTimer += Time.deltaTime;
        if (wanderTimer >= wanderInterval)
        {
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius);
            agent.SetDestination(newPos);
            wanderTimer = 0f;
        }

        if (PlayerInSight())
        {
            Debug.Log($"{name} spotted Player! Starting lock-on...");
            currentState = AIState.LockingOn;
            visibleTimer = 0f;
            invisibleTimer = 0f;
        }
    }

    // ------------------------------------------------------------
    // LOCK-ON STATE
    // ------------------------------------------------------------
    void HandleLockingOn()
    {
        if (PlayerInSight())
        {
            visibleTimer += Time.deltaTime;
            invisibleTimer = 0f;

            if (visibleTimer >= lockOnTime)
            {
                Debug.Log($"{name} locked on to Player → switching to CHASE!");
                currentState = AIState.Chasing;
                agent.speed = chaseSpeed;
            }
        }
        else
        {
            invisibleTimer += Time.deltaTime;

            // brief lapses forgiven
            if (invisibleTimer >= memoryTime)
            {
                Debug.Log($"{name} lost sight for too long → back to SEARCH.");
                currentState = AIState.Searching;
                visibleTimer = 0f;
                invisibleTimer = 0f;
            }
        }
    }

    // ------------------------------------------------------------
    // CHASE STATE
    // ------------------------------------------------------------
    void HandleChasing()
    {
        agent.SetDestination(player.position);

        if (PlayerInSight())
        {
            forgetTimer = 0f;
        }
        else
        {
            forgetTimer += Time.deltaTime;
            if (forgetTimer >= chaseForgetTime)
            {
                Debug.Log($"{name} lost Player completely → back to SEARCH.");
                currentState = AIState.Searching;
                forgetTimer = 0f;
            }
        }
    }

    // ------------------------------------------------------------
    // DETECTION
    // ------------------------------------------------------------
    bool PlayerInSight()
    {
        Vector3 toPlayer = (player.position - transform.position).normalized;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > fieldOfView / 2f) return false;

        if (Vector3.Distance(transform.position, player.position) > scanRadius)
            return false;

        return MultiRayPlayerCheck();
    }

    bool MultiRayPlayerCheck()
    {
        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        float angleStep = fieldOfView / (rayCount - 1);

        for (int i = 0; i < rayCount; i++)
        {
            float angleOffset = -fieldOfView / 2f + angleStep * i;
            Vector3 rayDir = Quaternion.Euler(0, angleOffset, 0) * transform.forward;

            if (Physics.Raycast(eyePos, rayDir, out RaycastHit hit, scanRadius, detectionMask))
            {
                Debug.DrawRay(eyePos, rayDir * scanRadius,
                    hit.collider.CompareTag("Player") ? Color.green : Color.red);

                if (hit.collider.CompareTag("Player"))
                    return true;
            }
            else
            {
                Debug.DrawRay(eyePos, rayDir * scanRadius, Color.red);
            }
        }
        return false;
    }

    // ------------------------------------------------------------
    // UTILITIES
    // ------------------------------------------------------------
    public static Vector3 RandomNavSphere(Vector3 origin, float dist)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist + origin;
        NavMesh.SamplePosition(randDirection, out NavMeshHit navHit, dist, NavMesh.AllAreas);
        return navHit.position;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, scanRadius);

        Vector3 left = Quaternion.Euler(0, -fieldOfView / 2f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, fieldOfView / 2f, 0) * transform.forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position + Vector3.up * eyeHeight, left * scanRadius);
        Gizmos.DrawRay(transform.position + Vector3.up * eyeHeight, right * scanRadius);
    }
}
