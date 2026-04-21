using UnityEngine;
using UnityEngine.Events;

public class ClickableItem : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private string itemName = "Предмет";
    [SerializeField] private KeyCode interactionKey = KeyCode.Mouse0;
    [SerializeField] private bool canClickOnlyOnce = true;

    [Header("Действия")]
    [SerializeField] private GameObject[] objectsToActivate;
    [SerializeField] private GameObject[] objectsToDeactivate;

    [Header("Визуальные эффекты")]
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private ParticleSystem clickEffect;
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Color highlightColor = Color.yellow;

    [Header("Ссылка на группу")]
    [SerializeField] private ClickableGroup parentGroup;

    [Header("События")]
    [SerializeField] private UnityEvent OnClickEvent;

    private bool hasBeenClicked = false;
    private Renderer objectRenderer;
    private AudioSource audioSource;
    private Color originalColor;
    private Material originalMaterial;

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalColor = objectRenderer.material.color;
            originalMaterial = objectRenderer.material;
        }
    }

    private void OnMouseDown()
    {
        if (canClickOnlyOnce && hasBeenClicked) return;
        if (!Input.GetKeyDown(interactionKey)) return;

        hasBeenClicked = true;

        // Звук
        if (clickSound != null)
            audioSource.PlayOneShot(clickSound);

        // Эффект
        if (clickEffect != null)
        {
            ParticleSystem effect = Instantiate(clickEffect, transform.position, Quaternion.identity);
            Destroy(effect.gameObject, 1f);
        }

        // Активация объектов
        foreach (var obj in objectsToActivate)
        {
            if (obj != null) obj.SetActive(true);
        }

        // Деактивация объектов
        foreach (var obj in objectsToDeactivate)
        {
            if (obj != null) obj.SetActive(false);
        }

        // Уведомляем группу
        if (parentGroup != null)
        {
            var itemData = new ClickableGroup.ClickableItem
            {
                itemName = itemName,
                targetObject = gameObject,
                isClicked = true
            };
            parentGroup.OnItemClicked(itemData);
        }

        OnClickEvent?.Invoke();

        // Меняем цвет (опционально)
        if (objectRenderer != null)
        {
            objectRenderer.material.color = Color.gray;
        }

        Debug.Log($"Клик по предмету: {itemName}");
    }

    private void OnMouseEnter()
    {
        if (hasBeenClicked) return;

        if (objectRenderer != null)
        {
            if (highlightMaterial != null)
                objectRenderer.material = highlightMaterial;
            else
                objectRenderer.material.color = highlightColor;
        }
    }

    private void OnMouseExit()
    {
        if (hasBeenClicked) return;

        if (objectRenderer != null)
        {
            if (originalMaterial != null)
                objectRenderer.material = originalMaterial;
            else
                objectRenderer.material.color = originalColor;
        }
    }
}