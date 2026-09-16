using UnityEngine;

public class HistoricalFigure : MonoBehaviour
{
    public string characterName = "Historical Figure";

    private bool conversationStarted = false;

    public void StartConversation()
    {
        if (conversationStarted)
        {
            Debug.Log(characterName + " is already talking to you.");
            return;
        }

        conversationStarted = true;

        Debug.Log(characterName + ": Good evening. I'll have a beer, please.");
    }
}
