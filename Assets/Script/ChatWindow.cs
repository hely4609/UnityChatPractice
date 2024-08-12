using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ChatWindow : MonoBehaviour
{
    [SerializeField] protected TextMeshProUGUI textUI;
    [SerializeField] protected TMP_InputField inputUI;
    void Start()
    {
        Client.RecieveChat += (message) => { ReceieveChat(message); };
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SendChat(string message)
    {
        Client.ClaimChat(message);
        // Ä­ Áö¿ì±â.
        inputUI.SetTextWithoutNotify("");
    }

    protected void ReceieveChat(string message)
    {
        textUI.text += $"{message}\n";
    }
}
