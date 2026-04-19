using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIHoverFader : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Target Settings")]
    [Tooltip("UI элемент, прозрачность которого будем менять. Если не указан - используется этот объект")]
    [SerializeField] private Graphic targetGraphic;

    [Header("Alpha Values")]
    [Tooltip("Прозрачность когда курсор НЕ наведён")]
    [SerializeField][Range(0f, 1f)] private float alphaHidden = 0f;

    [Tooltip("Прозрачность при наведении")]
    [SerializeField][Range(0f, 1f)] private float alphaVisible = 1f;

    [Header("Animation Speed")]
    [Tooltip("Скорость появления (при наведении)")]
    [SerializeField] private float fadeInSpeed = 5f;

    [Tooltip("Скорость исчезновения (при уходе курсора)")]
    [SerializeField] private float fadeOutSpeed = 5f;

    [Header("Initial State")]
    [Tooltip("Начинать с прозрачным объектом?")]
    [SerializeField] private bool startHidden = true;

    // Текущая прозрачность
    private float currentAlpha;
    // Целевая прозрачность
    private float targetAlpha;
    // Кэш CanvasRenderer для производительности
    private CanvasRenderer canvasRenderer;

    private void Awake()
    {
        // Если целевой объект не указан - используем текущий
        if (targetGraphic == null)
            targetGraphic = GetComponent<Graphic>();

        if (targetGraphic == null)
        {
            Debug.LogError($"UIHoverFader: На объекте {gameObject.name} нет Graphic компонента (Image/Text/RawImage)!");
            enabled = false;
            return;
        }

        canvasRenderer = targetGraphic.canvasRenderer;
    }

    private void Start()
    {
        // Устанавливаем начальное состояние
        if (startHidden)
        {
            currentAlpha = alphaHidden;
            targetAlpha = alphaHidden;
        }
        else
        {
            currentAlpha = alphaVisible;
            targetAlpha = alphaVisible;
        }

        ApplyAlpha(currentAlpha);
    }

    private void Update()
    {
        // Плавно меняем текущую прозрачность к целевой
        float speed = (targetAlpha > currentAlpha) ? fadeInSpeed : fadeOutSpeed;
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, speed * Time.deltaTime);

        // Применяем прозрачность
        if (Mathf.Abs(currentAlpha - targetAlpha) > 0.01f)
            ApplyAlpha(currentAlpha);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetAlpha = alphaVisible;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetAlpha = alphaHidden;
    }

    private void ApplyAlpha(float alpha)
    {
        Color color = targetGraphic.color;
        color.a = alpha;
        targetGraphic.color = color;
    }

    // Публичные методы для вызова из других скриптов (опционально)
    public void Show()
    {
        targetAlpha = alphaVisible;
    }

    public void Hide()
    {
        targetAlpha = alphaHidden;
    }

    public void SetAlphaImmediate(float alpha)
    {
        currentAlpha = alpha;
        targetAlpha = alpha;
        ApplyAlpha(alpha);
    }
}