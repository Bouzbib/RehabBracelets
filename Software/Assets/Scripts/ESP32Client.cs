using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class ESP32Client : MonoBehaviour
{
	public string esp32IP = "192.168.1.2";
	UdpClient client;

    Thread receiveThread;

    public int port = 4210;

    void Start()
    {
        client = new UdpClient(4211);

        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();

        Debug.Log("UDP Client Started");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            Send("ON");
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            Send("OFF");
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            Send("STATUS");
        }
    }

    void Send(string message)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);

            client.Send(data, data.Length, esp32IP, port);

            Debug.Log("Sent: " + message);
        }
        catch (System.Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    void ReceiveData()
	{
	    IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);

	    while (true)
	    {
	        try
	        {
	            byte[] data = client.Receive(ref anyIP);

	            string text = Encoding.UTF8.GetString(data);

	            Debug.Log("ESP32 says: " + text);
	        }
	        catch (SocketException)
	        {
	            // Ignore socket shutdown errors
	            break;
	        }
	        catch (System.Exception e)
	        {
	            Debug.Log(e.ToString());
	            break;
	        }
	    }
	}

    private void OnApplicationQuit()
	{
	    if (client != null)
	    {
	        client.Close();
	    }

	    if (receiveThread != null && receiveThread.IsAlive)
	    {
	        receiveThread.Interrupt();
	    }
	}
}