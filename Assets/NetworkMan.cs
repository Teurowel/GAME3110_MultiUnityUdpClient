using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;

public class NetworkMan : MonoBehaviour
{
    //Like socket in python
    public UdpClient udp;
    // Start is called before the first frame update
    void Start()
    {
        //Create socket
        udp = new UdpClient();

        //Set socket to connet server
        //udp.Connect("ec2-3-15-221-96.us-east-2.compute.amazonaws.com", 12345);
        udp.Connect("localhost", 12345);


        //Send data
        //We can only send Byte type so we need to convert data to Bytes
        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
        udp.Send(sendBytes, sendBytes.Length);


        //Make OnReceived Function to handle all receving data, pass argument for OnReceived function
        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1, 1);
    }

    void OnDestroy()
    {
        udp.Dispose();
    }




    public enum commands
    {
        NEW_CLIENT,
        UPDATE,
        All_CLIENT_INFO
    };
    
    [Serializable]
    public class Message
    {
        public commands cmd;
    }
    
    [Serializable]
    public class Player
    {
        [Serializable]
        public struct receivedColor
        {
            public float R;
            public float G;
            public float B;
        }
        public string id;
        public receivedColor color;        
    }

    [Serializable]
    public class NewPlayer
    {
        
    }

    [Serializable]
    public class GameState
    {
        public Player[] players;
    }

    [Serializable]
    public class ClientInfo
    {
        public string IP;
        public int PORT;
    }

    [Serializable]
    public class AllClientsInfo
    {
        public int numOfClients;

        public ClientInfo[] allClients;
    }

    public Message latestMessage;
    public GameState lastestGameState;
    public AllClientsInfo lastestAllClietnsInfo;
    void OnReceived(IAsyncResult result)
    {
        // this is what had been passed into BeginReceive as the second parameter from BeginReceive function:
        UdpClient socket = result.AsyncState as UdpClient;
        
        // points towards whoever had sent the message:
        //This will be information of who sent the message
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        //using socket, get last data,
        //when EndReceive is called, we stop handle next message until handling current message and continue when BeginReceive called
        byte[] message = socket.EndReceive(result, ref source);


        // do what you'd like with `message` here:
        //convert byte[] data(jason) to string
        string returnData = Encoding.ASCII.GetString(message);
        //Debug.Log("Got this: " + returnData);
        
        //convert string to Message class
        latestMessage = JsonUtility.FromJson<Message>(returnData);
        try
        {
            //What kind of message is this?
            switch(latestMessage.cmd)
            {
                //New client connected
                case commands.NEW_CLIENT:
                    break;
                //Update game with new info
                case commands.UPDATE:
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                    break;
                //Get all clients info
                case commands.All_CLIENT_INFO:
                    lastestAllClietnsInfo = JsonUtility.FromJson<AllClientsInfo>(returnData);
                    string msg = "All clients list\n";

                    msg += "Number of clients: " + lastestAllClietnsInfo.numOfClients.ToString() + "\n";

                    for (int i = 0; i < lastestAllClietnsInfo.numOfClients; ++i)
                    {
                        msg += "Client" + i + "\n";
                        msg += "IP: " + lastestAllClietnsInfo.allClients[i].IP + "\n";
                        msg += "PORT: " + lastestAllClietnsInfo.allClients[i].PORT + "\n\n";
                    }

                    Debug.Log(msg);
                    break;
                default:
                    Debug.Log("Error");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
        
        // schedule the next receive operation once reading is done:
        //continue get next message
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    void SpawnPlayers()
    {

    }

    void UpdatePlayers()
    {

    }

    void DestroyPlayers()
    {

    }
    
    void HeartBeat()
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update()
    {
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }
}
