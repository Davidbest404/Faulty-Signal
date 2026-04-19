using UnityEngine;
using UnityEngine.UI;

public class ClickToDisableTarget : MonoBehaviour
{
    [Header("Целевой объект")]
    [SerializeField] private GameObject targetObject;        // Какой объект отключать/уничтожать
    [SerializeField] private bool disableTargetOnClick = true;   // Отключать цель при клике
    [SerializeField] private bool destroyTargetOnClick = false;  // Уничтожить цель при клике

    [Header("Настройки")]
    [SerializeField] private bool requireSpecificCursor = true;
    [SerializeField] private bool disableSelfAfterClick = false;  // Отключить и сам скрипт после клика

    [Header("Визуальный фидбек (для этого объекта)")]
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private float hoverScale = 1.05f;

    [Header("Звук")]
    [SerializeField] private AudioClip clickSound;

    [Header("Ссылки")]
    [SerializeField] private MonitorCursor monitorCursor;

    private Color originalColor;
    private Vector3 originalScale;
    private Image cachedImage;
    private SpriteRenderer cachedSpriteRenderer;
    private bool isHovered = false;

    private void Start()
    {
        // Ищем скрипт курсора если не назначен
        if (monitorCursor == null)
            monitorCursor = FindObjectOfType<MonitorCursor>();

        // Сохраняем оригинальные значения для визуального фидбека
        originalScale = transform.localScale;

        cachedImage = GetComponent<Image>();
        if (cachedImage != null)
            originalColor = cachedImage.color;

        if (cachedImage == null)
        {
            cachedSpriteRenderer = GetComponent<SpriteRenderer>();
            if (cachedSpriteRenderer != null)
                originalColor = cachedSpriteRenderer.color;
        }

        // Подписываемся на клик курсора
        if (monitorCursor != null)
        {
            monitorCursor.OnClickAtPosition += HandleClick;
        }
        else
        {
            Debug.LogWarning("MonitorCursor не найден!");
        }
    }

    private void Update()
    {
        // Проверка наведения для визуального фидбека
        if (monitorCursor != null && (hoverColor != originalColor || hoverScale != 1f))
        {
            CheckHover();
        }
    }

    private void CheckHover()
    {
        Vector2 cursorPos = monitorCursor.GetTopLeftCornerPosition();
        bool isOver = IsPointOverObject(cursorPos);

        if (isOver && !isHovered)
        {
            isHovered = true;
            OnHoverStart();
        }
        else if (!isOver && isHovered)
        {
            isHovered = false;
            OnHoverEnd();
        }
    }

    private void OnHoverStart()
    {
        if (cachedImage != null)
            cachedImage.color = hoverColor;
        else if (cachedSpriteRenderer != null)
            cachedSpriteRenderer.color = hoverColor;

        transform.localScale = originalScale * hoverScale;
    }

    private void OnHoverEnd()
    {
        if (cachedImage != null)
            cachedImage.color = originalColor;
        else if (cachedSpriteRenderer != null)
            cachedSpriteRenderer.color = originalColor;

        transform.localScale = originalScale;
    }

    private void HandleClick(Vector2 clickPosition)
    {
        // Проверяем, нажали ли на ЭТОТ объект (кнопку/иконку)
        if (IsPointOverObject(clickPosition))
        {
            OnThisObjectClicked();
        }
    }

    private bool IsPointOverObject(Vector2 point)
    {
        // Для UI элементов
        if (cachedImage != null && cachedImage.canvas != null)
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector2 localPoint;
                Camera cam = cachedImage.canvas.worldCamera;

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform, point, cam, out localPoint))
                {
                    return rectTransform.rect.Contains(localPoint);
                }
            }
        }

        // Для обычных объектов в мире
        if (cachedSpriteRenderer != null)
        {
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(new Vector3(point.x, point.y, 10));
            Bounds bounds = cachedSpriteRenderer.bounds;
            return bounds.Contains(worldPoint);
        }

        // Для Collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(new Vector3(point.x, point.y, 10));
            return col.bounds.Contains(worldPoint);
        }

        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null)
        {
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(new Vector3(point.x, point.y, 10));
            return col2D.bounds.Contains(worldPoint);
        }

        return false;
    }

    private void OnThisObjectClicked()
    {
        Debug.Log($"Нажат объект-триггер: {gameObject.name}");

        // Воспроизводим звук
        if (clickSound != null)
        {
            AudioSource.PlayClipAtPoint(clickSound, Camera.main.transform.position);
        }

        // Вызываем событие
        OnClick?.Invoke(targetObject);

        // ОТКЛЮЧАЕМ ИЛИ УНИЧТОЖАЕМ ЦЕЛЕВОЙ ОБЪЕКТ (не этот!)
        if (targetObject != null)
        {
            if (destroyTargetOnClick)
            {
                Destroy(targetObject);
                Debug.Log($"Уничтожен целевой объект: {targetObject.name}");
            }
            else if (disableTargetOnClick)
            {
                targetObject.SetActive(false);
                Debug.Log($"Отключён целевой объект: {targetObject.name}");
            }
        }
        else
        {
            Debug.LogWarning("Целевой объект не назначен в инспекторе!");
        }

        // Отключаем сам скрипт (и визуал) после клика
        if (disableSelfAfterClick)
        {
            // Отключаем визуал
            if (cachedImage != null)
                cachedImage.raycastTarget = false;

            // Отключаем компонент
            enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (monitorCursor != null)
            monitorCursor.OnClickAtPosition -= HandleClick;
    }

    public System.Action<GameObject> OnClick;

    // Публичный метод для смены цели динамически
    public void SetTargetObject(GameObject newTarget)
    {
        targetObject = newTarget;
    }
}