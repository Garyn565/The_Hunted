using UnityEngine;
using System.Collections.Generic;

public class AI_Communication : MonoBehaviour
{
    public static List<AI_Communication> allAI = new List<AI_Communication>();

    [Header("Communication Settings")]
    public float alertRadius = 25f;
    public int maxGroupSize = 7; // Maximum AI per group

    private Hunting hunting;

    // Group management
    public AI_Communication groupLeader = null;
    public List<AI_Communication> groupMembers = new List<AI_Communication>();

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

        // Remove from group if needed
        if (groupLeader != null && groupLeader.groupMembers.Contains(this))
            groupLeader.groupMembers.Remove(this);

        // If this was a leader, clear group
        if (groupLeader == this)
        {
            foreach (var member in groupMembers)
            {
                if (member != null)
                    member.groupLeader = null;
            }
            groupMembers.Clear();
        }
    }

    // ============================================================
    // PUBLIC: Trigger Alert (usually called from Hunting)
    // ============================================================
    public void SendAlert(Vector3 enemyPos)
    {
        AlertNearbyAI(this, enemyPos, alertRadius, maxGroupSize);
    }

    // ============================================================
    // STATIC: Alert all nearby AI and form a group
    // ============================================================
    public static void AlertNearbyAI(AI_Communication spotter, Vector3 alertPos, float radius, int maxGroup)
    {
        // Find nearby AI (including self)
        List<AI_Communication> group = new List<AI_Communication>();
        foreach (AI_Communication ai in allAI)
        {
            if (ai == null || ai.groupLeader != null) continue; // Already in a group

            float dist = Vector3.Distance(ai.transform.position, alertPos);
            if (dist <= radius)
                group.Add(ai);

            if (group.Count >= maxGroup)
                break;
        }

        // Assign leader and group
        foreach (var ai in group)
        {
            ai.groupLeader = spotter;
            spotter.groupMembers.Add(ai);
        }

        // Leader does pathfinding, followers follow leader
        foreach (var ai in group)
        {
            ai.ReceiveAlert(alertPos, spotter);
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
            // Followers use boid-style following
            hunting.BeginFollowerHunt(leader.transform);
        }
    }
}
