using System;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;

public enum RPS : byte { Rock, Paper, Scissors }
public enum MessageType : byte { Quit, Chat, RPS, Arrive, MyNumber, Lost, Host }

public class Client : MonoBehaviour
{
    [SerializeField] ChatWindow chatWindow;
    [SerializeField] string serverAddress = "127.0.0.1";
    [SerializeField] int port = 56789;

    public static Dictionary<int, RPS> userInfo = new Dictionary<int, RPS>();
    public static Dictionary<int, bool> lostInfo = new Dictionary<int, bool>();
    public static int myNumber;
    public static int hostNumber;

    public static Action<string> ClaimChat;
    public static Action<RPS> ClaimRPS;
    public static Action<int,bool> ClaimLost;
    public static Action<int> ClaimHost;

    public static Action<string> RecieveChat;
    public static Action<int, RPS> RecieveRPS;
    public static Action<int, RPS> RecieveArrive;
    public static Action<int, bool> RecieveLost;
    public static Action<int> RecieveMyNumber;
    public static Action<int> HostNumber;

    public static Queue<byte[]> messageQueue = new Queue<byte[]>();

    bool isConnected = false;

    // ���� �� ���� �κ�.
    [SerializeField] private float maxTime;
    private float currentTime = 0;
    public float CurrentTime { get { return currentTime; } }

