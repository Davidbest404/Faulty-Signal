using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class LoadingWindow : MonoBehaviour
{
    [Header("Настройки окна")]
    [SerializeField] private GameObject loadingWindow;        // Само окно загрузки
    [SerializeField] private Transform spawnParent;           // Панель, где спавнятся префабы
    [SerializeField] private GameObject[] loadingPrefabs;     // Массив префабов для спавна

    [Header("Настройки времени")]
    [SerializeField] private float minTotalTime = 3f;         // Минимальное общее время загрузки
    [SerializeField] private float maxTotalTime = 8f;         // Максимальное общее время загрузки

    [Header("Настройки спавна")]
    [SerializeField] private float minSpawnInterval = 0.1f;    // Минимальный интервал между спавном
    [SerializeField] private float maxSpawnInterval = 0.8f;    // Максимальный интервал между спавном
    [SerializeField] private bool useCurveForSpawning = true;  // Использовать кривую (в начале быстро, в конце медленно)
    [SerializeField] private AnimationCurve spawnSpeedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Визуальные эффекты")]
    [SerializeField] private bool fadeOutOnComplete = true;    // Затухание при завершении
    [SerializeField] private float fadeDuration = 0.5f;        // Длительность затухания
    [SerializeField] private CanvasGroup canvasGroup;          // Для затухания (опционально)

    [Header("Звуки")]
    [SerializeField] private AudioClip spawnSound;             // Звук при спавне
    [SerializeField] private AudioClip completeSound;          // Звук завершения

    [Header("События")]
    [SerializeField] public UnityEngine.Events.UnityEvent onLoadingComplete;

    private float totalLoadTime;
    private float elapsedTime = 0f;
    private float nextSpawnTime = 0f;
    private bool isLoading = false;
    private AudioSource audioSource;
    private List<GameObject> spawnedObjects = new List<GameObject>();

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        // Скрываем окно при старте
        if (loadingWindow != null)
            loadingWindow.SetActive(false);
    }

    /// <summary>
    /// Запустить процесс загрузки
    /// </summary>
    public void StartLoading()
    {
        if (isLoading) return;

        // Показываем окно
        if (loadingWindow != null)
            loadingWindow.SetActive(true);

        // Сбрасываем состояние
        ClearAllSpawnedObjects();
        elapsedTime = 0f;

        // Выбираем случайное общее время загрузки
        totalLoadTime = Random.Range(minTotalTime, maxTotalTime);

        // Рассчитываем время первого спавна (быстрый старт)
        nextSpawnTime = GetSpawnInterval(0f);

        isLoading = true;

        Debug.Log($"Загрузка начата. Общее время: {totalLoadTime:F2} секунд");

        StartCoroutine(LoadingProcess());
    }

    private IEnumerator LoadingProcess()
    {
        while (elapsedTime < totalLoadTime)
        {
            elapsedTime += Time.deltaTime;

            // Проверяем, пора ли спавнить новый префаб
            if (elapsedTime >= nextSpawnTime)
            {
                SpawnRandomPrefab();

                // Рассчитываем следующий интервал (зависит от прогресса)
                float progress = elapsedTime / totalLoadTime;
                float nextInterval = GetSpawnInterval(progress);
                nextSpawnTime = elapsedTime + nextInterval;
            }

            yield return null;
        }

        // Загрузка завершена
        isLoading = false;
        Debug.Log("Загрузка завершена!");

        // Воспроизводим звук завершения
        PlaySound(completeSound);

        // Затухание и закрытие
        if (fadeOutOnComplete)
        {
            yield return StartCoroutine(FadeOutAndClose());
        }
        else
        {
            CloseWindow();
        }

        // Вызываем событие
        onLoadingComplete?.Invoke();
    }

    private float GetSpawnInterval(float progress)
    {
        if (useCurveForSpawning)
        {
            // Кривая: в начале быстро (маленький интервал), в конце медленно (большой интервал)
            float curveValue = spawnSpeedCurve.Evaluate(progress);
            // Инвертируем: чем больше прогресс, тем больше интервал
            float t = Mathf.Lerp(minSpawnInterval, maxSpawnInterval, curveValue);
            return t;
        }
        else
        {
            // Полностью случайный интервал
            return Random.Range(minSpawnInterval, maxSpawnInterval);
        }
    }

    private void SpawnRandomPrefab()
    {
        if (loadingPrefabs == null || loadingPrefabs.Length == 0)
        {
            Debug.LogWarning("Нет префабов для спавна!");
            return;
        }

        // Выбираем случайный префаб
        int randomIndex = Random.Range(0, loadingPrefabs.Length);
        GameObject prefabToSpawn = loadingPrefabs[randomIndex];

        if (prefabToSpawn == null) return;

        // Спавним
        GameObject newObject = Instantiate(prefabToSpawn, spawnParent);

        // Рандомная позиция в пределах панели
        RectTransform parentRect = spawnParent as RectTransform;
        if (parentRect != null)
        {
            RectTransform rect = newObject.GetComponent<RectTransform>();
            if (rect != null)
            {
                // Случайная позиция в пределах панели
                float randomX = Random.Range(-parentRect.rect.width / 2, parentRect.rect.width / 2);
                float randomY = Random.Range(-parentRect.rect.height / 2, parentRect.rect.height / 2);
                rect.anchoredPosition = new Vector2(randomX, randomY);

                // Случайное вращение
                rect.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));

                // Случайный масштаб
                float randomScale = Random.Range(0.8f, 1.2f);
                rect.localScale = Vector3.one * randomScale;
            }
        }

        // Добавляем анимацию появления
        StartCoroutine(AnimateSpawn(newObject));

        // Добавляем анимацию исчезновения через некоторое время
        float lifetime = Random.Range(0.5f, 1.5f);
        StartCoroutine(AnimateDespawn(newObject, lifetime));

        spawnedObjects.Add(newObject);

        // Воспроизводим звук
        PlaySound(spawnSound);
    }

    private IEnumerator AnimateSpawn(GameObject obj)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect != null)
        {
            Vector3 originalScale = rect.localScale;
            rect.localScale = Vector3.zero;

            float duration = 0.2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rect.localScale = Vector3.Lerp(Vector3.zero, originalScale, t);
                yield return null;
            }

            rect.localScale = originalScale;
        }
    }

    private IEnumerator AnimateDespawn(GameObject obj, float lifetime)
    {
        yield return new WaitForSeconds(lifetime);

        if (obj != null)
        {
            // Анимация исчезновения
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect != null)
            {
                Vector3 originalScale = rect.localScale;
                float duration = 0.15f;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    rect.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
                    yield return null;
                }
            }

            Destroy(obj);
            spawnedObjects.Remove(obj);
        }
    }

    private IEnumerator FadeOutAndClose()
    {
        if (canvasGroup == null)
        {
            CloseWindow();
            yield break;
        }

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        CloseWindow();
        canvasGroup.alpha = 1f; // Сброс для следующего раза
    }

    private void CloseWindow()
    {
        ClearAllSpawnedObjects();

        if (loadingWindow != null)
            loadingWindow.SetActive(false);
    }

    private void ClearAllSpawnedObjects()
    {
        foreach (var obj in spawnedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        spawnedObjects.Clear();
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Пропустить загрузку (для тестирования)
    /// </summary>
    public void SkipLoading()
    {
        if (isLoading)
        {
            StopAllCoroutines();
            isLoading = false;
            CloseWindow();
            onLoadingComplete?.Invoke();
        }
    }
}