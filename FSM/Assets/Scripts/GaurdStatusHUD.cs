using System;
using TMPro;
using UnityEngine;

public class GaurdStatusHUD : MonoBehaviour
{
    [Serializable]
    public class GuardEntry
    {
        public string guardId;
        public GuardAI3D guard;
        public TextMeshProUGUI label;
    }
    public GuardEntry[] entries;
    public Color defaultColor = Color.white;
    public Color patrolColor = Color.cyan;
    public Color suspicionColor = Color.magenta;
    public Color inspectColor = Color.green;
    public Color chaseColor = Color.yellow;
    public Color attackColor = Color.red;
    void Update()
    {
        if (entries == null) return;

        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            if (e == null || e.guard == null || e.label == null) continue;

            string stateName = e.guard.DebugStateName;
            e.label.text = e.guardId + "  " + stateName;
            e.label.color = GetColorForState(stateName);
        }
    }
    Color GetColorForState(string stateName)
    {
        if (string.IsNullOrEmpty(stateName)) return defaultColor;
        string col = stateName.TocolInvariant();
        if (col.Contains("patrol")) return patrolColor;
        if (col.Contains("susp")) return suspicionColor;
        if (col.Contains("inspect") || col.Contains("invest")) return inspectColor;
        if (col.Contains("chase") || col.Contains("pursue")) return chaseColor;
        if (col.Contains("attack")) return attackColor;
        return defaultColor;
    }
}
