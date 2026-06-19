using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

public class ReceiveData : MonoBehaviour
{

    [Header("The rest")]
    private Thread receiveThread;
    private UdpClient udpClient;
    public int port = 12345;
    public string receivedMessage;

    

    void Start()
    {
        StartUDPReceiver();
    }

    void OnApplicationQuit()
    {
        StopUDPReceiver();
    }

    private void StartUDPReceiver()
    {
        udpClient = new UdpClient(port);
        receiveThread = new Thread(new ThreadStart(ReceiveTheData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
        Debug.Log($"UDP Server listening on port {port}");
    }

    private void StopUDPReceiver()
    {
        if (receiveThread != null)
        {
            receiveThread.Abort();
            udpClient.Close();
            Debug.Log("UDP Server stopped.");
        }
    }

    private void ReceiveTheData()
    {
        try
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, port);
            while (true)
            {
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                receivedMessage = Encoding.UTF8.GetString(data);

                ParseMessage(receivedMessage);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error receiving data: {e}");
        }
    }

    private void ParseMessage(string message)
    {
        try
        {
            Debug.Log(message);

        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing message: {e}");
        }
    }

   
   
}
