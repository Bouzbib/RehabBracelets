using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
/* This script works when the ESPs run "ESP32_UDP_withnewhotspot.ino and "ESP32_Hotspot", having relay and forward port to 4210*/
public class ESP32Comm : MonoBehaviour
{
    [Header("Relay (hotspot ESP32)")]
    public string relayIP   = "192.168.4.1";
    public int    relayPort = 4210;

    [Header("Local — must match FORWARD_PORT in hotspot")]
    public int   localPort    = 4210;
    public float sendInterval = 1f;

    private UdpClient _udpSend;
    private UdpClient _udpReceive;
    private Thread    _receiveThread;
    private float     _timer;
    private bool      _running;

    void Start()
    {
        string localIP = GetLocalIP();
        Debug.Log($"[ESP32Comm] Local IP: {localIP}");

        // Bind send socket to the 192.168.4.x interface
        _udpSend    = new UdpClient(new IPEndPoint(IPAddress.Parse(localIP), 0));
        // Listen on the same fixed port the relay forwards to
        _udpReceive = new UdpClient(new IPEndPoint(IPAddress.Parse(localIP), localPort));

        _running = true;
        _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
        _receiveThread.Start();

        // Register with relay
        Send("UNITY_HELLO");
        Debug.Log($"[ESP32Comm] Registered. Listening on {localIP}:{localPort}");
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= sendInterval)
        {
            _timer = 0f;
            Send("Hello from Unity!");
        }
    }

    void Send(string message)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            _udpSend.Send(data, data.Length, relayIP, relayPort);
            Debug.Log($"[ESP32Comm] Sent: \"{message}\"");
        }
        catch (Exception e) { Debug.LogError($"[ESP32Comm] Send error: {e.Message}"); }
    }

    void ReceiveLoop()
    {
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
        while (_running)
        {
            try
            {
                byte[] data = _udpReceive.Receive(ref ep);
                string msg  = Encoding.UTF8.GetString(data);
                Debug.Log($"[ESP32Comm] Received: \"{msg}\" from {ep}");
            }
            catch (SocketException) { break; }
            catch (Exception e) { Debug.LogError($"[ESP32Comm] Receive error: {e.Message}"); }
        }
    }

    string GetLocalIP()
    {
        foreach (var ip in Dns.GetHostAddresses(Dns.GetHostName()))
            if (ip.AddressFamily == AddressFamily.InterNetwork
                && ip.ToString().StartsWith("192.168.4."))
                return ip.ToString();
        Debug.LogWarning("[ESP32Comm] No 192.168.4.x IP found!");
        return "0.0.0.0";
    }

    void OnDestroy()
    {
        _running = false;
        _udpReceive?.Close();
        _udpSend?.Close();
        _receiveThread?.Abort();
    }
}