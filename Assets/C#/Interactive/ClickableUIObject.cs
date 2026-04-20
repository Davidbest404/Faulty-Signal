using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ClickableUIObject : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Настройки")]
    [SerializeField] private string objectName = "UI Элемент";
    [SerializeField] private Animator animator;
    [SerializeField] private string firstStageTrigger = "Stage1";
    [SerializeField] private string secondStageTrigger = "Stage2";

    [Header("Временные настройки")]
    [SerializeField] private float delayBeforeSecondStage = 2f;
    [SerializeField] private bool autoStartSecondStage = true;

    [Header("Визуальные эффекты")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Image targetImage;
    [SerializeField] private GameObject clickEffectPrefab;

    [Header("Звуки")]
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip completeSound;

    private bool isFirstStage = false;
    private bool isSecondStage = false;
    private float timer = 0f;
    private AudioSource audioSource;

    public System.Action OnFirstStageComplete;
    public System.Action OnSecondStageComplete;

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    private void Update()
    {
        if (isFirstStage && !isSecondStage && autoStartSecondStage)
        {
            timer += Time.deltaTime;
            if (timer >= delayBeforeSecondStage)
            {
                StartSecondStage();
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isFirstStage && !isSecondStage)
        {
            StartFirstStage();
        }
        else if (isFirstStage && !isSecondStage)
        {
            // Пропустить ожидание
            StartSecondStage();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetImage != null && !isFirstStage && !isSecondStage)
        {
            targetImage.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (targetImage != null && !isFirstStage && !isSecondStage)
        {
            targetImage.color = normalColor;
        }
    }

    private void StartFirstStage()
    {
        isFirstStage = true;
        timer = 0f;

        PlaySound(clickSound);
        ShowClickEffect();

        if (animator != null)
        {
            animator.SetTrigger(firstStageTrigger);
        }

        if (targetImage != null)
        {
            targetImage.color = normalColor;
        }

        Debug.Log($"Первая стадия: {objectName}");
    }

    private void StartSecondStage()
    {
        if (isSecondStage) return;

        isSecondStage = true;
        PlaySound(completeSound);

        if (animator != null)
        {
            animator.SetTrigger(secondStageTrigger);
        }

        OnFirstStageComplete?.Invoke();
        Debug.Log($"Вторая стадия: {objectName}");
    }

    public void OnAnimationComplete()
    {
        isFirstStage = false;
        isSecondStage = false;
        OnSecondStageComplete?.Invoke();
        Debug.Log($"Анимация завершена: {objectName}");
    }

    private void ShowClickEffect()
    {
        if (clickEffectPrefab != null)
        {
            Vector3 clickPos = Input.mousePosition;
            clickPos.z = 10f;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(clickPos);

            GameObject effect = Instantiate(clickEffectPrefab, worldPos, Quaternion.identity);
            Destroy(effect, 0.5f);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}