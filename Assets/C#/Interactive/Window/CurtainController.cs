using UnityEngine;

/// <summary>
/// Управляет жалюзь через блендшейп "Key 1".
/// 0 = закрыто (защита от врага), 100 (Unity) = открыто.
/// Нажми F для переключения.
/// </summary>
public class CurtainController : MonoBehaviour
{
    [Header("Блендшейп шторы")]
    [Tooltip("SkinnedMeshRenderer шторы (Cube.001 внутри жалюзь).")]
    [SerializeField] private SkinnedMeshRenderer curtainMesh;

    [Tooltip("Индекс блендшейпа (0 = Key 1 по умолчанию).")]
    [SerializeField] private int blendShapeIndex = 0;

    [Header("Настройки анимации")]
    [Tooltip("Скорость закрытия/открытия (единицы в секунду, max 100).")]
    [SerializeField] private float animationSpeed = 80f;

    [Header("Управление")]
    [Tooltip("Кнопка для переключения жалюзь.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F;

    // --- Состояние ---
    private float currentValue = 100f; // 100 = открыто
    private float targetValue = 100f;

    /// <summary>Жалюзь полностью закрыты? (защищает от врага)</summary>
    public bool IsClosed => currentValue <= 5f;

    /// <summary>Жалюзь закрываются или уже закрыты (targetValue = 0)?</summary>
    public bool IsClosing => targetValue <= 0f;

    private void Start()
    {
        if (curtainMesh == null)
        {
            curtainMesh = GetComponentInChildren<SkinnedMeshRenderer>();
            if (curtainMesh == null)
            {
                Debug.LogError("[CurtainController] SkinnedMeshRenderer не найден! Перетащите Cube.001 в поле CurtainMesh.");
                return;
            }
        }

        // Начальное состояние — открыто (100)
        currentValue = 100f;
        curtainMesh.SetBlendShapeWeight(blendShapeIndex, currentValue);
    }

    private void Update()
    {
        // Нажатие F для переключения
        //if (Input.GetKeyDown(toggleKey))
        //{
        //    ToggleCurtain();
        //}

        if (curtainMesh == null) return;

        if (!Mathf.Approximately(currentValue, targetValue))
        {
            currentValue = Mathf.MoveTowards(currentValue, targetValue, animationSpeed * Time.deltaTime);
            curtainMesh.SetBlendShapeWeight(blendShapeIndex, currentValue);
        }
    }

    /// <summary>
    /// Переключить жалюзь (закрыть если открыта, открыть если закрыта).
    /// Можно также вызвать из OnClickEvent любого ClickableObject.
    /// </summary>
    public void ToggleCurtain()
    {
        if (targetValue > 50f)
            CloseCurtain();
        else
            OpenCurtain();
    }

    /// <summary>Закрыть жалюзь (даёт защиту от врага).</summary>
    public void CloseCurtain()
    {
        targetValue = 0f;
        Debug.Log("[CurtainController] Жалюзь закрываются... (враг не может атаковать)");
    }

    /// <summary>Открыть жалюзь (убирает защиту).</summary>
    public void OpenCurtain()
    {
        targetValue = 100f;
        Debug.Log("[CurtainController] Жалюзь открываются...");
    }
}
