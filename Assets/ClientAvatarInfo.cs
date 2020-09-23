//using System.Collections;
//using System.Collections.Generic;
using UnityEngine;

public class ClientAvatarInfo : MonoBehaviour
{
    //IP, PORT
    public string id;
    //// Start is called before the first frame update
    void Start()
    {
        GetComponentInChildren<TextMesh>().text = id;
    }

    //// Update is called once per frame
    //void Update()
    //{
        
    //}
}
