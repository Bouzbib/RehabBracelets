// ============================================================
//  IMUReceiver.cs
//  - Works on both Windows (Unity Editor) and Meta Quest (Android)
//  - Uses NetworkInterface to find the correct local IP
//  - No UnityMainThreadDispatcher dependency
// ============================================================

using System;
using System.Collections;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IMUReceiver : MonoBehaviour
{
    [Serializable]
    public enum ArmID {Left, Right};
    public ArmID armID;

    [Header("Recording")]
    public float timeOn = 5.0f;
    [Header("Network")]
    public int    imuPort      = 5007;
    [Tooltip("Must match subnetPrefix in HapticUDPController")]
    public string subnetPrefix = "192.168.4.";

    [Header("Visualization")]
    public Transform wristVisual;

    [Header("UI")]
    public TextMeshProUGUI rawDataText;
    // public TextMeshProUGUI handLabel;
    // public TextMeshProUGUI positionLabel;
    public Button          initButton;
    public Button          calibrateButton;

    [Header("Smoothing")]
    public float smoothing = 8f;

    public float _ax, _ay, _az, _gx, _gy, _gz;
    private bool  _newData = false;
    private readonly object _lock = new object();

    private bool  _calibrated = false;
    private float _calAx, _calAy, _calAz;

    private UdpClient  _receiver;
    private Thread     _receiveThread;
    private Quaternion _smoothRot = Quaternion.identity;
    private Coroutine  _imuWindowCoroutine;
    private volatile bool _imuWindowActive = false;

    public CalibrateHandPosition calibratingPosition;

    // ═════════════════════════════════════════════════════════
    void Start()
    {
        if(this.armID == ArmID.Right)
        {
            this.imuPort = 5007;
        }
        else
        {
            this.imuPort = 5009;
        }

        calibratingPosition = this.GetComponent<CalibrateHandPosition>();

        string localIP = GetLocalIP();
        Debug.Log($"[IMU] Binding to {localIP}:{imuPort}");

        try {
            var socket = new Socket(AddressFamily.InterNetwork,
                                    SocketType.Dgram, ProtocolType.Udp);
            socket.ExclusiveAddressUse = false;
            socket.SetSocketOption(SocketOptionLevel.Socket,
                                   SocketOptionName.ReuseAddress, true);
            socket.Bind(new IPEndPoint(IPAddress.Parse(localIP), imuPort));
            _receiver = new UdpClient { Client = socket };

            _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            _receiveThread.Start();
            Debug.Log($"[IMU] Listening on {localIP}:{imuPort}");
        } catch (Exception e) {
            Debug.LogWarning($"[IMU] Could not bind port {imuPort}: {e.Message}");
        }

        if (calibrateButton != null)
            calibrateButton.onClick.AddListener(SendCalibrate);

        if (initButton != null)
            initButton.onClick.AddListener(SendInitWire);


        if (HapticUDPController.Instance != null)
            HapticUDPController.Instance.OnCalibrationReceived += OnCalibrationReceived;
        else
            Debug.LogWarning("[IMU] HapticUDPController not found — start order issue?");
    }

    void Update()
    {
        bool hasNew;
        float ax, ay, az, gx, gy, gz;
        lock (_lock) {
            hasNew   = _newData;
            ax = _ax; ay = _ay; az = _az;
            gx = _gx; gy = _gy; gz = _gz;
            _newData = false;
        }

        if (!hasNew || !_imuWindowActive) return;

        if (rawDataText != null)
            rawDataText.text =
                $"Accel  X:{ax:+0.000}  Y:{ay:+0.000}  Z:{az:+0.000} g\n" +
                $"Gyro   X:{gx:+0.0}  Y:{gy:+0.0}  Z:{gz:+0.0} °/s";

        if (wristVisual != null) {
            Quaternion target = Quaternion.FromToRotation(Vector3.up,
                                    new Vector3(ax, ay, az).normalized);
            _smoothRot       = Quaternion.Slerp(_smoothRot, target, Time.deltaTime * smoothing);
            wristVisual.rotation = _smoothRot;
        }
    }

    void OnDestroy()
    {
        _receiver?.Close();
        _receiver = null;
        if (HapticUDPController.Instance != null)
            HapticUDPController.Instance.OnCalibrationReceived -= OnCalibrationReceived;
    }

    // ═════════════════════════════════════════════════════════
    //  IMU window
    // ═════════════════════════════════════════════════════════
    public void RequestIMUWindow(float seconds)
    {
        if (_imuWindowCoroutine != null) StopCoroutine(_imuWindowCoroutine);
        _imuWindowCoroutine = StartCoroutine(IMUWindowCoroutine(seconds));
    }

    private IEnumerator IMUWindowCoroutine(float seconds)
    {
        _imuWindowActive = true;
        Debug.Log($"[IMU] Window open for {seconds}s");
        yield return new WaitForSeconds(seconds);
        if(calibratingPosition != null)
        {
            yield return new WaitUntil(() => calibratingPosition.finishCalibrating);
        }
        
        _imuWindowActive = false;
        Debug.Log("[IMU] Window closed");
        _imuWindowCoroutine = null;
    }

    // ═════════════════════════════════════════════════════════
    //  Calibration
    // ═════════════════════════════════════════════════════════

    public void SendInitWire()
    {
        if (HapticUDPController.Instance == null) {
            Debug.LogWarning("[IMU] No HapticUDPController");
            return;
        }
        HapticUDPController.Instance.SendCommand("INITIMU");
        Debug.Log("[IMU] Sent INITIMU");
    }

    public void SendCalibrate()
    {
        if (HapticUDPController.Instance == null) {
            Debug.LogWarning("[IMU] No HapticUDPController");
            return;
        }
        HapticUDPController.Instance.SendCommand("CALIBRATE");
        Debug.Log("[IMU] Sent CALIBRATE");
        RequestIMUWindow(timeOn);
    }

    private void OnCalibrationReceived(float ax, float ay, float az)
    {
        _calAx = ax; _calAy = ay; _calAz = az;
        _calibrated = true;
        Debug.Log($"[IMU] Calibration stored: ({ax:F3}, {ay:F3}, {az:F3})");
    }

    // ═════════════════════════════════════════════════════════
    //  Receive loop
    // ═════════════════════════════════════════════════════════
    private void ReceiveLoop()
    {
        IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
        while (_receiver != null)
        {
            try {
                byte[] data = _receiver.Receive(ref remote);
                string msg  = Encoding.UTF8.GetString(data);
                if (msg.StartsWith("IMU:")) ParseIMU(msg);
            } catch (Exception e) {
                if (_receiver == null) break;
                Debug.LogWarning($"[IMU] Receive error: {e.Message}");
            }
        }
    }

    private void ParseIMU(string msg)
    {
        string[] p = msg.Split(':');
        if (p.Length < 7) return;
        if (!TryParseFloat(p[1], out float ax)) return;
        if (!TryParseFloat(p[2], out float ay)) return;
        if (!TryParseFloat(p[3], out float az)) return;
        if (!TryParseFloat(p[4], out float gx)) return;
        if (!TryParseFloat(p[5], out float gy)) return;
        if (!TryParseFloat(p[6], out float gz)) return;

        lock (_lock) {
            _ax = ax; _ay = ay; _az = az;
            _gx = gx; _gy = gy; _gz = gz;
            _newData = true;
        }
    }

    // ═════════════════════════════════════════════════════════
    //  Helpers
    // ═════════════════════════════════════════════════════════
    string GetLocalIP()
    {
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
        Debug.LogWarning($"[IMU] No IP with prefix '{subnetPrefix}' — using 0.0.0.0");
        return "0.0.0.0";
    }

    static bool TryParseFloat(string s, out float r)
        => float.TryParse(s, System.Globalization.NumberStyles.Float,
                          System.Globalization.CultureInfo.InvariantCulture, out r);
}