using System.Diagnostics;
using TMPro;
using UnityEngine;

public class GuardStatusHUD : MonoBehaviour
{
    [System.Serializable]
    public class GuardEntry
    {
        public string guardId;
        public GaurdAI3D guard;
        public TextMeshProUGUI label;
    }

    public GuardEntry[] entries;
    public Color defaultColor = Color.white;
    public Color patrolColor = Color.cyan;
    public Color chaseColor = Color.yellow;
    public Color attackColor = Color.red;

    void Awake()
    {
        Debug.Log("[GuardStatusHUD3D] Awake, entries count=" + (entries == null ? 0 : entries.Length));
    }

    void OnEnable()
    {
        if (entries == null)
        {
            Debug.LogWarning("[GuardStatusHUD3D] Entries array is null.");
            return;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            if (e.guard == null)
            {
                Debug.LogWarning("[GuardStatusHUD3D] Missing guard reference at index " + i);
            }
            if (e.label == null)
            {
                Debug.LogWarning("[GuardStatusHUD3D] Missing label reference at index " + i);
            }
        }
    }

    void Update()
    {
        if (entries == null) return;

        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            if (e.guard == null || e.label == null) continue;

            string stateName = e.guard.DebugStateName;
            e.label.text = e.guardId + "  " + stateName;
            e.label.color = GetColorForState(stateName);
        }
    }

    Color GetColorForState(string stateName)
    {
        if (string.IsNullOrEmpty(stateName))
            return defaultColor;

        if (stateName.Contains("Patrol"))
            return patrolColor;

        if (stateName.Contains("Chase"))
            return chaseColor;

        if (stateName.Contains("Attack"))
            return attackColor;

        return defaultColor;
    }
}
