// ============================================================
//  HapticExample.cs
//  Example: how to trigger haptics from any other script.
//  Attach to a collider or XR interactable.
// ============================================================

using UnityEngine;

public class HapticExample : MonoBehaviour
{
    [Header("Motor Mapping")]
    [Tooltip("Which motor index this object maps to (0–7)")]
    public int motorIndex = 0;

    [Range(0, 255)]
    public int intensity = 200;

    [Tooltip("Pulse duration in ms (0 = hold)")]
    public int pulseDurationMs = 150;

    // ── Collision-based trigger ──────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        HapticUDPController.Instance.Pulse(motorIndex, pulseDurationMs, intensity);
    }


    // ── Manual test from Inspector (context menu) ────────────
    [ContextMenu("Test Pulse")]
    void TestPulse()
    {
        HapticUDPController.Instance.Pulse(motorIndex, pulseDurationMs, intensity);
    }

    [ContextMenu("Stop Motor")]
    void TestStop()
    {
        HapticUDPController.Instance.StopMotor(motorIndex);
    }
}
