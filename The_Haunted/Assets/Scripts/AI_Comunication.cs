using UnityEngine;
using System.Collections.Generic;

public class AI_Communication : MonoBehaviour
{
    // List of all AI units
    public static List<AI_Communication> allAI = new List<AI_Communication>();

    // The current squad leader (only one does the heavy pathfinding)
    public static AI_Communication currentLeader = null;

    [Header("Communication Settings")]
    public float alertRadius = 25f;

    private Hunting hunting;

    private void Awake()
    {
        hunting = GetComponent<Hunting>();
        if (hunting == null)
            Debug.LogError($"{name} has AI_Communication but no Hunting script!");
    }

    private void OnEnable()
    {
        if (!allAI.Contains(this))
            allAI.Add(this);
    }

    private void OnDisable()
    {
        allAI.Remove(this);

        // If the leader is disabled/dies → clear leadership
        if (currentLeader == this)
            currentLeader = null;
    }

    // ============================================================
    // PUBLIC: Trigger Alert (usually called from Hunting)
    // ============================================================
    public void SendAlert(Vector3 enemyPos)
    {
        AlertNearbyAI(this, enemyPos, alertRadius);
    }

    // ============================================================
    // STATIC: Alert all nearby AI
    // ============================================================
    public static void AlertNearbyAI(AI_Communication spotter, Vector3 alertPos, float radius)
    {
        // Assign leader if none exists
        if (currentLeader == null)
            currentLeader = spotter;

        foreach (AI_Communication ai in allAI)
        {
            if (ai == null) continue;

            float dist = Vector3.Distance(ai.transform.position, alertPos);
            if (dist <= radius)
                ai.ReceiveAlert(alertPos, currentLeader);
        }
    }

    // ============================================================
    // RECEIVING ALERT
    // ============================================================
    public void ReceiveAlert(Vector3 enemyPos, AI_Communication leader)
    {
        if (hunting == null)
            return;

        bool isLeader = (this == leader);

        if (isLeader)
        {
            // Leader does pathfinding
            hunting.BeginLeaderHunt(enemyPos);
        }
        else
        {
            // Followers just track the leader with no pathfinding
            hunting.BeginFollowerHunt(leader.transform);
        }
    }
}
