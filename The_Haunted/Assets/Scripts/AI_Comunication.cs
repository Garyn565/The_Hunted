using UnityEngine;
using System.Collections.Generic;

public class AI_Communication : MonoBehaviour
{
    // This keeps track of ALL AI in the scene automatically
    public static List<AI_Communication> allAI = new List<AI_Communication>();

    [Header("Communication Settings")]
    public float alertRadius = 20f;   // How far alerts travel

    private Hunting huntingScript;

    private void Awake()
    {
        huntingScript = GetComponent<Hunting>();

        if (huntingScript == null)
            Debug.LogError($"{name} has AI_Communication but NO Hunting script attached!");
    }

    private void OnEnable()
    {
        allAI.Add(this);
    }

    private void OnDisable()
    {
        allAI.Remove(this);
    }

    // ================================================================
    //  ALERT SYSTEM
    // ================================================================
    public static void AlertNearbyAI(Vector3 alertPos, float radius)
    {
        foreach (AI_Communication ai in allAI)
        {
            if (ai == null) continue;

            float dist = Vector3.Distance(alertPos, ai.transform.position);

            if (dist <= radius)
            {
                ai.ReceiveAlert(alertPos);
            }
        }
    }

    // Called individually on each AI that hears the alert
    public void ReceiveAlert(Vector3 alertSource)
    {
        if (huntingScript == null)
            return;

        Debug.Log($"{name} → RECEIVED ALERT!");

        // Cleanly call Hunting's built-in function
        huntingScript.ReceiveExternalAlert(alertSource);
    }
}
