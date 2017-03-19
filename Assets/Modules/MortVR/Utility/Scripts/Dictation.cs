using UnityEngine;
using System.Collections;
using System;

[Serializable]
public class Dictation{
    public int id;
    public string projectName;
    public float x;
    public float y;
    public float z;
    public string username;

    public void setPosition(Vector3 pos)
    {
        x = pos.x;
        y = pos.y;
        z = pos.z;
    }
}
