using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WebGLRandomDisabler : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private GameObject[] objectsToDisable;
    [SerializeField] private float minInterval = 8f;
    [SerializeField] private float maxInterval = 20f;
    [SerializeField][Range(0f, 1f)] private float disableChance = 0.4f;

    [Header("Электрощиток")]
    [SerializeField] private ElectricalPanel3D electricalPanel;

    [Header("Звуки")]
    [SerializeField] private AudioClip disableSound;
    [SerializeField] private AudioClip powerOffSound;

    [Header("Визуальные эффекты")]
    [SerializeField] private GameObject disableEffect;
    [SerializeField] private ParticleSystem powerOffSpark;

    private AudioSource audioSource;
    private bool isDisabled = false;
    private List<GameObject> disabledObjects = new List<GameObject>();
    private List<MoveDownObject> movedObjects = new List<MoveDownObject>();

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Подписываемся на событие восстановления питания
        if (electricalPanel != null)
        {
            electricalPanel.OnAllBreakersFixed += OnPowerRestored;
        }

        StartCoroutine(RandomDisableRoutine());
    }

    private IEnumerator RandomDisableRoutine()
    {
        while (!isDisabled)
        {
            float waitTime = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(waitTime);

            float roll = Random.Range(0f, 1f);
            if (roll <= disableChance && !isDisabled)
            {
                DisableObjects();
                yield break;
            }
        }
    }

    private void DisableObjects()
    {
        isDisabled = true;

        // Звук отключения
        if (disableSound != null)
            audioSource.PlayOneShot(disableSound);

        if (powerOffSound != null)
            audioSource.PlayOneShot(powerOffSound);

        // Эффект искр
        if (powerOffSpark != null)
        {
            ParticleSystem sparks = Instantiate(powerOffSpark, transform.position, Quaternion.identity);
            Destroy(sparks.gameObject, 1f);
        }

        // Эффект на каждом объекте
        if (disableEffect != null)
        {
            foreach (var obj in objectsToDisable)
            {
                if (obj != null)
                {
                    GameObject effect = Instantiate(disableEffect, obj.transform.position, Quaternion.identity);
                    Destroy(effect, 0.5f);
                }
            }
        }

        // Отключаем объекты и опускаем их
        foreach (var obj in objectsToDisable)
        {
            if (obj != null && obj.activeSelf)
            {
                obj.SetActive(false);
                disabledObjects.Add(obj);
                Debug.Log($"Объект {obj.name} ОТКЛЮЧЁН!");

                // Опускаем объект (если есть компонент MoveDownObject)
                MoveDownObject mover = obj.GetComponent<MoveDownObject>();
                if (mover != null)
                {
                    mover.MoveDown();
                    movedObjects.Add(mover);
                }
            }
        }

        // Опускаем кубики на щитке
        if (electricalPanel != null)
        {
            electricalPanel.TriggerRandomBreakers(disabledObjects);
        }
    }

    private void OnPowerRestored()
    {
        Debug.Log("Питание восстановлено! Включаем объекты обратно...");

        // Поднимаем объекты обратно
        foreach (var mover in movedObjects)
        {
            if (mover != null)
                mover.MoveUp();
        }

        // Включаем объекты обратно
        foreach (var obj in disabledObjects)
        {
            if (obj != null)
            {
                obj.SetActive(true);
                Debug.Log($"Объект {obj.name} ВКЛЮЧЁН обратно!");
            }
        }

        disabledObjects.Clear();
        movedObjects.Clear();
        isDisabled = false;

        // Запускаем проверку заново
        StartCoroutine(RandomDisableRoutine());
    }

    private void OnDestroy()
    {
        if (electricalPanel != null)
        {
            electricalPanel.OnAllBreakersFixed -= OnPowerRestored;
        }
    }

    public bool IsDisabled() => isDisabled;
}