// ============================================================
//  HapticUDPController.cs
//  - Direct connection to ESP (no discovery)
//  - ACK listener on port 5006
//  - Connection status indicator (UI Image color)
//  - Space key test in editor
// ============================================================

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class HapticUDPControllerCmd : MonoBehaviour
{
    public static HapticUDPControllerCmd Instance { get; private set; }

    [Header("ESP32 Address")]
    public string espIP   = "192.168.4.100";
    public int    espPort = 5005;
    public int    ackPort = 5006;

    [Header("Status indicator (optional)")]
    [Tooltip("Assign any UI Image — goes green when ACK received, red when stale")]
    public Image statusLight;

    // How many seconds without an ACK before we consider connection lost
    public float timeoutSeconds = 5f;

    // ── Private ──────────────────────────────────────────────
    private UdpClient  _sender;
    private UdpClient  _receiver;
    private Thread     _receiveThread;
    private IPEndPoint _espEndPoint;

    private float  _lastAckTime  = -999f;
    private bool   _isConnected  = false;

    private const byte CMD_MOTOR   = 0x01;
    private const byte CMD_ALL_OFF = 0x02;

    // ── Lifecycle ────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        _espEndPoint = new IPEndPoint(IPAddress.Parse(espIP), espPort);
        _sender      = new UdpClient();
        Debug.Log($"[Haptics] Sending to {espIP}:{espPort}");

        try {
            _receiver      = new UdpClient(ackPort);
            _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            _receiveThread.Start();
            Debug.Log($"[Haptics] Listening for ACKs on port {ackPort}");
        } catch (Exception e) {
            Debug.LogWarning($"[Haptics] ACK listener failed: {e.Message}");
        }

        SetStatusLight(false);
    }

    void Update()
    {
        // Space = test pulse on motor 0
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("[Haptics] Test pulse → motor 0");
            Pulse(0, 300, 255);
        }

        // Update connection status based on last ACK time
        bool connected = (Time.time - _lastAckTime) < timeoutSeconds;
        if (connected != _isConnected) {
            _isConnected = connected;
            SetStatusLight(connected);
            Debug.Log($"[Haptics] Status: {(connected ? "CONNECTED" : "NO SIGNAL")}");
        }
    }

    void OnDestroy()
    {
        StopAllMotors();
        _sender?.Close();
        _receiver?.Close();
        _receiver = null;
    }

    // ── Status light ─────────────────────────────────────────
    void SetStatusLight(bool connected)
    {
        if (statusLight == null) return;
        statusLight.color = connected
            ? new Color(0.2f, 0.9f, 0.3f)   // green
            : new Color(0.9f, 0.2f, 0.2f);  // red
    }

    // ── ACK receiver ─────────────────────────────────────────
    private void ReceiveLoop()
    {
        IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
        while (_receiver != null)
        {
            try {
                byte[] data = _receiver.Receive(ref remote);
                string msg  = Encoding.UTF8.GetString(data);
                float  now  = Time.time; // safe to read from thread

                UnityMainThreadDispatcher.Enqueue(() => {
                    _lastAckTime = Time.time;
                    Debug.Log($"[Haptics] ACK: {msg}");
                });
            } catch (Exception e) {
                if (_receiver == null) break;
                Debug.LogWarning($"[Haptics] Receive error: {e.Message}");
            }
        }
    }

    // ── Public API ───────────────────────────────────────────

    /// <summary>Trigger a motor. durationMs=0 holds until stopped manually.</summary>
    public void TriggerMotor(int motorIndex, int intensity = 255, int durationMs = 0)
    {
        if (motorIndex < 0 || motorIndex > 7) return;
        byte hi = (byte)((durationMs >> 8) & 0xFF);
        byte lo = (byte)(durationMs & 0xFF);
        Send(new byte[] {
            CMD_MOTOR,
            (byte)Mathf.Clamp(motorIndex, 0, 7),
            (byte)Mathf.Clamp(intensity,  0, 255),
            hi, lo
        });
    }

    /// <summary>Fire a motor for a fixed duration.</summary>
    public void Pulse(int motorIndex, int durationMs, int intensity = 255)
        => TriggerMotor(motorIndex, intensity, durationMs);

    /// <summary>Stop one motor.</summary>
    public void StopMotor(int motorIndex)
        => TriggerMotor(motorIndex, 0, 0);

    /// <summary>Stop all motors immediately.</summary>
    public void StopAllMotors()
        => Send(new byte[] { CMD_ALL_OFF });

    private void Send(byte[] data)
    {
        try {
            _sender.Send(data, data.Length, _espEndPoint);
        } catch (Exception e) {
            Debug.LogWarning($"[Haptics] Send failed: {e.Message}");
        }
    }
}
