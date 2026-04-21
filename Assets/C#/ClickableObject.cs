using UnityEngine;
using UnityEngine.Events;

public class ClickableObject : MonoBehaviour
{
    [Header("Настройки клика")]
    [SerializeField] private string objectName = "Объект";
    [SerializeField] private KeyCode interactionKey = KeyCode.Mouse0; // ЛКМ по умолчанию
    [SerializeField] private bool requireKeyPress = true; // Требовать нажатие клавиши
    [SerializeField] private bool canClickOnlyOnce = false; // Можно нажать только один раз
    [SerializeField] private float clickCooldown = 0.5f; // Защита от спама

    [Header("Объекты для активации")]
    [SerializeField] private GameObject[] objectsToActivate;

    [Header("Объекты для деактивации")]
    [SerializeField] private GameObject[] objectsToDeactivate;

    [Header("Визуальные эффекты")]
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private ParticleSystem clickEffect;
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material normalMaterial;

    [Header("События")]
    [SerializeField] private UnityEvent OnClickEvent;

    [Header("Задержка")]
    [SerializeField] private float delayBeforeAction = 0f;

    private bool hasBeenClicked = false;
    private float lastClickTime = 0f;
    private Renderer objectRenderer;
    private AudioSource audioSource;
    private Material originalMaterial;

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null && normalMaterial == null)
        {
            originalMaterial = objectRenderer.material;
        }
        else if (normalMaterial != null)
        {
            originalMaterial = normalMaterial;
        }
    }

    private void OnMouseDown()
    {
        if (requireKeyPress && !Input.GetKeyDown(interactionKey)) return;
        if (canClickOnlyOnce && hasBeenClicked) return;
        if (Time.time - lastClickTime < clickCooldown) return;

        lastClickTime = Time.time;
        hasBeenClicked = true;

        if (delayBeforeAction > 0)
        {
            Invoke(nameof(ExecuteAction), delayBeforeAction);
        }
        else
        {
            ExecuteAction();
        }
    }

    private void OnMouseEnter()
    {
        if (objectRenderer != null && highlightMaterial != null)
        {
            objectRenderer.material = highlightMaterial;
        }
    }

    private void OnMouseExit()
    {
        if (objectRenderer != null && originalMaterial != null)
        {
            objectRenderer.material = originalMaterial;
        }
    }

    private void ExecuteAction()
    {
        // Звук
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        // Эффект
        if (clickEffect != null)
        {
            ParticleSystem effect = Instantiate(clickEffect, transform.position, Quaternion.identity);
            Destroy(effect.gameObject, 1f);
        }

        // Активируем объекты
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
            {
                obj.SetActive(true);
                Debug.Log($"Активирован: {obj.name}");
            }
        }

        // Деактивируем объекты
        foreach (GameObject obj in objectsToDeactivate)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                Debug.Log($"Деактивирован: {obj.name}");
            }
        }

        // Событие
        OnClickEvent?.Invoke();

        Debug.Log($"Клик по объекту: {objectName}");
    }

    public void ResetClick()
    {
        hasBeenClicked = false;
    }
}