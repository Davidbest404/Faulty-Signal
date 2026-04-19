using UnityEngine;
using UnityEngine.UI;

public class MailNotificationBadge : MonoBehaviour
{
    [SerializeField] private MailApp mailApp;
    [SerializeField] private Text badgeText;
    [SerializeField] private GameObject badgeIcon;

    private int lastUnreadCount = -1;

    private void Start()
    {
        if (mailApp == null)
            mailApp = FindObjectOfType<MailApp>();

        if (mailApp != null)
        {
            mailApp.OnEmailOpened += (email) => UpdateBadge();
            UpdateBadge();
        }
    }

    private void UpdateBadge()
    {
        // Получаем количество непрочитанных (нужно добавить метод в MailApp)
        // Или храните счётчик отдельно
    }
}