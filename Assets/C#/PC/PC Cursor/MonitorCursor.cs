using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MonitorCursor : MonoBehaviour
{
    [Header("Настройки курсора")]
    [SerializeField] private Canvas targetCanvas;        // Canvas монитора
    [SerializeField] private RectTransform cursorRect;    // RectTransform курсора
    [SerializeField] private float mouseSensitivity = 1f; // Чувствительность

    [Header("Состояние")]
    [SerializeField] private bool isCursorActive = false; // Включен ли курсор

    // Границы движения (в пикселях Canvas)
    private Vector2 minBounds;
    private Vector2 maxBounds;

    // Текущая позиция курсора в локальных координатах Canvas
    private Vector2 currentCursorPosition;

    // Событие клика (верхний левый угол)
    public System.Action<Vector2> OnClickAtPosition;

    private void Start()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();

        if (cursorRect == null)
            cursorRect = GetComponent<RectTransform>();

        CalculateBounds();

        // Стартовая позиция - центр Canvas
        currentCursorPosition = (minBounds + maxBounds) / 2f;
        UpdateCursorVisual();
    }

    private void Update()
    {
        if (!isCursorActive) return;

        // Получаем движение мыши
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Обновляем позицию
        Vector2 newPosition = currentCursorPosition + new Vector2(mouseX, mouseY);
        currentCursorPosition = ClampToBounds(newPosition);

        UpdateCursorVisual();

        // Обработка клика
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    // Вычисление границ Canvas
    private void CalculateBounds()
    {
        Rect canvasRect = targetCanvas.GetComponent<RectTransform>().rect;

        // Получаем размеры курсора
        Vector2 cursorSize = cursorRect.rect.size;

        // Границы: от левого/верхнего края до правого/нижнего с учетом размера курсора
        // Верхний левый угол курсора — это pivot (обычно (0,1) для верхнего левого угла)
        Vector2 pivot = cursorRect.pivot;

        minBounds = new Vector2(
            -canvasRect.width * pivot.x,
            -canvasRect.height * (1 - pivot.y)
        );

        maxBounds = new Vector2(
            canvasRect.width * (1 - pivot.x),
            canvasRect.height * pivot.y
        );
    }

    // Ограничение позиции в пределах Canvas
    private Vector2 ClampToBounds(Vector2 position)
    {
        return new Vector2(
            Mathf.Clamp(position.x, minBounds.x, maxBounds.x),
            Mathf.Clamp(position.y, minBounds.y, maxBounds.y)
        );
    }

    // Обновление визуальной позиции курсора
    private void UpdateCursorVisual()
    {
        cursorRect.anchoredPosition = currentCursorPosition;
    }

    // Обработка клика (позиция верхнего левого угла курсора)
    private void HandleClick()
    {
        // Получаем позицию верхнего левого угла курсора в мировых координатах Canvas
        Vector2 topLeftCorner = currentCursorPosition;

        // Если нужно проверить, попал ли клик на какой-либо UI элемент
        // (например, кнопку на Canvas)
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = RectTransformUtility.WorldToScreenPoint(
            targetCanvas.worldCamera,
            cursorRect.TransformPoint(topLeftCorner)
        );

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        // Вызываем событие с позицией клика
        OnClickAtPosition?.Invoke(topLeftCorner);

        // Дополнительно: можно проверить, что именно было нажато
        foreach (var result in results)
        {
            Button button = result.gameObject.GetComponent<Button>();
            if (button != null && button.interactable)
            {
                // Симулируем нажатие на кнопку
                button.onClick.Invoke();
                Debug.Log($"Клик по кнопке: {button.name}");
                break;
            }
        }
    }

    // Публичные методы для управления извне
    public void SetCursorActive(bool active)
    {
        isCursorActive = active;
        cursorRect.gameObject.SetActive(active);

        if (active)
        {
            // При активации курсора скрываем системный
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void SetCursorPosition(Vector2 position)
    {
        currentCursorPosition = ClampToBounds(position);
        UpdateCursorVisual();
    }

    public Vector2 GetCursorPosition()
    {
        return currentCursorPosition;
    }

    // Получить позицию верхнего левого угла (для проверок)
    public Vector2 GetTopLeftCornerPosition()
    {
        return currentCursorPosition;
    }

    // Проверка, находится ли верхний левый угол курсора над определённым UI элементом
    public bool IsTopLeftCornerOverUI(GameObject uiElement)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = RectTransformUtility.WorldToScreenPoint(
            targetCanvas.worldCamera,
            cursorRect.TransformPoint(currentCursorPosition)
        );

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            if (result.gameObject == uiElement)
                return true;
        }
        return false;
    }
}