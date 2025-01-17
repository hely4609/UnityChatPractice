﻿using System;
using System.Net;
// 다른사람이랑 연결을 위한 소켓!
// 한번 연결한 상태로 데이터를 왔다갔다 하기위해 사용!?
using System.Net.Sockets;

// 한사람이 입력하는 동안 다른사람이 입력을 못하는 불상사를 막기위해 ..
using System.Threading;
using System.Threading.Tasks;

// 컴퓨터는 모두 같은 문자코드로 글자를 표현하지 않습니다!
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace RPS_Server
{
    internal class Program
    {
        enum RPS : byte { Rock, Paper, Scissors }
        enum MessageType : byte { Quit, Chat, RPS, Arrive, MyNumber, Lost, Host }

        // 돌고 있는지 없는지를 표시해봅시다!
        private static bool isRunning = false;
        private static byte[] greetingMessage = null;
        private static List<Socket> userSocketList = new List<Socket>();
        static Dictionary<int, RPS> userRPSData = new Dictionary<int, RPS>();
        static Dictionary<int, bool> lostInfo = new Dictionary<int, bool>();
        static int hostNumber = 0;

        // 포트 번호
        private const int port = 56789;

        static void Main(string[] args)
        {

            isRunning = true;

            greetingMessage = Encoding.UTF8.GetBytes("Hello, World");

            Task.Run(WaitForConnet);

            while (isRunning)
            {
                string command = Console.ReadLine().ToLower();
                if (command == null) continue;
                else
                {
                    switch (command)
                    {
                        case "exit":
                        case "quit":
                        case "end":
                        case "break":
                            Console.WriteLine("Close Server..");
                            isRunning = false;
                            break;
                    }
                }
            }
        }

        public static async Task WaitForConnet()
        {
            Console.WriteLine("[RPS server Activating]");
            Console.WriteLine("Generationg Listen Socket..");
            // 연결을 받을 건데, 얻다 꽂을 것인가? -> 소켓
            // 저희 클래스로 만드시면 되요!
            // 사람마다 소켓을 받을 수 있기는 함!
            // 제가 다른 사람의 입장을 받아주는 문지기?
            // 누가 오는지 감시하는 역할의 소켓!
            // 누가 오면 담당자를 붙여주는 역할? -> Listen Socket

            // SocketType
            // DGram : DataGram -> 데이터 덩어리 : 정보가 매무 많음 그냥 던져놓고 데이터가 알아서 가게 만들어요!
            //                                    그 뒤에 뭔 일이 있는지 난 몰름! ㄹㅇ 전혀 신경 안 써도 됨!
            //                     UDP
            // Stream 흐름! 왔다갔다 하면서 검증하는것, TCP
            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // 서버는 아무나 들어오는 것을 일단 허용! -> 아무나 받는다고 하면.. 정말 아무나 들어와도 되나?
            // 디스코드 음성 데이터.. 구글 검색 데이터..? 받아도 되나요?
            // 프로그램들은 알아서 "본인이 받고 싶은 항구" 가 있게 돼요! 배가 입항해서 데이터를 실어 놓죠!
            // 데이터가 "포트(Port)"를 통해서 들어온다! -> 포트는 2byte 주소 자료형! 0 ~ 65535 까지 가능!
            // 내가 만약 80번 포트(http)의 주인이에요! 120번 포트로 물건이 들어왔습니다! 받으면 안됩니다!
            // 0 ~ 1023 "잘 알려진 포트" (시스템에 관련된 포트들)
            // 1024 ~ 49151 등록된 포트 (쓸 수는 있는데.. 겹칠 가능성이 좀 있음)
            // 49152 ~ 65535 동적포트 -> 여기는 맘대로 !

            // 이 포트로 받겠다고 선언!
            // 이 IP가 데이터의 종착지가 되는 거에요!
            IPEndPoint listenPoint = new IPEndPoint(IPAddress.Any, port);

            Console.WriteLine("Binding..");
            // 소켓을 연결할 거에요!
            listenSocket.Bind(listenPoint);

            Console.WriteLine("Listen Socket Bind Successfully");

            while (isRunning)
            {
                await Task.Run(() =>
                {
                    // 이 친구가 무언가를 들을 때 까지 대기 하는건 곤란하다!
                    // 듣는 거는 다른 스레드로 보내버립시다!
                    listenSocket.Listen(0);

                    // 새로 들어온 친구를 위한 새로운 스레드
                    Task.Run(() =>
                    {
                        try
                        {
                            // 들었음! 누가왔어요!
                            // 다른 소켓에 옮겨줘야 해요!
                            // 받은 친구를 새로운 소켓에 넣어줍시다!
                            Socket currentClient = listenSocket.Accept();
                            userSocketList.Add(currentClient);

                            // 버퍼 -> 들어온 입력을 잠깐 저장해놓는 공간!
                            byte[] buffer = new byte[1024];

                            Console.WriteLine($"Connected : {currentClient.Connected}");

                            byte[] greetingBuffer = new byte[6];
                            byte[] arriveBuffer = new byte[6];

                            greetingBuffer[0] = (byte)MessageType.Arrive;
                            arriveBuffer[0] = (byte)MessageType.Arrive;
                            IntInsertToByteArray(userSocketList.IndexOf(currentClient), ref arriveBuffer, 1, 4);
                            if (userRPSData.ContainsKey(userSocketList.IndexOf(currentClient)))
                            {
                                userRPSData[userSocketList.IndexOf(currentClient)] = RPS.Rock;
                            }
                            else
                            {
                                userRPSData.Add(userSocketList.IndexOf(currentClient), RPS.Rock);
                            }


                            foreach (Socket currentSocket in userSocketList)
                            {
                                IntInsertToByteArray(userSocketList.IndexOf(currentSocket), ref greetingBuffer, 1, 4);
                                greetingBuffer[5] = (byte)userRPSData[userSocketList.IndexOf(currentSocket)];
                                currentClient.Send(greetingBuffer);
                                Console.WriteLine(greetingBuffer.Length);
                                if (currentClient != currentSocket)
                                {
                                    currentSocket.Send(arriveBuffer);
                                }
                                Thread.Sleep(1);
                            }
                            greetingBuffer = new byte[5];
                            greetingBuffer[0] = (byte)MessageType.MyNumber;
                            IntInsertToByteArray(userSocketList.IndexOf(currentClient), ref greetingBuffer, 1, 4);
                            currentClient.Send(greetingBuffer);


                            while (isRunning)
                            {
                                if (currentClient.Poll(1000000, SelectMode.SelectRead))
                                {
                                    if (currentClient.Available == 0)
                                    {
                                        userSocketList.Remove(currentClient);
                                        throw new Exception("Completed");
                                    }

                                    int length = currentClient.Receive(buffer);
                                    CallBack(ref currentClient, ref buffer, length);
                                }
                            }

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Connection Lost : {e.Message}");
                        }
                    });

                });
            }

        }

        public static void CallBack(ref Socket currentClient, ref byte[] buffer, int length)
        {
            if (length > 0)
            {
                byte[] currentBuffer = new byte[length - 1];
                byte[] recieveBuffer = new byte[length];
                Array.Copy(buffer, 1, currentBuffer, 0, currentBuffer.Length);
                Array.Copy(buffer, recieveBuffer, length);

                switch ((MessageType)buffer[0])
                {
                    case MessageType.Chat:
                        {
                            string message = Encoding.UTF8.GetString(currentBuffer, 0, currentBuffer.Length);
                            Console.WriteLine(message);
                            foreach (Socket currentSocket in userSocketList)
                            {
                                currentSocket.Send(recieveBuffer);
                            }
                            break;
                        }
                    case MessageType.RPS:
                        {
                            userRPSData[userSocketList.IndexOf(currentClient)] = (RPS)currentBuffer[0];
                            byte[] sendBuffer = new byte[6];
                            sendBuffer[0] = (byte)MessageType.RPS;
                            IntInsertToByteArray(userSocketList.IndexOf(currentClient), ref sendBuffer, 1, 4);
                            sendBuffer[5] = currentBuffer[0];
                            foreach (Socket currentSocket in userSocketList)
                            {
                                currentSocket.Send(sendBuffer);
                            }
                            break;
                        }
                    case MessageType.Lost:
                        {
                            int t = ByteArrayToInt(ref currentBuffer, 0, 3);
                            Console.WriteLine(t+"숫자 / "+ Convert.ToBoolean(currentBuffer[4]));
                            
                            lostInfo[userSocketList.IndexOf(currentClient)] = Convert.ToBoolean(currentBuffer[4]);

                            byte[] sendBuffer = new byte[6];
                            sendBuffer[0] = (byte)(MessageType.Lost);
                            IntInsertToByteArray(t, ref sendBuffer, 1, 4);
                            sendBuffer[5] = currentBuffer[4];
                            foreach (Socket currentSocket in userSocketList)
                            {
                                currentSocket.Send(sendBuffer);
                            }
                            break;
                        }
                    case MessageType.Host:
                        {
                            hostNumber = ByteArrayToInt(ref currentBuffer, 0, 3);

                            byte[] sendBuffer = new byte[5];
                            sendBuffer[0] = (byte)MessageType.Host;
                            IntInsertToByteArray(hostNumber, ref sendBuffer, 1, 4);
                            foreach (Socket currentSocket in userSocketList)
                            {
                                currentSocket.Send(sendBuffer);
                            }
                            //currentClient.Send(sendBuffer);
                            break;
                        }
                }
                Array.Clear(buffer, 0, length);
            }
        }


        public static int ByteArrayToInt(ref byte[] array, int start, int end)
        {
            if (array == null || array.Length < 4 || end - start < 3)
            {
                return -1;
            }
            // 리틀 엔디안.
            // 큰것이 앞으로옴.

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
            if (array == null || array.Length < 4 || end - start < 3)
            { return; }

            for (int i = end; i >= start; --i)
            {
                array[i] = (byte)(0x000000FF & (target >> 8 * (i - end)));
            }
        }
    }
}
