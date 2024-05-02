using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChatBot : MonoBehaviour
{
    
    private TMPro.TextMeshProUGUI chatBotDisplay;

    // Start is called before the first frame update
    void Start()
    {
        chatBotDisplay = GameObject.Find("Canvas").transform.Find("ChatBotResponse").GetComponent<TMPro.TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        

        if (Input.anyKeyDown)
        {
            string keyPressed = Input.inputString;

            chatBotDisplay.text = "Key pressed: " + keyPressed;
        }
    }
}
