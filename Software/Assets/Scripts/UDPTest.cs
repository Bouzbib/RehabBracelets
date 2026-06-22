using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UDPTest : MonoBehaviour
{

    [Header("Motor Mapping")]
    [Tooltip("Which motor index this object maps to (0–3)")]
    public int motorIndex = 0;

    [Range(0, 255)]
    public int intensity = 255;

    [Tooltip("Pulse duration in ms (0 = hold)")]
    public int pulseDurationMs = 500;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
	    {
	        LaunchVibration();
	    }
    }

    public void LaunchVibration()
    {
    	HapticUDPController.Instance.Pulse(motorIndex, pulseDurationMs, intensity);
	    Debug.Log("[Test] Motor " + motorIndex + " for " + pulseDurationMs + " ms at intensity " + intensity);
    }
}
