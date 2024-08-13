using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameRule : MonoBehaviour
{
    [SerializeField] ChatWindow displayWindow;
    public static int CheckRPS(RPS hostRPS, RPS guestRPS)
    {
        int result = 2;
        // guest±‚¡ÿ
        // 0 = ∫Ò±Ë, -1 = ¡¸, 1 = ¿Ã±Ë
        switch(hostRPS)
        {
            case RPS.Rock:
                if (guestRPS == RPS.Rock) result = 0;
                else if(guestRPS == RPS.Scissors) result = -1;
                else if(guestRPS == RPS.Paper) result = 1;
                break;
            case RPS.Scissors:
                if (guestRPS == RPS.Rock) result = 1;
                else if (guestRPS == RPS.Scissors) result = 0;
                else if (guestRPS == RPS.Paper) result = -1;
                break;
            case RPS.Paper:
                if (guestRPS == RPS.Rock) result = -1;
                else if (guestRPS == RPS.Scissors) result = 1;
                else if (guestRPS == RPS.Paper) result = 0;
                break;
        }
        return result;
    }

    public void RockButton()
    {
        Client.ClaimRPS(RPS.Rock);

        displayWindow.MyPick.sprite = Resources.Load<Sprite>("Sprite/rock");
        Debug.Log("¡÷∏‘");

    }
    public void PaperButton()
    {
        Client.ClaimRPS(RPS.Paper);
        displayWindow.MyPick.sprite = Resources.Load<Sprite>("Sprite/paper");

        Debug.Log("∫∏");

    }
    public void ScissorsButton()
    {
        Client.ClaimRPS(RPS.Scissors);
        displayWindow.MyPick.sprite = Resources.Load<Sprite>("Sprite/scissors");

        Debug.Log("∞°¿ß");
    }

}
