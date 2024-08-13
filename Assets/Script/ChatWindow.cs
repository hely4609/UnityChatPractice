using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatWindow : MonoBehaviour
{
    [SerializeField] protected TextMeshProUGUI textUI;
    [SerializeField] protected TMP_InputField inputUI;
    [SerializeField] protected Image myPick;
    public Image MyPick { get { return myPick; } }  
    [SerializeField] protected Image hostPick;
    public Image HostPick { get { return hostPick; } }  
    void Start()
    {
        Client.RecieveChat += (message) => { ReceieveChat(message); };
    }

    public void SendChat(string message)
    {
        Client.ClaimChat(message);
        // Ä­ Áö¿ì±â.
        inputUI.SetTextWithoutNotify("");
    }

    public void ReceieveChat(string message)
    {
        textUI.text += $"{message}\n";
    }
}
