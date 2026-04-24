using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeSceneOnClick : MonoBehaviour
{
    [Header("Настройки сцены")]
    [SerializeField] private string sceneName = "SceneName";  // Имя сцены для загрузки
    [SerializeField] private int sceneIndex = 1;              // Индекс сцены (альтернатива)
    [SerializeField] private bool loadByIndex = false;        // Загружать по индексу или имени

    [Header("Настройки клика")]
    [SerializeField] private bool requireRaycast = true;      // Нужен ли луч (клик по объекту)
    [SerializeField] private KeyCode alternativeKey = KeyCode.None; // Альтернативная клавиша

    [Header("Визуальный эффект")]
    [SerializeField] private Color hoverColor = Color.yellow;  // Цвет при наведении
    [SerializeField] private AudioClip clickSound;             // Звук при клике
    [SerializeField] private AudioSource audioSource;

    private Renderer objectRenderer;
    private Color originalColor;
    private Material originalMaterial;
    private bool isHovering = false;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        objectRenderer = GetComponent<Renderer>();

        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
            originalColor = originalMaterial.color;
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // Проверка клика мышью
        if (Input.GetMouseButtonDown(0))
        {
            if (requireRaycast)
            {
                // Клик по конкретному объекту
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider.gameObject == gameObject)
                    {
                        ChangeScene();
                    }
                }
            }
            else
            {
                // Клик в любом месте экрана
                ChangeScene();
            }
        }

        // Проверка альтернативной клавиши
        if (alternativeKey != KeyCode.None && Input.GetKeyDown(alternativeKey))
        {
            ChangeScene();
        }

        // Эффект наведения
        if (requireRaycast)
        {
            HandleMouseHover();
        }
    }

    void HandleMouseHover()
    {
        if (objectRenderer == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == gameObject)
            {
                if (!isHovering)
                {
                    isHovering = true;
                    if (objectRenderer != null)
                        objectRenderer.material.color = hoverColor;
                }
            }
            else
            {
                if (isHovering)
                {
                    isHovering = false;
                    if (objectRenderer != null)
                        objectRenderer.material.color = originalColor;
                }
            }
        }
        else
        {
            if (isHovering)
            {
                isHovering = false;
                if (objectRenderer != null)
                    objectRenderer.material.color = originalColor;
            }
        }
    }

    void ChangeScene()
    {
        // Воспроизводим звук
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        Debug.Log($"Загрузка сцены: {(loadByIndex ? sceneIndex.ToString() : sceneName)}");

        // Загружаем сцену
        if (loadByIndex)
        {
            SceneManager.LoadScene(sceneIndex);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    // Метод для загрузки сцены по имени из другого скрипта
    public void LoadSceneByName(string name)
    {
        SceneManager.LoadScene(name);
    }

    // Метод для загрузки сцены по индексу из другого скрипта
    public void LoadSceneByIndex(int index)
    {
        SceneManager.LoadScene(index);
    }
}