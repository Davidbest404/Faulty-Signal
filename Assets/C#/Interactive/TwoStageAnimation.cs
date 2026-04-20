using UnityEngine;
using UnityEngine.EventSystems;

public class TwoStageAnimation : MonoBehaviour, IPointerClickHandler
{
    [Header("Настройки анимации")]
    [SerializeField] private Animator targetAnimator;
    [SerializeField] private string firstStageTrigger = "Start";
    [SerializeField] private string secondStageTrigger = "Complete";

    [Header("Временные настройки")]
    [SerializeField] private float delayBeforeSecondStage = 2f;
    [SerializeField] private bool autoPlaySecondStage = true;

    [Header("Визуальные эффекты")]
    [SerializeField] private GameObject clickIndicator;    // Визуальный отклик на клик
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip stageCompleteSound;

    [Header("События")]
    [SerializeField] private UnityEngine.Events.UnityEvent onFirstStageStarted;
    [SerializeField] private UnityEngine.Events.UnityEvent onSecondStageStarted;
    [SerializeField] private UnityEngine.Events.UnityEvent onAnimationComplete;

    private bool isFirstStagePlaying = false;
    private bool isSecondStagePlaying = false;
    private float timer = 0f;
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (clickIndicator != null)
            clickIndicator.SetActive(false);
    }

    private void Update()
    {
        if (isFirstStagePlaying && !isSecondStagePlaying && autoPlaySecondStage)
        {
            timer += Time.deltaTime;
            if (timer >= delayBeforeSecondStage)
            {
                PlaySecondStage();
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Обработка клика реальным курсором
        if (!isFirstStagePlaying && !isSecondStagePlaying)
        {
            PlayFirstStage();
        }
        else if (isFirstStagePlaying && !isSecondStagePlaying)
        {
            // Можно пропустить ожидание и сразу начать вторую часть
            PlaySecondStage();
        }
    }

    private void PlayFirstStage()
    {
        isFirstStagePlaying = true;
        timer = 0f;

        // Визуальный отклик
        if (clickIndicator != null)
        {
            clickIndicator.SetActive(true);
            Invoke(nameof(HideClickIndicator), 0.3f);
        }

        // Звук
        PlaySound(clickSound);

        // Запуск анимации
        if (targetAnimator != null)
        {
            targetAnimator.SetTrigger(firstStageTrigger);
        }

        onFirstStageStarted?.Invoke();
        Debug.Log("Первая стадия анимации начата");
    }

    private void PlaySecondStage()
    {
        if (isSecondStagePlaying) return;

        isSecondStagePlaying = true;

        // Звук завершения первой части
        PlaySound(stageCompleteSound);

        // Запуск второй части анимации
        if (targetAnimator != null)
        {
            targetAnimator.SetTrigger(secondStageTrigger);
        }

        onSecondStageStarted?.Invoke();
        Debug.Log("Вторая стадия анимации начата");
    }

    public void OnAnimationComplete()
    {
        // Вызывается из Animation Event или по таймеру
        isFirstStagePlaying = false;
        isSecondStagePlaying = false;
        onAnimationComplete?.Invoke();
        Debug.Log("Анимация полностью завершена");
    }

    private void HideClickIndicator()
    {
        if (clickIndicator != null)
            clickIndicator.SetActive(false);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Публичные методы для вызова из Animation Events
    public void SkipToSecondStage()
    {
        if (isFirstStagePlaying && !isSecondStagePlaying)
        {
            PlaySecondStage();
        }
    }

    public void ResetAnimation()
    {
        isFirstStagePlaying = false;
        isSecondStagePlaying = false;
        timer = 0f;

        if (targetAnimator != null)
        {
            targetAnimator.Rebind();
        }
    }
}