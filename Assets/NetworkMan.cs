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

        InvokeRepeating("SendPos", 0.5f, 0.5f);
    }

    void OnDestroy()
    {
        Debug.Log("Close socket");
        CancelInvoke();
        udp.Close();
        //udp.Dispose();
    }




    public enum commands
    {
        NEW_CLIENT,
        UPDATE,
        All_CLIENT_INFO,
        DISCONNECTED_CLIENT,
        SENDER_IP_PORT
    };
    
    [Serializable]
    public class Message
    {
        public commands cmd;
    }
    
    [Serializable]
    public class Player
    {
        public string id;

        [Serializable]
        public struct receivedColor
        {
            public float R;
            public float G;
            public float B;
        }
        public receivedColor color;

        [Serializable]
        public struct V3Pos
        {
            public float X;
            public float Y;
            public float Z;
        }
        public V3Pos pos;
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

    //[Serializable]
    //public class AllClientsInfo
    //{
    //    public int numOfClients;

    //    public ClientInfo[] allClients;
    //}

    public string myIP = null;
    public int myPORT;
    private bool ShouldSetIP = false;

    public Message latestMessage;
    public GameState lastestGameState;
    //public AllClientsInfo lastestAllClietnsInfo;

    //List of currently connected players
    //public List<Player> listOfPlayers = new List<Player>();
    private Dictionary<string, GameObject> listOfPlayers = new Dictionary<string, GameObject>();
    //private List<GameObject> listOfPlayer = new List<GameObject>();
    public GameObject userAvatar;

    //TO create new player
    private bool ShouldSpawnAvatar = false;
    Player latestPlayerInfo = null;

    public Transform playerTransform;

    //To create other cube when enter game
    private GameState latestAllClientInfo;
    private bool ShouldSpawnOtherClients = false; 
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
            string msg = "";
            switch (latestMessage.cmd)
            {
                //New client connected
                case commands.NEW_CLIENT:
                    Debug.Log("NewClient");
                    latestPlayerInfo = JsonUtility.FromJson<Player>(returnData);

                    //msg = "New client\n";
                    //msg += "ID: " + newClient.id;
                    //Debug.Log(msg);

                    //Spawn new userAvatar
                    ShouldSpawnAvatar = true;
                    //Instantiate(userAvatar, new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0));
                    break;
                //Update game with new info
                case commands.UPDATE:
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                    break;
                //Get all clients info
                case commands.All_CLIENT_INFO:
                    Debug.Log("All client info");
                    //lastestAllClietnsInfo = JsonUtility.FromJson<AllClientsInfo>(returnData);
                    //msg = "All clients list\n";

                    //msg += "Number of clients: " + lastestAllClietnsInfo.numOfClients.ToString() + "\n";

                    //for (int i = 0; i < lastestAllClietnsInfo.numOfClients; ++i)
                    //{
                    //    msg += "Client" + i + "\n";
                    //    msg += "IP: " + lastestAllClietnsInfo.allClients[i].IP + "\n";
                    //    msg += "PORT: " + lastestAllClietnsInfo.allClients[i].PORT + "\n\n";
                    //}

                    //Debug.Log(msg);

                    //Save all client info to create cube
                    latestAllClientInfo = JsonUtility.FromJson<GameState>(returnData);
                    ShouldSpawnOtherClients = true;

                    break;
                case commands.DISCONNECTED_CLIENT:
                    ClientInfo disconnectedClient = JsonUtility.FromJson<ClientInfo>(returnData);
                    
                    msg = "disconnected client\n";
                    msg += "IP: " + disconnectedClient.IP + "\n";
                    msg += "PORT: " + disconnectedClient.PORT;
                    Debug.Log(msg);
                    break;
                case commands.SENDER_IP_PORT:
                    ClientInfo clientIPPORT = JsonUtility.FromJson<ClientInfo>(returnData);
                    myIP = clientIPPORT.IP;
                    myPORT = clientIPPORT.PORT;
                    ShouldSetIP = true;
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

    //Spawn new player
    void SpawnPlayers()
    {
        if(ShouldSpawnAvatar == true)
        {
            GameObject newPlayer = Instantiate(userAvatar, new Vector3(latestPlayerInfo.pos.X, latestPlayerInfo.pos.Y, latestPlayerInfo.pos.Z), new Quaternion(0, 0, 0, 0));
            newPlayer.GetComponent<ClientAvatarInfo>().id = latestPlayerInfo.id;
            //newPlayer.GetComponent<Material>().color = new Color(latestPlayerInfo.color.R, latestPlayerInfo.color.G, latestPlayerInfo.color.B);

            listOfPlayers.Add(latestPlayerInfo.id, newPlayer);

            ShouldSpawnAvatar = false;
        }
    }

    void UpdatePlayers()
    {
        for(int i = 0; i < lastestGameState.players.Length; ++i)
        {
            Player info = lastestGameState.players[i];

            string id = "(\'" + myIP + "\', " + myPORT.ToString() + ")";
            
            //if this info is mine.. skip
            if(info.id == id)
            {
                continue;
            }
            else
            {
                listOfPlayers[info.id].transform.position = new Vector3(info.pos.X, info.pos.Y, info.pos.Z);
            }
        }
    }

    void DestroyPlayers()
    {

    }
    
    void HeartBeat()
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
        //Debug.Log("HeartBeat");
    }
    
    //Send position to server
    void SendPos()
    {
        string pos = "position " + playerTransform.position.x.ToString() + " " +
                                   playerTransform.position.y.ToString() + " " +
                                   playerTransform.position.z.ToString() + " ";
        Byte[] sendBytes = Encoding.ASCII.GetBytes(pos);
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update()
    {
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();


        SpawnOtherClient();

        if(ShouldSetIP == true)
        {
            playerTransform.GetComponentInChildren<TextMesh>().text = myIP + " " + myPORT.ToString();

            ShouldSetIP = false;
        }
    }

    //Spawn existed players
    void SpawnOtherClient()
    {
        if( ShouldSpawnOtherClients == true)
        {
            for(int i = 0; i < latestAllClientInfo.players.Length; ++i)
            {
                Player clientInfo = latestAllClientInfo.players[i];

                GameObject newPlayer = Instantiate(userAvatar, new Vector3(clientInfo.pos.X, clientInfo.pos.Y, clientInfo.pos.Z), new Quaternion(0, 0, 0, 0));
                newPlayer.GetComponent<ClientAvatarInfo>().id = clientInfo.id;
                //newPlayer.GetComponent<Material>().color = new Color(clientInfo.color.R, clientInfo.color.G, clientInfo.color.B);

                listOfPlayers.Add(clientInfo.id, newPlayer);
            }

            ShouldSpawnOtherClients = false;
        }
    }

    void CreatePlayer()
    {
        
    }
}
