// ============================================================
//  HapticUDPController.cs
//  - Works on both Windows (Unity Editor) and Meta Quest (Android)
//  - Finds the correct local IP via NetworkInterface
//  - Thread-safe main-thread dispatch (no UnityMainThreadDispatcher needed)
// ============================================================

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class HapticUDPController : MonoBehaviour
{
    public static HapticUDPController Instance { get; private set; }

    [Header("ESP32 Address")]
    public string espIP   = "192.168.4.100";
    public int    espPort = 5005;
    public int    ackPort = 5006;

    [Header("Network")]
    [Tooltip("Subnet prefix of your hotspot — same on PC and Quest")]
    public string subnetPrefix = "192.168.4.";

    [Header("Status indicator (optional)")]
    public Image statusLight;
    public float timeoutSeconds = 5f;

    public event Action<float, float, float> OnCalibrationReceived;
    public event Action OnESPReady;

    private UdpClient  _sender;
    private UdpClient  _receiver;
    private Thread     _receiveThread;
    private IPEndPoint _espEndPoint;

    private float _lastAckTime      = -999f;
    private bool  _isConnected      = false;
    private float _lastReconnectTime = -999f;
    private const float ReconnectInterval = 3f;

    // Thread-safe queue — replaces UnityMainThreadDispatcher
    private readonly Queue<Action> _mainThreadQueue = new Queue<Action>();
    private readonly object        _queueLock       = new object();

    // ═════════════════════════════════════════════════════════
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        string localIP = GetLocalIP();
        Debug.Log($"[Haptics] Local interface: {localIP}");

        _espEndPoint = new IPEndPoint(IPAddress.Parse(espIP), espPort);
        _sender      = new UdpClient(new IPEndPoint(IPAddress.Parse(localIP), 0));

        StartReceiver(localIP);
        SetStatusLight(false);

        SendCommand("CONNECT");
        Debug.Log($"[Haptics] Sent CONNECT to {espIP}:{espPort}");
    }

    void StartReceiver(string localIP = null)
    {
        try {
            _receiver?.Close();
            localIP = localIP ?? GetLocalIP();

            var socket = new Socket(AddressFamily.InterNetwork,
                                    SocketType.Dgram, ProtocolType.Udp);
            socket.ExclusiveAddressUse = false;
            socket.SetSocketOption(SocketOptionLevel.Socket,
                                   SocketOptionName.ReuseAddress, true);
            socket.Bind(new IPEndPoint(IPAddress.Parse(localIP), ackPort));
            _receiver = new UdpClient { Client = socket };

            _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            _receiveThread.Start();
            Debug.Log($"[Haptics] Listening on {localIP}:{ackPort}");
        } catch (Exception e) {
            Debug.LogWarning($"[Haptics] Could not start receiver: {e.Message}");
        }
    }

    void Update()
    {
        // Drain the main-thread queue
        lock (_queueLock) {
            while (_mainThreadQueue.Count > 0)
                _mainThreadQueue.Dequeue()?.Invoke();
        }

        bool connected = (Time.time - _lastAckTime) < timeoutSeconds;
        if (connected != _isConnected) {
            _isConnected = connected;
            SetStatusLight(connected);
            Debug.Log($"[Haptics] {(connected ? "CONNECTED" : "NO SIGNAL")}");
        }

        if (!_isConnected && Time.time - _lastReconnectTime > ReconnectInterval) {
            _lastReconnectTime = Time.time;
            if (_receiveThread == null || !_receiveThread.IsAlive) {
                Debug.Log("[Haptics] Receive thread dead — restarting");
                StartReceiver();
            }
            SendCommand("CONNECT");
        }
    }

    void OnDestroy()
    {
        StopAllMotors();
        _sender?.Close();
        _receiver?.Close();
        _receiver = null;
    }

    // ═════════════════════════════════════════════════════════
    //  Receive loop (background thread)
    // ═════════════════════════════════════════════════════════
    private void ReceiveLoop()
    {
        IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
        while (_receiver != null)
        {
            try {
                byte[] data = _receiver.Receive(ref remote);
                string msg  = Encoding.UTF8.GetString(data).Trim();

                Enqueue(() => {
                    _lastAckTime = Time.time;
                    if (msg == "READY") {
                        Debug.Log("[Haptics] ESP is READY");
                        OnESPReady?.Invoke();
                    } else if (msg.StartsWith("CAL:")) {
                        ParseCalibration(msg);
                    } else if (msg.StartsWith("ACK:")) {
                        Debug.Log($"[Haptics] ACK: {msg}");
                    } else {
                        Debug.Log($"[Haptics] Reply: {msg}");
                    }
                });
            } catch (Exception e) {
                if (_receiver == null) break;
                Debug.LogWarning($"[Haptics] Receive error: {e.Message}");
                break;
            }
        }
        Debug.Log("[Haptics] Receive thread exited");
    }

    private void ParseCalibration(string msg)
    {
        string[] p = msg.Split(':');
        if (p.Length < 4) return;
        if (!TryParseFloat(p[1], out float ax)) return;
        if (!TryParseFloat(p[2], out float ay)) return;
        if (!TryParseFloat(p[3], out float az)) return;
        Debug.Log($"[Haptics] Calibration: ax={ax:F3} ay={ay:F3} az={az:F3}");
        OnCalibrationReceived?.Invoke(ax, ay, az);
    }

    // ═════════════════════════════════════════════════════════
    //  Public API
    // ═════════════════════════════════════════════════════════
    public void TriggerMotor(int motorIndex, int intensity = 255, int durationMs = 0)
        => SendCommand($"MOTOR:{Mathf.Clamp(motorIndex,0,7)}:{Mathf.Clamp(intensity,0,255)}:{durationMs}");

    public void Pulse(int motorIndex, int durationMs, int intensity = 255)
        => TriggerMotor(motorIndex, intensity, durationMs);

    public void StopMotor(int motorIndex)
        => TriggerMotor(motorIndex, 0, 0);

    public void StopAllMotors()
        => SendCommand("ALLOFF");

    public void SendCommand(string cmd)
    {
        try {
            byte[] data = Encoding.UTF8.GetBytes(cmd);
            _sender.Send(data, data.Length, _espEndPoint);
        } catch (Exception e) {
            Debug.LogWarning($"[Haptics] Send failed: {e.Message}");
        }
    }

    // ═════════════════════════════════════════════════════════
    //  Helpers
    // ═════════════════════════════════════════════════════════
    void Enqueue(Action a) { lock (_queueLock) _mainThreadQueue.Enqueue(a); }

    string GetLocalIP()
    {
        // NetworkInterface works reliably on both Windows and Android/Quest
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up) continue;
            foreach (var addr in nic.GetIPProperties().UnicastAddresses)
            {
                string ip = addr.Address.ToString();
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork
                    && ip.StartsWith(subnetPrefix))
                    return ip;
            }
        }
        Debug.LogWarning($"[Haptics] No IP with prefix '{subnetPrefix}' — using 0.0.0.0");
        return "0.0.0.0";
    }

    static bool TryParseFloat(string s, out float r)
        => float.TryParse(s, System.Globalization.NumberStyles.Float,
                          System.Globalization.CultureInfo.InvariantCulture, out r);

    void SetStatusLight(bool on)
    {
        if (statusLight == null) return;
        statusLight.color = on ? new Color(0.2f, 0.9f, 0.3f)
                               : new Color(0.9f, 0.2f, 0.2f);
    }
}