using UnityEngine;
using System.Collections;
using System;

[Serializable]
public class Dictation{
    public string GUID;
    public float x;
    public float y;
    public float z;
    public byte[] image;
    public byte[] audio;
    public string audioTranscription;
    public string username;
    public string designDiscipline;
    public string typeOfUser;
    public string estimatedCost;
    public string estimatedTime;
    public string levelOfImpact;

    public void setPosition(Vector3 pos)
    {
        x = pos.x;
        y = pos.y;
        z = pos.z;
    }
}
