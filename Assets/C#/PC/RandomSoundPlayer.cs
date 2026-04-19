using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class RandomSoundPlayer : MonoBehaviour
{
    [Header("Настройки звуков")]
    [SerializeField] private List<AudioClip> soundClips = new List<AudioClip>(); // Список звуков
    [SerializeField] private bool randomOrder = true;        // Случайный порядок
    [SerializeField] private bool allowDuplicates = false;   // Разрешить повтор подряд

    [Header("Настройки интервала")]
    [SerializeField] private float minInterval = 5f;         // Минимальный интервал
    [SerializeField] private float maxInterval = 15f;        // Максимальный интервал
    [SerializeField] private bool randomInterval = true;     // Случайный интервал
    [SerializeField] private float fixedInterval = 10f;      // Фиксированный интервал (если randomInterval = false)

    [Header("Настройки громкости")]
    [Range(0f, 1f)]
    [SerializeField] private float volumeMin = 0.7f;         // Минимальная громкость
    [Range(0f, 1f)]
    [SerializeField] private float volumeMax = 1f;           // Максимальная громкость
    [SerializeField] private bool randomVolume = true;       // Случайная громкость

    [Header("Настройки панорамы (3D звук)")]
    [Range(-1f, 1f)]
    [SerializeField] private float panMin = -1f;             // Минимальная панорама (лево)
    [Range(-1f, 1f)]
    [SerializeField] private float panMax = 1f;              // Максимальная панорама (право)
    [SerializeField] private bool randomPan = false;         // Случайная панорама

    [Header("AudioMixer")]
    [SerializeField] private AudioMixerGroup audioMixerGroup; // Группа в AudioMixer
    [SerializeField] private string mixerVolumeParameter = "Volume"; // Параметр громкости в микшере

    [Header("Настройки воспроизведения")]
    [SerializeField] private bool playOnStart = true;         // Начать при старте
    [SerializeField] private bool loopForever = true;         // Бесконечный цикл
    [SerializeField] private int maxPlayCount = 10;           // Макс. количество воспроизведений (если не loopForever)
    [SerializeField] private bool stopOnDisable = true;       // Остановить при отключении объекта

    [Header("Отладка")]
    [SerializeField] private bool debugMode = true;           // Вывод в консоль

    private AudioSource audioSource;
    private List<AudioClip> remainingClips;                   // Оставшиеся звуки для воспроизведения
    private bool isPlaying = false;
    private int playCount = 0;
    private Coroutine soundCoroutine;

    // События
    public System.Action<AudioClip> OnSoundPlayed;
    public System.Action OnAllSoundsPlayed;
    public System.Action OnPlaybackStarted;
    public System.Action OnPlaybackStopped;

    private void Awake()
    {
        SetupAudioSource();
    }

    private void SetupAudioSource()
    {
        // Получаем или создаём AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Настройка AudioSource
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        // Применяем AudioMixerGroup
        if (audioMixerGroup != null)
        {
            audioSource.outputAudioMixerGroup = audioMixerGroup;
        }

        // Дополнительные настройки для 3D звука
        audioSource.spatialBlend = 0f; // 0 = 2D, 1 = 3D (настраивайте по необходимости)
    }

    private void Start()
    {
        if (playOnStart)
        {
            StartPlayback();
        }
    }

    private void OnDisable()
    {
        if (stopOnDisable && isPlaying)
        {
            StopPlayback();
        }
    }

    public void StartPlayback()
    {
        if (soundClips == null || soundClips.Count == 0)
        {
            Debug.LogWarning("Нет звуков для воспроизведения!");
            return;
        }

        if (isPlaying) return;

        isPlaying = true;
        playCount = 0;
        InitializeRemainingClips();

        if (soundCoroutine != null)
            StopCoroutine(soundCoroutine);

        soundCoroutine = StartCoroutine(PlayRandomSounds());

        OnPlaybackStarted?.Invoke();
        if (debugMode) Debug.Log("🎵 Воспроизведение случайных звуков начато");
    }

    public void StopPlayback()
    {
        if (!isPlaying) return;

        isPlaying = false;

        if (soundCoroutine != null)
        {
            StopCoroutine(soundCoroutine);
            soundCoroutine = null;
        }

        if (audioSource.isPlaying)
            audioSource.Stop();

        OnPlaybackStopped?.Invoke();
        if (debugMode) Debug.Log("🔇 Воспроизведение случайных звуков остановлено");
    }

    private void InitializeRemainingClips()
    {
        remainingClips = new List<AudioClip>(soundClips);

        if (randomOrder)
        {
            ShuffleList(remainingClips);
        }
    }

    private System.Collections.IEnumerator PlayRandomSounds()
    {
        while (isPlaying)
        {
            // Проверка на максимальное количество воспроизведений
            if (!loopForever && playCount >= maxPlayCount)
            {
                if (debugMode) Debug.Log($"🏁 Достигнуто максимальное количество воспроизведений ({maxPlayCount})");
                StopPlayback();
                OnAllSoundsPlayed?.Invoke();
                yield break;
            }

            // Получаем следующий звук
            AudioClip clipToPlay = GetNextClip();

            if (clipToPlay == null)
            {
                Debug.LogWarning("Нет доступных звуков для воспроизведения!");
                yield break;
            }

            // Настройка громкости
            float volume = randomVolume ? Random.Range(volumeMin, volumeMax) : volumeMin;

            // Настройка панорамы
            float pan = randomPan ? Random.Range(panMin, panMax) : 0f;

            // Воспроизводим звук
            PlaySound(clipToPlay, volume, pan);

            // Ждём окончания звука + интервал
            float waitTime = clipToPlay.length;

            // Добавляем интервал после звука
            if (randomInterval)
                waitTime += Random.Range(minInterval, maxInterval);
            else
                waitTime += fixedInterval;

            yield return new WaitForSeconds(waitTime);
        }
    }

    private AudioClip GetNextClip()
    {
        if (remainingClips.Count == 0)
        {
            if (loopForever || allowDuplicates)
            {
                // Перезаполняем список
                InitializeRemainingClips();
                if (randomOrder)
                    ShuffleList(remainingClips);
            }
            else
            {
                return null;
            }
        }

        AudioClip nextClip = remainingClips[0];
        remainingClips.RemoveAt(0);
        return nextClip;
    }

    private void PlaySound(AudioClip clip, float volume, float pan)
    {
        if (clip == null) return;

        // Настройка AudioSource
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.panStereo = pan;

        // Воспроизводим
        audioSource.Play();
        playCount++;

        // Вызываем событие
        OnSoundPlayed?.Invoke(clip);

        if (debugMode)
        {
            Debug.Log($"🔊 Воспроизведён звук: {clip.name} (Громкость: {volume:F2}, Панорама: {pan:F2}) [{playCount}]");
        }
    }

    // Воспроизвести один случайный звук (вне очереди)
    public void PlayRandomSoundOnce()
    {
        if (soundClips == null || soundClips.Count == 0) return;

        AudioClip randomClip = soundClips[Random.Range(0, soundClips.Count)];
        float volume = randomVolume ? Random.Range(volumeMin, volumeMax) : volumeMin;
        float pan = randomPan ? Random.Range(panMin, panMax) : 0f;

        PlaySound(randomClip, volume, pan);
    }

    // Воспроизвести конкретный звук
    public void PlaySpecificSound(int index)
    {
        if (index < 0 || index >= soundClips.Count) return;

        AudioClip clip = soundClips[index];
        float volume = randomVolume ? Random.Range(volumeMin, volumeMax) : volumeMin;
        float pan = randomPan ? Random.Range(panMin, panMax) : 0f;

        PlaySound(clip, volume, pan);
    }

    // Перемешивание списка (алгоритм Фишера-Йетса)
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    // Публичные методы для управления
    public void SetVolumeRange(float min, float max)
    {
        volumeMin = Mathf.Clamp01(min);
        volumeMax = Mathf.Clamp01(max);
    }

    public void SetIntervalRange(float min, float max)
    {
        minInterval = min;
        maxInterval = max;
    }

    public void AddSound(AudioClip newClip)
    {
        if (newClip != null && !soundClips.Contains(newClip))
        {
            soundClips.Add(newClip);
            if (debugMode) Debug.Log($"➕ Добавлен звук: {newClip.name}");
        }
    }

    public void RemoveSound(AudioClip clip)
    {
        soundClips.Remove(clip);
    }

    public void ClearSounds()
    {
        soundClips.Clear();
    }

    public bool IsPlaying => isPlaying;
    public int GetRemainingSoundsCount => remainingClips?.Count ?? 0;
    public int GetTotalPlayCount => playCount;
}