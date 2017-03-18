using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UpdateUIText : MonoBehaviour
{
    public void sliderRGBText(float newVal)
    {
        GetComponent<Text>().text = ((int)(newVal * 255)).ToString();
    }
}
