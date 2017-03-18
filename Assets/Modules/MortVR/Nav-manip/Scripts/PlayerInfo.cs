using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class PlayerInfo : MonoBehaviour {

    //Packeth that will be passed to player on update
    public struct PlayerInfoPacket
    {
        public string playerName;
        public Color playerColor;
    }

    private float r;
    private float g;
    private float b;
    private string myName = "Name";

    //UI Elements that are located below the prefab
    private UnityEngine.UI.InputField nameField;
    private UnityEngine.UI.Slider rSlider;
    private UnityEngine.UI.Slider gSlider;
    private UnityEngine.UI.Slider bSlider;
    private UnityEngine.UI.Image colorSwatch;

    //Delegate to send events when info has been changed
    public delegate void OnPlayerInfoChangeDelegate(PlayerInfoPacket p);
    public event OnPlayerInfoChangeDelegate EventOnPlayerInfoChange;

    //Create the info packet to be sent from private variables
    private PlayerInfoPacket PreparePlayerInfo()
    {

        PlayerInfoPacket p;
        p.playerName = myName;
        p.playerColor = new Color(r, g, b, 1);
        return p;
    }

    //Update variable and color swatch on the UI if slider info has changed
    private void SliderChanged(float value)
    {
        r = rSlider.value;
        g = gSlider.value;
        b = bSlider.value;
        colorSwatch.color = new Color(r, g, b, 1);
    }


    private void NameChanged(string value)
    {
        myName = nameField.text;
    }

    public void GetPlayerInfo()
    {
        if (EventOnPlayerInfoChange != null)
        {
            EventOnPlayerInfoChange(PreparePlayerInfo());
        }
    }

    // Use this for initialization
    void Start () {
        r = Random.value;
        g = Random.value;
        b = Random.value;

        nameField = GameObject.Find("Player Name/InputField").GetComponent<UnityEngine.UI.InputField>();
        rSlider = GameObject.Find("Player Color/R:/Slider").GetComponent<UnityEngine.UI.Slider>();
        gSlider = GameObject.Find("Player Color/G:/Slider").GetComponent<UnityEngine.UI.Slider>();
        bSlider = GameObject.Find("Player Color/B:/Slider").GetComponent<UnityEngine.UI.Slider>();
        colorSwatch = GameObject.Find("Player Color/Color Swatch").GetComponent<UnityEngine.UI.Image>();

        rSlider.value = r;
        gSlider.value = g;
        bSlider.value = b;

        rSlider.onValueChanged.AddListener(SliderChanged);
        gSlider.onValueChanged.AddListener(SliderChanged);
        bSlider.onValueChanged.AddListener(SliderChanged);
        nameField.onEndEdit.AddListener(NameChanged);

        colorSwatch.color = new Color(r, g, b, 1);
    }
}
