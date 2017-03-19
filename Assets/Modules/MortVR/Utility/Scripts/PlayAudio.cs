using UnityEngine;
using System.Collections;

public class PlayAudio : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
        void OnCollisionEnter (Collision col)
    {
            if (col.gameObject.GetComponent<AudioSource>() != null)
            {
            col.gameObject.GetComponent<AudioSource>().Play();
            }
        }
    }
}
