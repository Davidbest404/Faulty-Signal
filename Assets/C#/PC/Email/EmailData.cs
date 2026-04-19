using UnityEngine;

[CreateAssetMenu(fileName = "NewEmail", menuName = "Mail/EmailData")]
public class EmailData : ScriptableObject
{
    public string emailId;
    public string senderName;
    public string senderEmail;
    public string subject;
    [TextArea(3, 10)]
    public string bodyText;
    public float receivedTime;  // ¬рем€ по€влени€ письма (в секундах от старта)

    // ѕростое поле - не сохран€етс€ между запусками
    public bool isRead;

    // —брос состо€ни€ (дл€ новой игры)
    public void ResetEmail()
    {
        isRead = false;
    }
}