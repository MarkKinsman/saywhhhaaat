using UnityEngine;
using System.Collections;

public class PlayAudio : MonoBehaviour {


    
    void Start()
    {

    }

    void OnTriggerEnter (Collider col)
    {
        Debug.Log("Hey look I found a collision");
        if (col.gameObject.GetComponent<AudioSource>() != null)
        {
            if (!col.gameObject.GetComponent<AudioSource>().isPlaying)
            {
                Debug.Log("Hey look that collison has an audio source");
                col.gameObject.GetComponent<AudioSource>().Play();
            }
        }
    }

    /*IEnumerator whenAudioFinish(AudioSource audio)
    {
        if (audio.isPlaying)
        {
            yield return null;
        }

        audio.gameObject.GetComponent<MeshRenderer>().material.color = new
    }*/
}