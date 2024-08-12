using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEditor.Experimental.GraphView;
using UnityEditor.PackageManager;
using System.Threading.Tasks;
using JetBrains.Annotations;

public class Client : MonoBehaviour
{
    [SerializeField] string serverAddress = "127.0.0.1";
    [SerializeField] int port = 56789;

    public static Action<string> ClaimChat;
    public static Action<string> RecieveChat;
    public static Queue<byte[]> messageQueue = new Queue<byte[]>();

    bool isConnected = false;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("generating client socket");
        // 서버의 리슨 소켓이랑 동일하게 함.
        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        // 클라이언트 측에서도 포인트를 잡는데, 여기는 서버를 가야함. IP 파싱,    포트 번호
        IPEndPoint clientPoint = new IPEndPoint(IPAddress.Parse(serverAddress), port);
        Debug.Log("try connect server");
        try
        {
            clientSocket.Connect(clientPoint);
            Debug.Log("connect success");
            isConnected = true;
            ClaimChat = (message) => { clientSocket.Send(Encoding.UTF8.GetBytes(message)); };

            Task.Run(() =>
            {
                try
                {
                    byte[] buffer = new byte[1024];
                    while (isConnected)
                    {
                        if (clientSocket.Poll(10000, SelectMode.SelectRead))
                        {
                            // 데이터를 받아서 길이를 length에 넣어주고, 내용은 버퍼에 넣어줄것.
                            int length = clientSocket.Receive(buffer);

                            if (length > 0)
                            {
                                byte[] currentBuffer = new byte[length];
                                Array.Copy(buffer, currentBuffer, length);
                                // 메시지를 받는다. 상대가 UTF8로 했으니, 여기도 UTF8로 번역
                                //string message = Encoding.UTF8.GetString(buffer, 0, length);
                                //ebug.Log(message);
                                messageQueue.Enqueue(currentBuffer);
                            }
                        }
                    }
                    clientSocket.Disconnect(false);
                    Debug.LogError($"disconnected from server");
                }
                
                catch (Exception e)
                {
                    Debug.LogError($"server connection lost from server : {e}");

                }
            });
            Debug.Log("server connection  function initiated");
        }
        catch(Exception e)
        {
            Debug.LogError($"server connect failed{e}");
        }

    }

    private void OnDestroy()
    {
        isConnected= false;
    }

    // Update is called once per frame
    void Update()
    {
        if (messageQueue.TryDequeue(out byte[] buffer))
        {
            string message = Encoding.UTF8.GetString(buffer);
            RecieveChat(message);
        }
    }
}