    public bool isGameStart = false;
    public bool isHost = false;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("generating client socket");
        // ������ ���� �����̶� �����ϰ� ��.
        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        // Ŭ���̾�Ʈ �������� ����Ʈ�� ��µ�, ����� ������ ������. IP �Ľ�,    ��Ʈ ��ȣ
        IPEndPoint clientPoint = new IPEndPoint(IPAddress.Parse(serverAddress), port);
        Debug.Log("try connect server");
        try
        {
            clientSocket.Connect(clientPoint);
            Debug.Log("connect success");
            isConnected = true;
            ClaimChat = (message) =>
            {
                byte[] messageByte = Encoding.UTF8.GetBytes(message);
                clientSocket.Send(CreateMessage(MessageType.Chat, ref messageByte)); 
            };
            ClaimRPS = (message) =>
            {
                byte[] messageByte = new byte[1];
                messageByte[0] = (byte)message;
                clientSocket.Send(CreateMessage(MessageType.RPS, ref messageByte));
            };
            ClaimLost = (number, message) =>
            {
                Debug.Log($"{number}:{message}");
                byte[] messageByte = new byte[5];
                messageByte[0] = Convert.ToByte(message);
                IntInsertToByteArray(number, ref messageByte, 1, 4);
                clientSocket.Send(CreateMessage(MessageType.Lost, ref messageByte));
            };
            ClaimHost = (message) =>
            {
                byte[] messageByte = new byte[4];
                IntInsertToByteArray(message, ref messageByte, 0, 3);
                clientSocket.Send(CreateMessage(MessageType.Host, ref messageByte));
            };
            RecieveMyNumber = (number) => { 
                myNumber= number; Debug.Log($"MyNumber : {number}"); 
            };
            RecieveArrive = (number, rps) => {
                Debug.Log($"{number}");
                if (!userInfo.ContainsKey(number)) 
                { userInfo.Add(number, rps); }
                else
                {
                    userInfo[number] = rps;
                }
                lostInfo.Add(number, false); Debug.Log($"{number} has Arrive:{rps}"); };
            HostNumber = (number) => { hostNumber = number;
                if (number == myNumber)
                {
                    isHost = true;
                }
                else { isHost = false; }
                Debug.Log($"HostNumber : {hostNumber}"); };
            RecieveLost = (number, lost) => { lostInfo[number] = lost; };
            RecieveRPS = (number, rps) =>
                {
                    if (userInfo.ContainsKey(number))
                    {
                        userInfo[number] = rps;
                        Debug.Log($"{number} change Position : {rps}");
                    }
                    else
                    {
                        Debug.Log("Long Number Detected");
                    }
                }; 


            Task.Run(() =>
            {
                try
                {
                    byte[] buffer = new byte[1024];
                    while (isConnected)
                    {
                        if (clientSocket.Poll(10000, SelectMode.SelectRead))
                        {
                            // �����͸� �޾Ƽ� ���̸� length�� �־��ְ�, ������ ���ۿ� �־��ٰ�.
                            int length = clientSocket.Receive(buffer);

                            if (length > 0)
                            {
                                byte[] currentBuffer = new byte[length];
                                Array.Copy(buffer, currentBuffer, length);
                                // �޽����� �޴´�. ��밡 UTF8�� ������, ���⵵ UTF8�� ����
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
        catch (Exception e)
        {
            Debug.LogError($"server connect failed{e}");
        }

    }

    private void OnDestroy()
    {
        isConnected = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.F12))
        {
            int numb = UnityEngine.Random.Range(0, userInfo.Count);
            Debug.Log($"{numb}");
            ClaimHost(numb);
            if(numb == myNumber)
            {
                isHost= true;
            }
            else { isHost= false; }
            
        }
        if(Input.GetKeyDown(KeyCode.Escape))
        { 
            ClaimChat("3�� �� ������������ �մϴ�. �����ּ���.");
            isGameStart = true;
            Debug.Log("dd");
        }
        if (messageQueue.TryDequeue(out byte[] buffer))
        {
            ReadMessage(ref buffer);
        }
        if(isGameStart)
        {
            // ���� ��ȸ�ڸ� �ð��� ���� ����
            if (isHost)
            {
                currentTime += Time.deltaTime;
                if (currentTime >= maxTime)
                {
                    Debug.Log($"{userInfo.Count} ��");
                    int remain = 0;
                    foreach (KeyValuePair<int, bool> guestData in lostInfo)
                    {
                        if(!guestData.Value) { remain++; }
                    }
                        Debug.Log($"{remain} �� ����");
                    if (userInfo.Count > 1)
                    {
                        int loseStack = 0;
                        // ��ȸ�ڶ� ������������ üũ��. ��ȸ�ڴ� �ϴ� �� �̱� ������, ��ȸ�ڰ� 
                        foreach (KeyValuePair<int, RPS> guestData in userInfo)
                        {
                            if (guestData.Key != myNumber)
                            {
                                if (!lostInfo[guestData.Key])
                                {
                                    int result = GameRule.CheckRPS(userInfo[myNumber], guestData.Value);
                                    switch (result)
                                    {
                                        case 1:
                                            ClaimChat($"{guestData.Key} �÷��̾ �¸��Ͽ����ϴ�!\n");
                                            Debug.Log("�̱�");
                                            loseStack++;
                                            break;
                                        case 0:
                                            ClaimChat($"{guestData.Key} �÷��̾ �����ϴ�.\n");
                                            Debug.Log(" ���");
                                            break;
                                        case -1:
                                            ClaimChat($"{guestData.Key} �÷��̾ �����ϴ�...\n");
                                            ClaimLost(guestData.Key, true);
                                            Debug.Log("��");
                                            break;
                                        default:
                                            ClaimChat($"{guestData.Key} �÷��̾��... ?");
                                            Debug.Log("�־�");
                                            break;
                                    }
                                }
                            }
                        }
                        if(loseStack >= userInfo.Count)
                        {
                            ClaimChat($"������ �����ϴ�.");
                        }
                        currentTime = 0;
                        loseStack = 0;
                        isGameStart = false;
                    }
                    else
                    {
                        ClaimChat($"�����մϴ�! ������ �̰���ϴ�. ���Ӱ� ���۵˴ϴ�.");
                        foreach (KeyValuePair<int, RPS> guestData in userInfo)
                        {
                            ClaimLost(guestData.Key, false);
                        }
                    }
                }
            }
        }
    }

    public void ReadMessage(ref byte[] buffer)
    {
        if (buffer == null || buffer.Length == 0)
        {
            return;
        }
        byte[] realBuffer = new byte[buffer.Length - 1];
        Array.Copy(buffer, 1, realBuffer, 0, realBuffer.Length);

        // �޽��� Ÿ���� ���� 1 ����Ʈ.
        switch ((MessageType)buffer[0])
        {
            case MessageType.Chat:
                RecieveChat(Encoding.UTF8.GetString(realBuffer));
                break;
            case MessageType.RPS:
                RecieveRPS(ByteArrayToInt(ref realBuffer, 0, 3), (RPS)realBuffer[4]);
                break;
            case MessageType.Arrive:
                RecieveArrive(ByteArrayToInt(ref realBuffer, 0, 3), (RPS)realBuffer[4]);
                break;
            case MessageType.MyNumber:
                RecieveMyNumber(ByteArrayToInt(ref realBuffer, 0, 3));
                break;
            case MessageType.Lost:
                RecieveLost(ByteArrayToInt(ref realBuffer, 1, 4), Convert.ToBoolean(realBuffer[0]));
                break;
            case MessageType.Host:
                HostNumber(ByteArrayToInt(ref realBuffer, 0, 3));
                break;
        }
    }

    public static int ByteArrayToInt(ref byte[] array, int start, int end)
    {
        if (array == null || array.Length < 4 || start > end || end - start < 3)
        {
            return -1;
        }
        // ��Ʋ �����.
        // ū���� �����ο�.

        int result = 0;
        for (int i = start; i <= end; i++)
        {
            result >>= 8;
            result += array[i];
        }
        return result;
    }

    public static void IntInsertToByteArray(int target, ref byte[] array, int start, int end)
    {
        if (array == null || array.Length < 4 || start>end || end - start < 3)
        { return; }

        for (int i = end; i >= start; --i)
        {
            array[i] = (byte)(0xFF000000 & (target >> 8 * (i - end)));
        }
    }

    public byte[] CreateMessage(MessageType wantType, ref byte[] content)
    {
        byte[] result = new byte[content.Length + 1];
        result[0] = (byte)wantType;
        Array.Copy(content, 0, result, 1, content.Length);
        return result;
    }
}
