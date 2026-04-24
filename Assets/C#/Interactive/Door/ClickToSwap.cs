using UnityEngine;
using System.Collections;

public class ClickToSwap : MonoBehaviour
{
    [Header("Настройки замены")]
    [SerializeField] private GameObject targetObject;     // Объект, который появится при клике
    [SerializeField] private bool destroyOnSwap = false;    // Уничтожить текущий объект при замене
    [SerializeField] private bool onlyOnce = true;         // Можно заменить только один раз
    [SerializeField] private bool canRevert = false;       // Можно ли вернуть обратно

    [Header("Управление")]
    [SerializeField] private bool useLeftClickForSwap = true;    // Левая кнопка для замены
    [SerializeField] private bool useRightClickForRevert = true; // Правая кнопка для возврата

    [Header("Звук")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip swapSound;
    [SerializeField] private AudioClip revertSound;
    [SerializeField] private float soundVolume = 1f;

    [Header("Визуальный эффект")]
    [SerializeField] private Color hoverColor = Color.yellow;   // Цвет при наведении

    private GameObject originalObject;
    private GameObject currentObject;
    private bool hasBeenSwapped = false;
    private Camera mainCamera;
    private Material originalMaterial;
    private Color originalColor;
    private bool isHovering = false;
    private Renderer objectRenderer;

    // Сохраняем исходные данные для повторной инициализации
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private Transform originalParent;

    void OnEnable()
    {
        // КАЖДЫЙ РАЗ ПРИ АКТИВАЦИИ ОБЪЕКТА - ПЕРЕИНИЦИАЛИЗИРУЕМСЯ
        Initialize();
    }

    void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        mainCamera = Camera.main;
        currentObject = gameObject;
        originalObject = gameObject;
        objectRenderer = currentObject.GetComponent<Renderer>();

        // Сохраняем исходные трансформации
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalScale = transform.localScale;
        originalParent = transform.parent;

        // Сбрасываем флаг замены (если not onlyOnce)
        if (!onlyOnce && hasBeenSwapped)
        {
            hasBeenSwapped = false;
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        Log($"ClickToSwap инициализирован на {gameObject.name}, hasBeenSwapped = {hasBeenSwapped}");
    }

    void Update()
    {
        // Проверяем, можем ли мы выполнять замену
        bool canSwap = !hasBeenSwapped || !onlyOnce;
        bool canDoRevert = canRevert && hasBeenSwapped;

        if (canSwap || canDoRevert)
        {
            HandleMouseClick();
        }

        HandleMouseHover();
    }

    void HandleMouseClick()
    {
        // Левая кнопка для замены
        if (useLeftClickForSwap && Input.GetMouseButtonDown(0))
        {
            CheckAndSwap();
        }

        // Правая кнопка для возврата
        if (canRevert && useRightClickForRevert && Input.GetMouseButtonDown(1))
        {
            CheckAndRevert();
        }
    }

    void CheckAndSwap()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == currentObject && (!hasBeenSwapped || !onlyOnce))
            {
                PerformSwap();
            }
        }
    }

    void CheckAndRevert()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == currentObject && hasBeenSwapped && canRevert)
            {
                PerformRevert();
            }
        }
    }

    void HandleMouseHover()
    {
        if (objectRenderer == null)
        {
            objectRenderer = currentObject.GetComponent<Renderer>();
            if (objectRenderer == null) return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == currentObject)
            {
                if (!isHovering)
                {
                    isHovering = true;
                    HighlightObject(true);
                }
            }
            else
            {
                if (isHovering)
                {
                    isHovering = false;
                    HighlightObject(false);
                }
            }
        }
        else
        {
            if (isHovering)
            {
                isHovering = false;
                HighlightObject(false);
            }
        }
    }

    void PerformSwap()
    {
        if (targetObject == null)
        {
            Debug.LogError("Target object not assigned!");
            return;
        }

        Log($"Выполняется замена на {gameObject.name}");

        // Визуальный эффект перед заменой
        StartCoroutine(FlashObject());

        // Сохраняем позицию, поворот и родителя текущего объекта
        Vector3 position = currentObject.transform.position;
        Quaternion rotation = currentObject.transform.rotation;
        Vector3 scale = currentObject.transform.localScale;
        Transform parent = currentObject.transform.parent;

        // Активируем объект
        targetObject.SetActive(true);

        // Копируем компонент ClickToSwap на новый объект если его там нет
        ClickToSwap targetSwap = targetObject.GetComponent<ClickToSwap>();
        if (targetSwap == null)
        {
            targetSwap = targetObject.AddComponent<ClickToSwap>();
            CopySettingsTo(targetSwap);
        }
        targetSwap.Initialize();

        // Уничтожаем или деактивируем текущий объект
        if (destroyOnSwap)
        {
            Destroy(currentObject);
        }
        else
        {
            currentObject.SetActive(false);
        }

        currentObject = targetObject;
        objectRenderer = currentObject.GetComponent<Renderer>();
        hasBeenSwapped = true;

        // Звук замены
        PlaySound(swapSound);

        Debug.Log($"Объект заменен: {gameObject.name} -> {targetObject.name}");
    }

    void PerformRevert()
    {
        Log($"Выполняется возврат на {gameObject.name}");

        // Сохраняем позицию текущего объекта
        Vector3 position = currentObject.transform.position;
        Quaternion rotation = currentObject.transform.rotation;
        Vector3 scale = currentObject.transform.localScale;
        Transform parent = currentObject.transform.parent;

        GameObject revertedObject;

        if (destroyOnSwap)
        {
            // Создаем новый объект из префаба или копию
            revertedObject = Instantiate(originalObject, position, rotation);
            revertedObject.transform.localScale = scale;
            revertedObject.transform.parent = parent;

            // Копируем компонент ClickToSwap
            ClickToSwap newSwap = revertedObject.GetComponent<ClickToSwap>();
            if (newSwap == null)
                newSwap = revertedObject.AddComponent<ClickToSwap>();
            CopySettingsTo(newSwap);
            newSwap.Initialize();

            // Удаляем текущий объект
            if (currentObject != null)
                Destroy(currentObject);
        }
        else
        {
            revertedObject = originalObject;
            originalObject.transform.position = position;
            originalObject.transform.rotation = rotation;
            originalObject.transform.localScale = scale;
            originalObject.transform.parent = parent;
            originalObject.SetActive(true);

            // Обновляем ClickToSwap на возвращаемом объекте
            ClickToSwap revertSwap = originalObject.GetComponent<ClickToSwap>();
            if (revertSwap != null)
            {
                revertSwap.hasBeenSwapped = false;
                revertSwap.currentObject = originalObject;
                revertSwap.Initialize();
            }

            if (currentObject != null && currentObject != originalObject)
                Destroy(currentObject);
        }

        currentObject = revertedObject;
        objectRenderer = currentObject.GetComponent<Renderer>();
        hasBeenSwapped = false;

        // Звук возврата
        PlaySound(revertSound);

        Debug.Log($"Объект возвращен: {currentObject.name}");
    }

    void CopySettingsTo(ClickToSwap target)
    {
        target.targetObject = this.targetObject;
        target.destroyOnSwap = this.destroyOnSwap;
        target.onlyOnce = this.onlyOnce;
        target.canRevert = this.canRevert;
        target.useLeftClickForSwap = this.useLeftClickForSwap;
        target.useRightClickForRevert = this.useRightClickForRevert;
        target.swapSound = this.swapSound;
        target.revertSound = this.revertSound;
        target.soundVolume = this.soundVolume;
        target.hoverColor = this.hoverColor;
    }

    void HighlightObject(bool highlight)
    {
        if (objectRenderer != null)
        {
            if (highlight)
            {
                originalMaterial = objectRenderer.material;
                originalColor = objectRenderer.material.color;
                objectRenderer.material.color = hoverColor;
            }
            else
            {
                if (objectRenderer.material != null)
                    objectRenderer.material.color = originalColor;
            }
        }
    }

    IEnumerator FlashObject()
    {
        if (objectRenderer != null)
        {
            Color original = objectRenderer.material.color;
            objectRenderer.material.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            objectRenderer.material.color = original;
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }

    void Log(string message)
    {
        // Можно включить отладку при необходимости
        // Debug.Log($"[ClickToSwap] {message}");
    }

    // Публичный метод для принудительного сброса (если нужно)
    public void ResetSwapState()
    {
        hasBeenSwapped = false;
        currentObject = gameObject;
        Initialize();
        Log($"Состояние сброшено для {gameObject.name}");
    }
}