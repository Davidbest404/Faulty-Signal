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

    [Header("Force Settings")]
    [Tooltip("Принудительно отключить (игнорировать наведение)")]
    [SerializeField] private bool forceDisabled = false;

    private float currentAlpha;
    private float targetAlpha;
    private CanvasRenderer canvasRenderer;
    private bool isHovered = false;

    private void Awake()
    {
        if (targetGraphic == null)
            targetGraphic = GetComponent<Graphic>();

        if (targetGraphic == null)
        {
            Debug.LogError($"UIHoverFader: На объекте {gameObject.name} нет Graphic компонента!");
            enabled = false;
            return;
        }

        canvasRenderer = targetGraphic.canvasRenderer;
    }

    private void Start()
    {
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
        if (forceDisabled) return;

        // Обновляем целевую прозрачность в зависимости от состояния наведения
        if (isHovered)
            targetAlpha = alphaVisible;
        else
            targetAlpha = alphaHidden;

        float speed = (targetAlpha > currentAlpha) ? fadeInSpeed : fadeOutSpeed;
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, speed * Time.deltaTime);

        if (Mathf.Abs(currentAlpha - targetAlpha) > 0.01f)
            ApplyAlpha(currentAlpha);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (forceDisabled) return;
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (forceDisabled) return;
        isHovered = false;
    }

    private void ApplyAlpha(float alpha)
    {
        Color color = targetGraphic.color;
        color.a = alpha;
        targetGraphic.color = color;
    }

    // Публичные методы
    public void Show()
    {
        if (forceDisabled) return;
        targetAlpha = alphaVisible;
        isHovered = true;
    }

    public void Hide()
    {
        if (forceDisabled) return;
        targetAlpha = alphaHidden;
        isHovered = false;
    }

    public void SetAlphaImmediate(float alpha)
    {
        currentAlpha = alpha;
        targetAlpha = alpha;
        ApplyAlpha(alpha);
    }

    public void SetForceDisabled(bool disabled)
    {
        forceDisabled = disabled;
        if (disabled)
        {
            SetAlphaImmediate(0f);
        }
        else
        {
            SetAlphaImmediate(alphaHidden);
        }
    }
}