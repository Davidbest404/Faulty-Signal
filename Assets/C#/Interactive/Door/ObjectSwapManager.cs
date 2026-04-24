using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;

public class ObjectSwapManager : MonoBehaviour
{
    [System.Serializable]
    public class SwapPair
    {
        public GameObject objectToDeactivate;
        public GameObject objectToActivate;

        [Range(0f, 100f)]
        public float swapChance = 50f;

        [HideInInspector]
        public bool isSwapped = false;
    }

    [Header("Временные настройки")]
    [SerializeField] private float minTimeBeforeSwapWave = 10f;
    [SerializeField] private float maxTimeBeforeSwapWave = 30f;

    [Header("Задержки между заменами в волне")]
    [SerializeField] private bool useRandomDelayBetweenSwaps = true;
    [SerializeField] private float minDelayBetweenSwaps = 1f;
    [SerializeField] private float maxDelayBetweenSwaps = 5f;
    [SerializeField] private float fixedDelayBetweenSwaps = 2f;

    [Header("Настройки сброса раунда")]
    [SerializeField] private bool resetAllSwapsAfterWave = true;
    [SerializeField] private bool autoResetObjectsAfterWave = false;

    [Header("Настройки звука")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip swapSound;
    [SerializeField] private AudioClip doorSound;
    [SerializeField] private float soundVolume = 1f;

    [Header("Настройки замен")]
    [SerializeField] private List<SwapPair> swapPairs = new List<SwapPair>();

    [Header("Вызов методов")]
    [SerializeField] private UnityEvent methodsToCall;

    [Header("Отладка")]
    [SerializeField] private bool enableDebugLogs = true;

    private bool isSwapWaveActive = false;
    private int waveNumber = 0;

    void Start()
    {
        InitializeAudioSource();
        StartCoroutine(SwapWaveScheduler());
    }

    void InitializeAudioSource()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null && swapSound != null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (audioSource == null && doorSound != null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    IEnumerator SwapWaveScheduler()
    {
        while (true)
        {
            float waitTime = UnityEngine.Random.Range(minTimeBeforeSwapWave, maxTimeBeforeSwapWave);
            Log($"Следующая волна замен начнется через {waitTime:F1} секунд");
            yield return new WaitForSeconds(waitTime);

            PrepareForNewWave();
            yield return StartCoroutine(SwapWave());
        }
    }

    void PrepareForNewWave()
    {
        waveNumber++;
        Log($"=== ПОДГОТОВКА К РАУНДУ #{waveNumber} ===");

        if (resetAllSwapsAfterWave)
        {
            Log("Сброс всех замен перед новым раундом...");

            foreach (var pair in swapPairs)
            {
                if (pair.isSwapped)
                {
                    if (autoResetObjectsAfterWave)
                    {
                        if (pair.objectToActivate != null)
                            pair.objectToActivate.SetActive(false);

                        if (pair.objectToDeactivate != null)
                            pair.objectToDeactivate.SetActive(true);
                    }

                    pair.isSwapped = false;
                    Log($"Сброшен флаг замены для: {pair.objectToDeactivate.name}");
                }
            }

            LogCurrentState();
        }
    }

    IEnumerator SwapWave()
    {
        if (isSwapWaveActive)
        {
            Log("Волна замен уже активна, пропускаем");
            yield break;
        }
        PlayDoorSound();
        isSwapWaveActive = true;
        Log($"=== НАЧАЛСЯ РАУНД ЗАМЕН #{waveNumber} ===");

        List<SwapPair> availablePairs = new List<SwapPair>();
        foreach (var pair in swapPairs)
        {
            if (!pair.isSwapped)
            {
                availablePairs.Add(pair);
                Log($"Доступен для замены в этом раунде: {pair.objectToDeactivate.name}");
            }
            else
            {
                Log($"Уже заменен в этом раунде: {pair.objectToDeactivate.name}");
            }
        }

        if (availablePairs.Count == 0)
        {
            Log("НЕТ ДОСТУПНЫХ ОБЪЕКТОВ ДЛЯ ЗАМЕНЫ В ЭТОМ РАУНДЕ!");
            isSwapWaveActive = false;
            yield break;
        }

        Log($"Всего доступно объектов для замены в раунде: {availablePairs.Count}");

        ShuffleList(availablePairs);

        int swapAttempts = 0;
        int successfulSwaps = 0;

        foreach (var pair in availablePairs)
        {
            swapAttempts++;

            float randomChance = UnityEngine.Random.Range(0f, 100f);
            Log($"Проверка {pair.objectToDeactivate.name}: шанс замены {pair.swapChance}%, выпало {randomChance:F1}%");

            if (randomChance <= pair.swapChance)
            {
                PerformSwap(pair);
                successfulSwaps++;

                if (swapAttempts < availablePairs.Count)
                {
                    float delay = GetRandomDelay();
                    Log($"Следующая замена через {delay:F1} секунд");
                    yield return new WaitForSeconds(delay);
                }
            }
            else
            {
                Log($"Объект {pair.objectToDeactivate.name} НЕ заменился (не повезло со шансом)");

                if (swapAttempts < availablePairs.Count)
                {
                    float delay = GetRandomDelay();
                    yield return new WaitForSeconds(delay);
                }
            }
        }

        Log($"РАУНД #{waveNumber} ЗАВЕРШЕН. Попыток: {swapAttempts}, Успешных замен: {successfulSwaps}");
        LogCurrentState();

        // Синхронизируем состояние и проверяем, все ли объекты реально заменены
        CheckAndCallIfAllSwapped();

        isSwapWaveActive = false;
    }

    float GetRandomDelay()
    {
        if (useRandomDelayBetweenSwaps)
        {
            return UnityEngine.Random.Range(minDelayBetweenSwaps, maxDelayBetweenSwaps);
        }
        else
        {
            return fixedDelayBetweenSwaps;
        }
    }

    void PerformSwap(SwapPair pair)
    {
        if (pair.objectToDeactivate == null || pair.objectToActivate == null)
        {
            Debug.LogWarning("Один из объектов не назначен!");
            return;
        }

        if (pair.isSwapped)
        {
            Log($"Пропускаем {pair.objectToDeactivate.name} - уже заменен в этом раунде");
            return;
        }

        if (pair.objectToDeactivate != null)
            pair.objectToDeactivate.SetActive(false);

        if (pair.objectToActivate != null)
            pair.objectToActivate.SetActive(true);

        pair.isSwapped = true;

        PlaySwapSound();

        Log($"✅ ЗАМЕНА УСПЕШНА: {pair.objectToDeactivate.name} -> {pair.objectToActivate.name}");
    }

    // Метод для возврата объекта (вызывается из другого скрипта)
    public void RevertSwapByObject(GameObject targetObject)
    {
        Log($"Попытка вернуть объект: {targetObject.name}");

        for (int i = 0; i < swapPairs.Count; i++)
        {
            if (swapPairs[i].objectToActivate == targetObject && swapPairs[i].isSwapped)
            {
                Log($"Найден замененный объект: {swapPairs[i].objectToDeactivate.name}");

                if (swapPairs[i].objectToActivate != null)
                    swapPairs[i].objectToActivate.SetActive(false);

                if (swapPairs[i].objectToDeactivate != null)
                    swapPairs[i].objectToDeactivate.SetActive(true);

                swapPairs[i].isSwapped = false;

                PlaySwapSound();

                Log($"✅ ВОЗВРАТ УСПЕШЕН: {swapPairs[i].objectToDeactivate.name} восстановлен");

                // После возврата проверяем, все ли еще пары заменены (если нет - метод не вызовется)
                CheckAndCallIfAllSwapped();

                return;
            }
        }

        Log($"❌ Объект {targetObject.name} не найден среди замененных!");
    }

    public void RevertSwap(int pairIndex)
    {
        if (pairIndex < 0 || pairIndex >= swapPairs.Count)
        {
            Debug.LogWarning("Неверный индекс пары");
            return;
        }

        var pair = swapPairs[pairIndex];

        if (pair.isSwapped)
        {
            if (pair.objectToActivate != null)
                pair.objectToActivate.SetActive(false);

            if (pair.objectToDeactivate != null)
                pair.objectToDeactivate.SetActive(true);

            pair.isSwapped = false;

            PlaySwapSound();

            Log($"Возврат пары {pairIndex}: {pair.objectToDeactivate.name} восстановлен");

            // После возврата проверяем, все ли еще пары заменены
            CheckAndCallIfAllSwapped();
        }
    }

    public void RevertAllSwaps()
    {
        Log("Возврат ВСЕХ объектов в текущем раунде...");

        foreach (var pair in swapPairs)
        {
            if (pair.isSwapped)
            {
                if (pair.objectToActivate != null)
                    pair.objectToActivate.SetActive(false);

                if (pair.objectToDeactivate != null)
                    pair.objectToDeactivate.SetActive(true);

                pair.isSwapped = false;
            }
        }

        PlaySwapSound();
        Log("ВСЕ ОБЪЕКТЫ ВОЗВРАЩЕНЫ");
        LogCurrentState();

        // После возврата всех проверяем (вернет false, метод не вызовется)
        CheckAndCallIfAllSwapped();
    }

    // Принудительный сброс для следующего раунда (без возврата объектов)
    public void ForceResetForNextWave()
    {
        Log("Принудительный сброс флагов для следующего раунда");

        foreach (var pair in swapPairs)
        {
            pair.isSwapped = false;
        }

        LogCurrentState();
    }

    // Синхронизирует булевые флаги с реальным состоянием объектов
    public void SyncSwapStatesWithActualObjects()
    {
        Log("Синхронизация состояния пар с реальными объектами...");

        foreach (var pair in swapPairs)
        {
            bool shouldBeSwapped = false;

            if (pair.objectToDeactivate != null && pair.objectToActivate != null)
            {
                shouldBeSwapped = !pair.objectToDeactivate.activeSelf && pair.objectToActivate.activeSelf;
            }

            if (pair.isSwapped != shouldBeSwapped)
            {
                Log($"Синхронизация пары {pair.objectToDeactivate?.name}: было {pair.isSwapped}, стало {shouldBeSwapped}");
                pair.isSwapped = shouldBeSwapped;
            }
        }
    }

    // Публичный метод для проверки и вызова (можно вызывать из другого скрипта)
    public bool CheckAndCallIfAllSwapped()
    {
        // Сначала синхронизируем состояние
        SyncSwapStatesWithActualObjects();

        // Проверяем реальное состояние объектов
        bool allPairsAreSwapped = true;

        foreach (var pair in swapPairs)
        {
            bool isDeactivateObjectCorrect = false;
            bool isActivateObjectCorrect = false;

            if (pair.objectToDeactivate != null)
            {
                isDeactivateObjectCorrect = !pair.objectToDeactivate.activeSelf;
            }

            if (pair.objectToActivate != null)
            {
                isActivateObjectCorrect = pair.objectToActivate.activeSelf;
            }

            if (!isDeactivateObjectCorrect || !isActivateObjectCorrect)
            {
                allPairsAreSwapped = false;
                Log($"❌ Пара НЕ заменена: {GetPairName(pair)}. " +
                    $"{pair.objectToDeactivate?.name} активен: {pair.objectToDeactivate?.activeSelf}, " +
                    $"{pair.objectToActivate?.name} активен: {pair.objectToActivate?.activeSelf}");
                break;
            }
            else
            {
                Log($"✅ Пара заменена: {GetPairName(pair)}");
            }
        }

        // Вызываем методы ТОЛЬКО если ВСЕ пары реально заменены
        if (allPairsAreSwapped)
        {
            Log("🎉 ВСЕ ПАРЫ ДЕЙСТВИТЕЛЬНО ЗАМЕНЕНЫ! Вызываем методы...");
            methodsToCall?.Invoke();
        }
        else
        {
            Log($"❌ НЕ ВСЕ ПАРЫ ЗАМЕНЕНЫ! Метод НЕ вызван.");
        }

        return allPairsAreSwapped;
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = UnityEngine.Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    string GetPairName(SwapPair pair)
    {
        if (pair.objectToDeactivate == null || pair.objectToActivate == null)
            return "Unknown";
        return $"{pair.objectToDeactivate.name} -> {pair.objectToActivate.name}";
    }

    void PlaySwapSound()
    {
        if (audioSource != null && swapSound != null)
        {
            audioSource.PlayOneShot(swapSound, soundVolume);
        }
    }

    void PlayDoorSound()
    {
        if (audioSource != null && doorSound != null)
        {
            audioSource.PlayOneShot(doorSound, soundVolume);
        }
    }

    void LogCurrentState()
    {
        if (!enableDebugLogs) return;

        Debug.Log($"=== СОСТОЯНИЕ ПОСЛЕ РАУНДА #{waveNumber} ===");
        for (int i = 0; i < swapPairs.Count; i++)
        {
            var pair = swapPairs[i];
            string status = pair.isSwapped ? "ЗАМЕНЕН" : "ОРИГИНАЛ";
            Debug.Log($"Пара {i}: {pair.objectToDeactivate.name} -> {status}");
        }
        Debug.Log("=====================================");
    }

    void Log(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[ObjectSwapManager] {message}");
        }
    }

    // Геттеры
    public bool IsSwapWaveActive() { return isSwapWaveActive; }
    public int GetCurrentWaveNumber() { return waveNumber; }

    public int GetSwappedCount()
    {
        int count = 0;
        foreach (var pair in swapPairs)
            if (pair.isSwapped) count++;
        return count;
    }

    public int GetRemainingCount()
    {
        int count = 0;
        foreach (var pair in swapPairs)
            if (!pair.isSwapped) count++;
        return count;
    }

    public List<SwapPair> GetSwapPairs() { return swapPairs; }
}