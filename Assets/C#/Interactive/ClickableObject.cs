using UnityEngine;

public class ClickableObject : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private string objectName = "Объект";
    [SerializeField] private float clickCooldown = 1f; // Защита от спама

    [Header("Анимация")]
    [SerializeField] private Animator animator;
    [SerializeField] private string animationTrigger = "Activate";
    [SerializeField] private float delayBeforeNextStage = 1.5f;

    [Header("Визуальные эффекты")]
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private Renderer objectRenderer;

    [Header("Звуки")]
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip completeSound;

    [Header("События")]
    public System.Action OnAnimationStart;
    public System.Action OnAnimationComplete;

    private bool isActive = false;
    private bool isAnimating = false;
    private float lastClickTime = 0f;
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void OnMouseDown()
    {
        // Для 3D объектов с Collider
        if (CanClick())
        {
            OnClick();
        }
    }

    private void OnMouseEnter()
    {
        // Подсветка при наведении
        if (objectRenderer != null && highlightMaterial != null)
        {
            objectRenderer.material = highlightMaterial;
        }
    }

    private void OnMouseExit()
    {
        // Возврат цвета
        if (objectRenderer != null && defaultMaterial != null)
        {
            objectRenderer.material = defaultMaterial;
        }
    }

    private bool CanClick()
    {
        return !isAnimating && Time.time - lastClickTime >= clickCooldown;
    }

    private void OnClick()
    {
        lastClickTime = Time.time;
        isAnimating = true;

        PlaySound(clickSound);
        OnAnimationStart?.Invoke();

        if (animator != null)
        {
            animator.SetTrigger(animationTrigger);
        }

        // Задержка перед второй частью
        Invoke(nameof(OnSecondStage), delayBeforeNextStage);

        Debug.Log($"Клик по объекту: {objectName}");
    }

    private void OnSecondStage()
    {
        PlaySound(completeSound);

        // Здесь можно запустить вторую часть анимации
        if (animator != null)
        {
            animator.SetTrigger("SecondStage");
        }
    }

    public void OnAnimationFinished()
    {
        // Вызывается из Animation Event
        isAnimating = false;
        OnAnimationComplete?.Invoke();
        Debug.Log($"Анимация объекта {objectName} завершена");
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}