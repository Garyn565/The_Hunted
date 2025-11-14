using UnityEngine;
using System.Collections.Generic;

public class AI_Communication : MonoBehaviour
{
    public static List<AI_Communication> allAI = new List<AI_Communication>();

    [Header("Communication Settings")]
    public float alertRadius = 20f;

    [HideInInspector]
    public Hunting huntingScript;

    private void Awake()
    {
        huntingScript = GetComponent<Hunting>();

        if (huntingScript == null)
            Debug.LogError($"{name} has AI_Communication but NO Hunting script attached!");
    }

    private void OnEnable()
    {
        if (!allAI.Contains(this))
            allAI.Add(this);
    }

    private void OnDisable()
    {
        if (allAI.Contains(this))
            allAI.Remove(this);
    }

    // ===========================================================
    // BROADCAST ALERT TO NEARBY AI
    // ===========================================================
    public static void AlertNearbyAI(Vector3 alertPosition, float radius)
    {
        foreach (AI_Communication ai in allAI)
        {
            float dist = Vector3.Distance(alertPosition, ai.transform.position);

            if (dist <= radius)
            {
                ai.ReceiveAlert(alertPosition);
            }
        }
    }

    // ===========================================================
    // RECEIVE ALERT FROM ANOTHER AI
    // ===========================================================
    public void ReceiveAlert(Vector3 alertSource)
    {
        if (huntingScript == null)
            return;

        Debug.Log($"{name} → ALERT RECEIVED from another AI!");

        huntingScript.ReceiveExternalAlert(alertSource);
    }
}
