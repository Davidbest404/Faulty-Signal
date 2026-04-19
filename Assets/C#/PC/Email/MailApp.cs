using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MailApp : MonoBehaviour
{
    [Header("Секция 1 - Список писем")]
    [SerializeField] private Transform emailListParent;
    [SerializeField] private GameObject emailButtonPrefab;

    [Header("Секция 2 - Содержание письма")]
    [SerializeField] private GameObject contentPanel;
    [SerializeField] private TextMeshProUGUI senderNameText;
    [SerializeField] private TextMeshProUGUI senderEmailText;
    [SerializeField] private TextMeshProUGUI subjectText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TextMeshProUGUI emptyStateText;

    [Header("Данные")]
    [SerializeField] private List<EmailData> allEmails = new List<EmailData>();

    [Header("Визуальные настройки")]
    [SerializeField] private Sprite readBackgroundSprite;
    [SerializeField] private Sprite readIconSprite;
    [SerializeField] private Color readTextColor = Color.gray;
    [SerializeField] private Sprite unreadBackgroundSprite;
    [SerializeField] private Sprite unreadIconSprite;
    [SerializeField] private Color unreadTextColor = Color.white;

    [Header("Настройки появления писем")]
    [SerializeField] private bool spawnEmailsOverTime = true;
    [SerializeField] private bool spawnOnStart = true;

    [Header("WebGL оптимизация")]
    [SerializeField] private int maxEmailsPerFrame = 2;

    [Header("Важно! Курсор")]
    [SerializeField] private MonitorCursor monitorCursor;

    private List<EmailButtonUI> emailButtons = new List<EmailButtonUI>();
    private EmailData currentOpenEmail;
    private List<EmailData> pendingEmails = new List<EmailData>();
    private List<EmailData> spawnedEmails = new List<EmailData>();
    private bool isSpawning = false;
    private Coroutine spawnCoroutine;
    private float gameStartTime;

    public System.Action<EmailData> OnEmailOpened;
    public System.Action<EmailData> OnNewEmailReceived;

    [System.Serializable]
    public class EmailButtonUI
    {
        public GameObject gameObject;
        public RectTransform rectTransform;
        public Image backgroundImage;
        public Image statusIcon;
        public TextMeshProUGUI senderText;
        public TextMeshProUGUI subjectText;
        public TextMeshProUGUI dateText;
        public EmailData emailData;
    }

    private void Start()
    {
        // Запоминаем время старта игры
        gameStartTime = Time.time;

        if (monitorCursor == null)
            monitorCursor = FindObjectOfType<MonitorCursor>();

        if (monitorCursor == null)
        {
            Debug.LogError("MailApp: MonitorCursor не найден!");
            return;
        }

        monitorCursor.OnClickAtPosition += HandleCursorClick;

        // Сбрасываем все письма в непрочитанные
        ResetAllEmailsToUnread();

        // Настраиваем появление писем
        SetupEmailSpawning();

        // Создаём UI для уже появившихся писем
        StartCoroutine(CreateEmailListCoroutine());

        ShowEmptyContent();

        // Запускаем спавн писем
        if (spawnEmailsOverTime && spawnOnStart)
        {
            StartSpawning();
        }
    }

    // Сброс всех писем в непрочитанные (для новой игры)
    private void ResetAllEmailsToUnread()
    {
        foreach (var email in allEmails)
        {
            email.ResetEmail();
        }
        Debug.Log("Все письма сброшены в непрочитанные");
    }

    private void SetupEmailSpawning()
    {
        var sortedEmails = allEmails.OrderBy(e => e.receivedTime).ToList();

        pendingEmails.Clear();
        spawnedEmails.Clear();

        float currentTime = Time.time - gameStartTime;

        foreach (var email in sortedEmails)
        {
            if (email.receivedTime <= currentTime)
            {
                // Письмо уже должно быть доступно
                spawnedEmails.Add(email);
            }
            else
            {
                // Письмо появится позже
                pendingEmails.Add(email);
            }
        }

        Debug.Log($"Уже появилось: {spawnedEmails.Count}, ожидают: {pendingEmails.Count}");
    }

    public void StartSpawning()
    {
        if (isSpawning) return;

        isSpawning = true;
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(SpawnEmailsCoroutine());
    }

    public void StopSpawning()
    {
        isSpawning = false;
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnEmailsCoroutine()
    {
        while (isSpawning && pendingEmails.Count > 0)
        {
            EmailData nextEmail = pendingEmails[0];
            float currentTime = Time.time - gameStartTime;
            float timeUntilSpawn = nextEmail.receivedTime - currentTime;

            if (timeUntilSpawn > 0)
            {
                Debug.Log($"Следующее письмо от {nextEmail.senderName} через {timeUntilSpawn:F1} секунд");
                yield return new WaitForSeconds(timeUntilSpawn);
            }

            SpawnEmail(nextEmail);
            pendingEmails.RemoveAt(0);
            spawnedEmails.Add(nextEmail);

            OnNewEmailReceived?.Invoke(nextEmail);

            // Небольшая задержка между письмами, если они приходят в одно время
            yield return new WaitForSeconds(0.3f);
        }

        Debug.Log("Все письма получены!");
    }

    private void SpawnEmail(EmailData email)
    {
        Debug.Log($"📨 НОВОЕ ПИСЬМО! От: {email.senderName}, Тема: {email.subject}");

        CreateEmailButton(email);
        UpdateUnreadCount();
        StartCoroutine(FlashNewEmailIndicator());
    }

    private IEnumerator FlashNewEmailIndicator()
    {
        var titleText = GetComponentInChildren<TextMeshProUGUI>();
        if (titleText != null && titleText.gameObject.name == "TitleText")
        {
            Color originalColor = titleText.color;
            for (int i = 0; i < 3; i++)
            {
                titleText.color = Color.yellow;
                yield return new WaitForSeconds(0.2f);
                titleText.color = originalColor;
                yield return new WaitForSeconds(0.2f);
            }
        }
    }

    private IEnumerator CreateEmailListCoroutine()
    {
        // Очищаем старые кнопки
        foreach (var emailUI in emailButtons)
        {
            if (emailUI.gameObject != null)
                Destroy(emailUI.gameObject);
        }
        emailButtons.Clear();

        // Создаём кнопки для уже появившихся писем
        var sortedEmails = spawnedEmails.OrderByDescending(e => e.receivedTime).ToList();

        for (int i = 0; i < sortedEmails.Count; i++)
        {
            CreateEmailButton(sortedEmails[i]);

            if (i % maxEmailsPerFrame == 0 && i > 0)
                yield return null;
        }

        UpdateUnreadCount();
    }

    private void CreateEmailButton(EmailData email)
    {
        GameObject buttonObj = Instantiate(emailButtonPrefab, emailListParent);
        EmailButtonUI emailUI = new EmailButtonUI();

        emailUI.gameObject = buttonObj;
        emailUI.rectTransform = buttonObj.GetComponent<RectTransform>();
        emailUI.emailData = email;

        emailUI.backgroundImage = buttonObj.GetComponent<Image>();
        emailUI.statusIcon = buttonObj.transform.Find("StatusIcon")?.GetComponent<Image>();
        emailUI.senderText = buttonObj.transform.Find("SenderText")?.GetComponent<TextMeshProUGUI>();
        emailUI.subjectText = buttonObj.transform.Find("SubjectText")?.GetComponent<TextMeshProUGUI>();
        emailUI.dateText = buttonObj.transform.Find("DateText")?.GetComponent<TextMeshProUGUI>();

        if (emailUI.senderText != null)
            emailUI.senderText.text = email.senderName;

        if (emailUI.subjectText != null)
            emailUI.subjectText.text = email.subject;

        if (emailUI.dateText != null)
            emailUI.dateText.text = GetFormattedDate(email.receivedTime);

        ApplyEmailVisual(emailUI);

        emailButtons.Add(emailUI);

        // Обновляем Layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(emailListParent as RectTransform);
    }

    private void HandleCursorClick(Vector2 clickPosition)
    {
        if (!gameObject.activeInHierarchy) return;

        // Проверяем клик по каждой кнопке
        for (int i = emailButtons.Count - 1; i >= 0; i--)
        {
            var emailUI = emailButtons[i];
            if (emailUI.gameObject != null && emailUI.gameObject.activeInHierarchy)
            {
                if (IsPointOverRectTransform(clickPosition, emailUI.rectTransform))
                {
                    OnEmailClicked(emailUI.emailData);
                    return;
                }
            }
        }
    }

    private bool IsPointOverRectTransform(Vector2 screenPoint, RectTransform rectTransform)
    {
        if (rectTransform == null) return false;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return false;

        Vector2 localPoint;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, cam, out localPoint))
        {
            return rectTransform.rect.Contains(localPoint);
        }

        return false;
    }

    private void OnEmailClicked(EmailData email)
    {
        Debug.Log($"Нажато письмо: {email.subject}, прочитано: {email.isRead}");

        if (currentOpenEmail == email) return;

        // Если письмо непрочитанное - отмечаем как прочитанное
        if (!email.isRead)
        {
            email.isRead = true;
            Debug.Log($"Письмо {email.subject} теперь ПРОЧИТАНО");

            // Обновляем визуал кнопки
            var emailUI = emailButtons.Find(e => e.emailData == email);
            if (emailUI != null)
                ApplyEmailVisual(emailUI);

            UpdateUnreadCount();
        }

        DisplayEmailContent(email);
        currentOpenEmail = email;
        OnEmailOpened?.Invoke(email);
    }

    private void DisplayEmailContent(EmailData email)
    {
        if (contentPanel != null)
            contentPanel.SetActive(true);

        if (emptyStateText != null)
            emptyStateText.gameObject.SetActive(false);

        if (senderNameText != null)
            senderNameText.text = email.senderName;

        if (senderEmailText != null)
            senderEmailText.text = email.senderEmail;

        if (subjectText != null)
            subjectText.text = email.subject;

        if (bodyText != null)
            bodyText.text = email.bodyText;
    }

    private void ShowEmptyContent()
    {
        if (contentPanel != null)
            contentPanel.SetActive(false);

        if (emptyStateText != null)
            emptyStateText.gameObject.SetActive(true);

        currentOpenEmail = null;
    }

    private void ApplyEmailVisual(EmailButtonUI emailUI)
    {
        if (emailUI.emailData.isRead)
        {
            // ПРОЧИТАННОЕ
            if (emailUI.backgroundImage != null && readBackgroundSprite != null)
            {
                emailUI.backgroundImage.sprite = readBackgroundSprite;
                emailUI.backgroundImage.color = Color.white;
            }

            if (emailUI.statusIcon != null && readIconSprite != null)
                emailUI.statusIcon.sprite = readIconSprite;

            if (emailUI.senderText != null)
                emailUI.senderText.color = readTextColor;

            if (emailUI.subjectText != null)
                emailUI.subjectText.color = readTextColor;

            if (emailUI.dateText != null)
                emailUI.dateText.color = readTextColor;
        }
        else
        {
            // НЕПРОЧИТАННОЕ
            if (emailUI.backgroundImage != null && unreadBackgroundSprite != null)
            {
                emailUI.backgroundImage.sprite = unreadBackgroundSprite;
                emailUI.backgroundImage.color = Color.white;
            }

            if (emailUI.statusIcon != null && unreadIconSprite != null)
                emailUI.statusIcon.sprite = unreadIconSprite;

            if (emailUI.senderText != null)
                emailUI.senderText.color = unreadTextColor;

            if (emailUI.subjectText != null)
                emailUI.subjectText.color = unreadTextColor;

            if (emailUI.dateText != null)
                emailUI.dateText.color = unreadTextColor;
        }
    }

    private void UpdateUnreadCount()
    {
        int unreadCount = spawnedEmails.Count(e => !e.isRead);
        var titleText = GetComponentInChildren<TextMeshProUGUI>();
        if (titleText != null && titleText.gameObject.name == "TitleText")
        {
            titleText.text = unreadCount > 0 ? $"Почта ({unreadCount})" : "Почта";
        }
    }

    private string GetFormattedDate(float receivedTime)
    {
        // Форматируем время в минутах:секундах
        int minutes = Mathf.FloorToInt(receivedTime / 60);
        int seconds = Mathf.FloorToInt(receivedTime % 60);

        if (minutes > 0)
            return $"{minutes} мин назад";
        else
            return $"{seconds} сек назад";
    }

    public int GetUnreadCount()
    {
        return spawnedEmails.Count(e => !e.isRead);
    }

    // Метод для полного сброса (для новой игры)
    public void ResetForNewGame()
    {
        // Останавливаем спавн
        StopSpawning();

        // Сбрасываем время
        gameStartTime = Time.time;

        // Сбрасываем состояние писем
        ResetAllEmailsToUnread();

        // Очищаем списки
        pendingEmails.Clear();
        spawnedEmails.Clear();

        // Заново настраиваем спавн
        SetupEmailSpawning();

        // Пересоздаём UI
        StartCoroutine(CreateEmailListCoroutine());

        // Закрываем открытое письмо
        ShowEmptyContent();

        // Запускаем спавн заново
        if (spawnEmailsOverTime)
        {
            StartSpawning();
        }

        Debug.Log("Почта сброшена для новой игры!");
    }

    private void OnDestroy()
    {
        if (monitorCursor != null)
            monitorCursor.OnClickAtPosition -= HandleCursorClick;
    }
}