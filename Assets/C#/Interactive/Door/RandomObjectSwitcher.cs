using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class RandomObjectSwitcher : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private List<ObjectSwitchGroup> switchGroups = new List<ObjectSwitchGroup>();
    [SerializeField] private float minDelayBetweenSwitches = 2f;
    [SerializeField] private float maxDelayBetweenSwitches = 8f;
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool randomOrder = true;

    [Header("Глобальные настройки")]
    [SerializeField] private bool playSoundOnSwitch = true;
    [SerializeField] private AudioClip defaultSwitchSound;
    [SerializeField] private float soundVolume = 1f;

    [Header("Визуальные эффекты")]
    [SerializeField] private ParticleSystem switchEffect;
    [SerializeField] private GameObject flashEffect;

    [Header("Событие при завершении")]
    [SerializeField] private UnityEngine.Events.UnityEvent onAllUnocked;
    [SerializeField] private MonoBehaviour targetComponent;
    [SerializeField] private string onAllLockedMethodName = "";

    private AudioSource audioSource;
    private bool isRunning = false;
    private Coroutine switchCoroutine;
    private int totalLockedCount = 0;
    private int totalObjectsCount = 0;

    [System.Serializable]
    public class ObjectSwitchGroup
    {
        [Header("Информация о группе")]
        public string groupName = "Группа";
        public bool isEnabled = true;

        [Header("Объекты в группе")]
        public List<SwitchableObject> objects = new List<SwitchableObject>();

        [Header("Настройки группы")]
        public bool useCustomDelay = false;
        public float customMinDelay = 2f;
        public float customMaxDelay = 5f;
        public bool requireAllLockedToComplete = true;

        [Header("Звуки группы")]
        public AudioClip groupSwitchSound;

        [HideInInspector]
        public int lockedCount = 0;
        [HideInInspector]
        public bool isCompleted = false;
    }

    [System.Serializable]
    public class SwitchableObject
    {
        [Header("Целевой объект")]
        public string objectName;
        public GameObject targetObject;

        [Header("Действия при активации")]
        public GameObject[] objectsToActivate;
        public GameObject[] objectsToDeactivate;

        [Header("Настройки")]
        public bool isLocked = false;           // Заблокирован ли уже
        public bool canBeLocked = true;          // Может ли быть заблокирован
        public float lockChance = 1f;            // Шанс блокировки (0-1)

        [Header("Визуальные эффекты")]
        public AudioClip switchSound;
        public ParticleSystem customEffect;

        [Header("Задержка перед действием")]
        public float delayBeforeSwitch = 0f;

        [Header("Событие при блокировке")]
        public UnityEngine.Events.UnityEvent onObjectLocked;

        [HideInInspector]
        public bool isActive = true;
    }

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;

        // Подсчитываем общее количество объектов
        foreach (var group in switchGroups)
        {
            if (group.isEnabled)
            {
                totalObjectsCount += group.objects.Count(o => o.canBeLocked);
            }
        }

        if (autoStart)
        {
            StartSwitching();
        }
    }

    public void StartSwitching()
    {
        if (isRunning) return;

        isRunning = true;
        totalLockedCount = 0;

        // Сбрасываем состояние групп
        foreach (var group in switchGroups)
        {
            group.lockedCount = 0;
            group.isCompleted = false;

            foreach (var obj in group.objects)
            {
                obj.isLocked = false;
                obj.isActive = true;

                // Активируем начальные объекты
                if (obj.targetObject != null)
                    obj.targetObject.SetActive(true);
            }
        }

        if (switchCoroutine != null)
            StopCoroutine(switchCoroutine);
        switchCoroutine = StartCoroutine(SwitchRoutine());
    }

    public void StopSwitching()
    {
        isRunning = false;
        if (switchCoroutine != null)
            StopCoroutine(switchCoroutine);
    }

    private IEnumerator SwitchRoutine()
    {
        while (isRunning && totalLockedCount < totalObjectsCount)
        {
            // Выбираем группу
            List<ObjectSwitchGroup> availableGroups = switchGroups
                .Where(g => g.isEnabled && !g.isCompleted)
                .ToList();

            if (availableGroups.Count == 0)
            {
                Debug.Log("Нет доступных групп!");
                break;
            }

            ObjectSwitchGroup selectedGroup = randomOrder ?
                availableGroups[Random.Range(0, availableGroups.Count)] :
                availableGroups[0];

            // Выбираем объект в группе
            List<SwitchableObject> availableObjects = selectedGroup.objects
                .Where(o => !o.isLocked && o.canBeLocked)
                .ToList();

            if (availableObjects.Count == 0)
            {
                // Все объекты в группе заблокированы
                selectedGroup.isCompleted = true;
                continue;
            }

            SwitchableObject selectedObject = randomOrder ?
                availableObjects[Random.Range(0, availableObjects.Count)] :
                availableObjects[0];

            // Проверка шанса
            float roll = Random.Range(0f, 1f);
            if (roll > selectedObject.lockChance)
            {
                Debug.Log($"Объект {selectedObject.objectName} не был заблокирован (шанс {selectedObject.lockChance}, выпало {roll})");

                // Ждём следующий цикл
                float delay = GetDelayForGroup(selectedGroup);
                yield return new WaitForSeconds(delay);
                continue;
            }

            // Выполняем блокировку объекта
            yield return StartCoroutine(LockObject(selectedObject, selectedGroup));

            // Обновляем счётчики
            selectedGroup.lockedCount++;
            totalLockedCount++;

            // Проверяем завершение группы
            if (selectedGroup.requireAllLockedToComplete &&
                selectedGroup.lockedCount >= selectedGroup.objects.Count(o => o.canBeLocked))
            {
                selectedGroup.isCompleted = true;
                Debug.Log($"Группа {selectedGroup.groupName} полностью заблокирована!");
            }

            // Ждём следующий цикл
            float nextDelay = GetDelayForGroup(selectedGroup);
            yield return new WaitForSeconds(nextDelay);
        }

        // Все объекты заблокированы!
        Debug.Log("ВСЕ ОБЪЕКТЫ ЗАБЛОКИРОВАНЫ!");

        // Вызываем событие
        onAllUnocked?.Invoke();

        if (targetComponent != null && !string.IsNullOrEmpty(onAllLockedMethodName))
        {
            targetComponent.Invoke(onAllLockedMethodName, 0f);
        }

        isRunning = false;
    }

    private IEnumerator LockObject(SwitchableObject obj, ObjectSwitchGroup group)
    {
        // Задержка перед блокировкой
        if (obj.delayBeforeSwitch > 0)
        {
            yield return new WaitForSeconds(obj.delayBeforeSwitch);
        }

        // Визуальный эффект
        if (flashEffect != null)
        {
            GameObject flash = Instantiate(flashEffect, obj.targetObject.transform.position, Quaternion.identity);
            Destroy(flash, 0.3f);
        }

        if (switchEffect != null)
        {
            ParticleSystem effect = Instantiate(switchEffect, obj.targetObject.transform.position, Quaternion.identity);
            Destroy(effect.gameObject, 0.5f);
        }

        if (obj.customEffect != null)
        {
            ParticleSystem customEffect = Instantiate(obj.customEffect, obj.targetObject.transform.position, Quaternion.identity);
            Destroy(customEffect.gameObject, 0.5f);
        }

        // Звук
        PlaySound(obj.switchSound != null ? obj.switchSound :
                  (group.groupSwitchSound != null ? group.groupSwitchSound : defaultSwitchSound));

        // Деактивируем целевой объект
        if (obj.targetObject != null)
        {
            obj.targetObject.SetActive(false);
            obj.isActive = false;
            Debug.Log($"Объект {obj.objectName} ЗАБЛОКИРОВАН!");
        }

        // Активируем указанные объекты
        foreach (var activateObj in obj.objectsToActivate)
        {
            if (activateObj != null)
            {
                activateObj.SetActive(true);
                Debug.Log($"Активирован объект: {activateObj.name}");
            }
        }

        // Деактивируем указанные объекты
        foreach (var deactivateObj in obj.objectsToDeactivate)
        {
            if (deactivateObj != null)
            {
                deactivateObj.SetActive(false);
                Debug.Log($"Деактивирован объект: {deactivateObj.name}");
            }
        }

        // Отмечаем как заблокированный
        obj.isLocked = true;

        // Вызываем событие
        obj.onObjectLocked?.Invoke();
    }

    private float GetDelayForGroup(ObjectSwitchGroup group)
    {
        if (group.useCustomDelay)
        {
            return Random.Range(group.customMinDelay, group.customMaxDelay);
        }
        return Random.Range(minDelayBetweenSwitches, maxDelayBetweenSwitches);
    }

    private void PlaySound(AudioClip clip)
    {
        if (playSoundOnSwitch && clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }

    // Публичные методы
    public void ResetAllObjects()
    {
        StopSwitching();

        foreach (var group in switchGroups)
        {
            group.lockedCount = 0;
            group.isCompleted = false;

            foreach (var obj in group.objects)
            {
                obj.isLocked = false;
                obj.isActive = true;

                if (obj.targetObject != null)
                    obj.targetObject.SetActive(true);
            }
        }

        totalLockedCount = 0;

        if (autoStart)
            StartSwitching();
    }

    public int GetLockedCount()
    {
        return totalLockedCount;
    }

    public int GetTotalCount()
    {
        return totalObjectsCount;
    }

    public bool IsAllLocked()
    {
        return totalLockedCount >= totalObjectsCount;
    }
}