using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;

/// <summary>
/// Скрипт "Мужчина за окном" (Брухх).
/// - Не может атаковать пока жалюзь закрыты/закрываются.
/// - Через 5 секунд закрытых жалюзь — исчезает.
/// </summary>
public class WindowMan : MonoBehaviour
{
    [Header("Настройки атаки")]
    [Tooltip("Сколько секунд у игрока, чтобы закрыть жалюзь.")]
    [SerializeField] private float attackTimeLimit = 15f;

    [Tooltip("Через сколько секунд закрытых жалюзь монстр исчезает.")]
    [SerializeField] private float curtainDefeatDelay = 5f;

    [Header("Ссылки")]
    [Tooltip("Скрипт жалюзь.")]
    [SerializeField] private CurtainController curtainController;

    [Header("Анимации")]
    [Tooltip("Animator на самом бруххе.")]
    [SerializeField] private Animator bruhhAnimator;
    [Tooltip("Animator на Cube.074.")]
    [SerializeField] private Animator windowAnimator;

    [Header("Звуки")]
    [Tooltip("Звук появления (zvuki-shagi_uZ54dyh0).")]
    [SerializeField] private AudioClip appearanceSound;
    private AudioSource audioSource;

    [Header("Вызов методов")]
    [SerializeField] private UnityEvent methodsToCall;

    // --- Состояние ---
    private bool isAttacking = false;
    private float attackTimer = 0f;

    // Таймер для 5-секундного отступления при закрытых жалюзь
    private float curtainClosedTimer = 0f;
    private bool isCounting = false;   // Считаем ли мы 5 сек прямо сейчас

    private void Start()
    {
        SetVisible(false);

        if (curtainController == null)
            curtainController = FindFirstObjectByType<CurtainController>();

        if (bruhhAnimator == null)
            bruhhAnimator = GetComponent<Animator>();

        if (windowAnimator == null)
        {
            GameObject windowObj = GameObject.Find("Cube.074");
            if (windowObj != null) windowAnimator = windowObj.GetComponent<Animator>();
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Оставляем аниматор включенным, чтобы работало состояние New State
        if (windowAnimator != null) windowAnimator.enabled = true;
    }

    private void Update()
    {
        // ЗАЩИТА: Пока жалюзь закрываются или закрыты — враг не может атаковать
        bool curtainBlocking = curtainController != null && curtainController.IsClosing;

        if (!isAttacking)
        {
            // Сброс таймера ожидания если он есть
            curtainClosedTimer = 0f;
            isCounting = false;
            return;
        }

        // --- Логика 5-секундного исчезновения ---
        if (curtainBlocking)
        {
            if (!isCounting)
            {
                isCounting = true;
                curtainClosedTimer = 0f;
                Debug.Log("[WindowMan] Жалюзь закрываются! Начинаю отсчёт 5 сек до исчезновения...");
            }

            curtainClosedTimer += Time.deltaTime;

            if (curtainClosedTimer >= curtainDefeatDelay)
            {
                Defeat();
                return;
            }

            // Пока жалюзь закрыты — пауза на таймер атаки (не проигрываем)
            return;
        }
        else
        {
            // Открыли жалюзь — сбрасываем таймер отсчёта
            if (isCounting)
            {
                isCounting = false;
                curtainClosedTimer = 0f;
                Debug.Log("[WindowMan] Жалюзь снова открыты! Таймер исчезновения сброшен.");
            }
        }

        // --- Обычный таймер атаки ---
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            PlayerLoses();
        }
    }

    public void StartAttack()
    {
        if (isAttacking) return;

        // ЗАЩИТА: Не начинаем атаку если жалюзь закрыты/закрываются
        if (curtainController != null && curtainController.IsClosing)
        {
            Debug.Log("[WindowMan] Попытка атаки заблокирована — жалюзь закрыты.");
            return;
        }

        isAttacking = true;
        attackTimer = attackTimeLimit;
        curtainClosedTimer = 0f;
        isCounting = false;
        Debug.Log("[WindowMan] Брухх появился!");

        // Звук шагов
        if (audioSource != null && appearanceSound != null)
            audioSource.PlayOneShot(appearanceSound);

        // Анимации
        if (bruhhAnimator != null)
            bruhhAnimator.Play("Window Bruh");

        if (windowAnimator != null)
        {
            windowAnimator.enabled = true;
            windowAnimator.Play("Windows open");
        }

        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }

    private void Defeat()
    {
        isAttacking = false;
        isCounting = false;
        curtainClosedTimer = 0f;
        Debug.Log("[WindowMan] Брухх испугался жалюзь и убежал!");

        StopAllCoroutines();
        StartCoroutine(FadeOut());
    }

    private void PlayerLoses()
    {
        isAttacking = false;
        Debug.Log("[WindowMan] Игрок не успел закрыть жалюзь! Загрузка GameOver...");
        methodsToCall?.Invoke();
    }

    private IEnumerator FadeIn()
    {
        SetVisible(true);
        yield return null;
    }

    private IEnumerator FadeOut()
    {
        // Возвращаем окно в дефолтное (закрытое) состояние
        if (windowAnimator != null)
        {
            windowAnimator.Play("New State");
            // Мы больше НЕ выключаем аниматор, чтобы Unity сама вернула окно
            // в исходное положение за счет пустой анимации "New State".
        }

        yield return new WaitForSeconds(0.5f);
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            // Не трогаем рендерер самого окна, если оно случайно оказалось в детях
            if (windowAnimator != null && r.gameObject == windowAnimator.gameObject)
                continue;

            r.enabled = visible;
        }
    }

    public bool IsAttacking => isAttacking;
}
