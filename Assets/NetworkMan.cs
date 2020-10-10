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
        udp.Connect("ec2-3-15-221-96.us-east-2.compute.amazonaws.com", 12345);
        //udp.Connect("localhost", 12345);

        

        //Send data
        //We can only send Byte type so we need to convert data to Bytes
        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
        udp.Send(sendBytes, sendBytes.Length);


        //Make OnReceived Function to handle all receving data, pass argument for OnReceived function
        udp.BeginReceive(new AsyncCallback(OnReceived), udp);
  


        InvokeRepeating("HeartBeat", 1, 1);

        InvokeRepeating("SendPos", 0.01f, 0.01f);

        
        
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


        public V3Pos rot;
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
    public class DisconnectedClientID
    {
        public string id;
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

    //TO delete disconnected client
    private string latestDisconnectedClientID;
    private bool ShouldDeleteClient = false;
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

                    msg = "New client connected!\n";
                    msg += "ID: " + latestPlayerInfo.id;
                    Debug.Log(msg);

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

                    msg = "All clients list\n";

                    for (int i = 0; i < latestAllClientInfo.players.Length; ++i)
                    {
                        msg += "Client" + (i + 1) + "\n";
                        msg += "ID: " + latestAllClientInfo.players[i].id + "\n\n";
                    }

                    Debug.Log(msg);

                    ShouldSpawnOtherClients = true;

                    break;
                case commands.DISCONNECTED_CLIENT:
                    DisconnectedClientID disconnectedClientID = JsonUtility.FromJson<DisconnectedClientID>(returnData);

                    //msg = "disconnected client\n";
                    //msg += "IP: " + disconnectedClient.IP + "\n";
                    //msg += "PORT: " + disconnectedClient.PORT;
                    //Debug.Log(msg);

                    Debug.Log("Disconnected Client: " + disconnectedClientID.id);

                    latestDisconnectedClientID = disconnectedClientID.id;
                    ShouldDeleteClient = true;

                    

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
            if (info.id == id)
            {
                playerTransform.GetComponent<Renderer>().material.color = new Color(info.color.R, info.color.G, info.color.B);
                continue;
            }
            else
            {
                if (listOfPlayers.ContainsKey(info.id) == true)
                {
                    listOfPlayers[info.id].transform.position = new Vector3(info.pos.X, info.pos.Y, info.pos.Z);
                    listOfPlayers[info.id].transform.eulerAngles = new Vector3(info.rot.X, info.rot.Y, info.rot.Z);
                    listOfPlayers[info.id].transform.GetComponent<Renderer>().material.color = new Color(info.color.R, info.color.G, info.color.B);
                }
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


        string rot = "rotation " + playerTransform.rotation.eulerAngles.x.ToString() + " " +
                                   playerTransform.rotation.eulerAngles.y.ToString() + " " +
                                   playerTransform.rotation.eulerAngles.z.ToString() + " ";

        sendBytes = Encoding.ASCII.GetBytes(rot);
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update()
    {
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();


        SpawnOtherClient();

        //Setting IP and PORt of mine
        if(ShouldSetIP == true)
        {
            playerTransform.GetComponentInChildren<TextMesh>().text = myIP + " " + myPORT.ToString();

            ShouldSetIP = false;
        }

        //Delete disconnected client
        if (ShouldDeleteClient == true)
        {
            if (listOfPlayers.ContainsKey(latestDisconnectedClientID) == true)
            {
                Destroy(listOfPlayers[latestDisconnectedClientID]);
                listOfPlayers.Remove(latestDisconnectedClientID);
            }
            
            ShouldDeleteClient = false;
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






//////////////////////////////////////////SOLUTION/////////////////////////////////////////
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using System;
//using System.Text;
//using System.Net.Sockets;
//using System.Net;

///// <summary>
///// A class that takes care of talking to our server
///// </summary>
//public class NetworkMan : MonoBehaviour
//{
//    public UdpClient udp; // an instance of the UDP client
//    public GameObject playerGO; // our player object

//    public string myAddress; // my address = (IP, PORT)
//    public Dictionary<string, GameObject> currentPlayers; // A list of currently connected players
//    public List<string> newPlayers, droppedPlayers; // a list of new players, and a list of dropped players
//    public GameState lastestGameState; // the last game state received from server
//    public ListOfPlayers initialSetofPlayers; // initial set of players to spawn

//    public MessageType latestMessage; // the last message received from the server


//    // Start is called before the first frame update
//    void Start()
//    {
//        // Initialize variables
//        newPlayers = new List<string>();
//        droppedPlayers = new List<string>();
//        currentPlayers = new Dictionary<string, GameObject>();
//        initialSetofPlayers = new ListOfPlayers();
//        // Connect to the client.
//        // All this is explained in Week 1-4 slides
//        udp = new UdpClient();
//        Debug.Log("Connecting...");
//        udp.Connect("localhost", 12345);
//        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
//        udp.Send(sendBytes, sendBytes.Length);
//        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

//        InvokeRepeating("HeartBeat", 1, 1);
//    }

//    void OnDestroy()
//    {
//        udp.Dispose();
//    }

//    /// <summary>
//    /// A structure that replicates our server color dictionary
//    /// </summary>
//    [Serializable]
//    public struct receivedColor
//    {
//        public float R;
//        public float G;
//        public float B;
//    }

//    /// <summary>
//    /// A structure that replicates our player dictionary on server
//    /// </summary>
//    [Serializable]
//    public class Player
//    {
//        public string id;
//        public receivedColor color;
//    }


//    [Serializable]
//    public class ListOfPlayers
//    {
//        public Player[] players;

//        public ListOfPlayers()
//        {
//            players = new Player[0];
//        }
//    }
//    [Serializable]
//    public class ListOfDroppedPlayers
//    {
//        public string[] droppedPlayers;
//    }

//    /// <summary>
//    /// A structure that replicates our game state dictionary on server
//    /// </summary>
//    [Serializable]
//    public class GameState
//    {
//        public int pktID;
//        public Player[] players;
//    }

//    /// <summary>
//    /// A structure that replicates the mesage dictionary on our server
//    /// </summary>
//    [Serializable]
//    public class MessageType
//    {
//        public commands cmd;
//    }

//    /// <summary>
//    /// Ordererd enums for our cmd values
//    /// </summary>
//    public enum commands
//    {
//        PLAYER_CONNECTED,       //0
//        GAME_UPDATE,            // 1
//        PLAYER_DISCONNECTED,    // 2
//        CONNECTION_APPROVED,    // 3
//        LIST_OF_PLAYERS,        // 4
//    };

//    void OnReceived(IAsyncResult result)
//    {
//        // this is what had been passed into BeginReceive as the second parameter:
//        UdpClient socket = result.AsyncState as UdpClient;

//        // points towards whoever had sent the message:
//        IPEndPoint source = new IPEndPoint(0, 0);

//        // get the actual message and fill out the source:
//        byte[] message = socket.EndReceive(result, ref source);

//        // do what you'd like with `message` here:
//        string returnData = Encoding.ASCII.GetString(message);
//        // Debug.Log("Got this: " + returnData);

//        latestMessage = JsonUtility.FromJson<MessageType>(returnData);

//        Debug.Log(returnData);
//        try
//        {
//            switch (latestMessage.cmd)
//            {
//                case commands.PLAYER_CONNECTED:
//                    ListOfPlayers latestPlayer = JsonUtility.FromJson<ListOfPlayers>(returnData);
//                    Debug.Log(returnData);
//                    foreach (Player player in latestPlayer.players)
//                    {
//                        newPlayers.Add(player.id);
//                    }
//                    break;
//                case commands.GAME_UPDATE:
//                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
//                    break;
//                case commands.PLAYER_DISCONNECTED:
//                    ListOfDroppedPlayers latestDroppedPlayer = JsonUtility.FromJson<ListOfDroppedPlayers>(returnData);
//                    foreach (string player in latestDroppedPlayer.droppedPlayers)
//                    {
//                        droppedPlayers.Add(player);
//                    }
//                    break;
//                case commands.CONNECTION_APPROVED:
//                    ListOfPlayers myPlayer = JsonUtility.FromJson<ListOfPlayers>(returnData);
//                    Debug.Log(returnData);
//                    foreach (Player player in myPlayer.players)
//                    {
//                        newPlayers.Add(player.id);
//                        myAddress = player.id;
//                    }
//                    break;
//                case commands.LIST_OF_PLAYERS:
//                    initialSetofPlayers = JsonUtility.FromJson<ListOfPlayers>(returnData);
//                    break;
//                default:
//                    Debug.Log("Error: " + returnData);
//                    break;
//            }
//        }
//        catch (Exception e)
//        {
//            Debug.Log(e.ToString());
//        }

//        // schedule the next receive operation once reading is done:
//        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
//    }

//    void SpawnPlayers()
//    {
//        if (newPlayers.Count > 0)
//        {
//            foreach (string playerID in newPlayers)
//            {
//                currentPlayers.Add(playerID, Instantiate(playerGO, new Vector3(0, 0, 0), Quaternion.identity));
//                currentPlayers[playerID].name = playerID;
//            }
//            newPlayers.Clear();
//        }
//        if (initialSetofPlayers.players.Length > 0)
//        {
//            Debug.Log(initialSetofPlayers);
//            foreach (Player player in initialSetofPlayers.players)
//            {
//                if (player.id == myAddress)
//                    continue;
//                currentPlayers.Add(player.id, Instantiate(playerGO, new Vector3(0, 0, 0), Quaternion.identity));
//                currentPlayers[player.id].GetComponent<Renderer>().material.color = new Color(player.color.R, player.color.G, player.color.B);
//                currentPlayers[player.id].name = player.id;
//            }
//            initialSetofPlayers.players = new Player[0];
//        }
//    }

//    void UpdatePlayers()
//    {
//        if (lastestGameState.players.Length > 0)
//        {
//            foreach (NetworkMan.Player player in lastestGameState.players)
//            {
//                string playerID = player.id;
//                currentPlayers[player.id].GetComponent<Renderer>().material.color = new Color(player.color.R, player.color.G, player.color.B);
//            }
//            lastestGameState.players = new Player[0];
//        }
//    }

//    void DestroyPlayers()
//    {
//        if (droppedPlayers.Count > 0)
//        {
//            foreach (string playerID in droppedPlayers)
//            {
//                Debug.Log(playerID);
//                Debug.Log(currentPlayers[playerID]);
//                Destroy(currentPlayers[playerID].gameObject);
//                currentPlayers.Remove(playerID);
//            }
//            droppedPlayers.Clear();
//        }
//    }

//    void HeartBeat()
//    {
//        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
//        udp.Send(sendBytes, sendBytes.Length);
//    }

//    void Update()
//    {
//        SpawnPlayers();
//        UpdatePlayers();
//        DestroyPlayers();
//    }
//}