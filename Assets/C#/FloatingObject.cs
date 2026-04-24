using UnityEngine;
using System.Collections;

public class FloatingObject : MonoBehaviour
{
    [Header("Vertical Bobbing")]
    [SerializeField] private float floatAmplitude = 0.5f;
    [SerializeField] private float floatSpeed = 1.5f;

    [Header("Horizontal Sway")]
    [SerializeField] private float swayAmplitude = 0.3f;
    [SerializeField] private float swaySpeed = 1.2f;

    [Header("Rotation")]
    [SerializeField] private float rotateAmplitude = 15f;
    [SerializeField] private float rotateSpeed = 1f;

    [Header("Smoothing")]
    [SerializeField] private float smoothness = 5f;

    [Header("Randomization")]
    [SerializeField] private bool useRandomOffsets = true;

    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 1f;

    // Смещения для каждого объекта
    private float positionOffsetX;
    private float positionOffsetZ;
    private float rotationOffsetX;
    private float rotationOffsetZ;
    private float verticalOffset;

    // Локальная базовая позиция и поворот
    private Vector3 startLocalPosition;
    private Quaternion startLocalRotation;

    // Текущая целевая локальная позиция для плавания
    private Vector3 targetBaseLocalPosition;
    private Quaternion targetBaseLocalRotation;

    // Состояние плавания
    private bool isFloating = true;
    private bool isTransitioning = false;

    // Для отслеживания изменений родителя
    private Transform parentTransform;
    private Vector3 lastParentPosition;
    private Quaternion lastParentRotation;

    // Кэшированные компоненты
    private Transform cachedTransform;

    private void Awake()
    {
        cachedTransform = transform;
        parentTransform = cachedTransform.parent;
    }

    private void Start()
    {
        // Сохраняем начальные локальные значения
        startLocalPosition = cachedTransform.localPosition;
        startLocalRotation = cachedTransform.localRotation;
        targetBaseLocalPosition = startLocalPosition;
        targetBaseLocalRotation = startLocalRotation;

        // Сохраняем позицию родителя для отслеживания
        if (parentTransform != null)
        {
            lastParentPosition = parentTransform.position;
            lastParentRotation = parentTransform.rotation;
        }

        // Генерируем случайные смещения
        if (useRandomOffsets)
        {
            positionOffsetX = Random.Range(0f, Mathf.PI * 2);
            positionOffsetZ = Random.Range(0f, Mathf.PI * 2);
            rotationOffsetX = Random.Range(0f, Mathf.PI * 2);
            rotationOffsetZ = Random.Range(0f, Mathf.PI * 2);
            verticalOffset = Random.Range(0f, Mathf.PI * 2);
        }
        else
        {
            positionOffsetX = 0;
            positionOffsetZ = 0;
            rotationOffsetX = 0;
            rotationOffsetZ = 0;
            verticalOffset = 0;
        }
    }

    private void Update()
    {
        if (isTransitioning) return;

        if (isFloating)
        {
            UpdateFloatingMotion();
        }
    }

    private void UpdateFloatingMotion()
    {
        // Вертикальное движение (локальное)
        float verticalMove = Mathf.Sin(Time.time * floatSpeed + verticalOffset) * floatAmplitude;

        // Горизонтальное покачивание (локальное)
        float horizontalMoveX = Mathf.Sin(Time.time * swaySpeed + positionOffsetX) * swayAmplitude;
        float horizontalMoveZ = Mathf.Cos(Time.time * swaySpeed + positionOffsetZ) * swayAmplitude;

        // Новая локальная позиция относительно базовой
        Vector3 newLocalPosition = targetBaseLocalPosition + new Vector3(horizontalMoveX, verticalMove, horizontalMoveZ);

        // Плавное перемещение в локальных координатах
        cachedTransform.localPosition = Vector3.Lerp(cachedTransform.localPosition, newLocalPosition, Time.deltaTime * smoothness);

        // Вращение (локальное)
        float rotateX = Mathf.Sin(Time.time * rotateSpeed + rotationOffsetX) * rotateAmplitude;
        float rotateZ = Mathf.Cos(Time.time * rotateSpeed + rotationOffsetZ) * rotateAmplitude;
        float rotateY = Mathf.Sin(Time.time * rotateSpeed * 0.7f + rotationOffsetX) * (rotateAmplitude * 0.5f);

        Quaternion newLocalRotation = targetBaseLocalRotation * Quaternion.Euler(rotateX, rotateY, rotateZ);
        cachedTransform.localRotation = Quaternion.Slerp(cachedTransform.localRotation, newLocalRotation, Time.deltaTime * smoothness);
    }

    /// <summary>
    /// Остановить плавание и плавно вернуться на начальную локальную позицию
    /// </summary>
    public void StopFloatingAndReset()
    {
        StopAllCoroutines();
        StartCoroutine(TransitionToLocalPosition(startLocalPosition, startLocalRotation, false));
    }

    /// <summary>
    /// Остановить плавание и плавно переместиться на указанную локальную позицию
    /// </summary>
    public void StopFloatingAndMoveToLocal(Vector3 targetLocalPos, Quaternion targetLocalRot)
    {
        StopAllCoroutines();
        StartCoroutine(TransitionToLocalPosition(targetLocalPos, targetLocalRot, false));
    }

    /// <summary>
    /// Запустить плавание на текущей позиции
    /// </summary>
    public void StartFloating()
    {
        StopAllCoroutines();
        StartCoroutine(StartFloatingCoroutine());
    }

    /// <summary>
    /// Запустить плавание на указанной локальной позиции
    /// </summary>
    public void StartFloatingAtLocalPosition(Vector3 newBaseLocalPosition)
    {
        StartFloatingAtLocalPosition(newBaseLocalPosition, Quaternion.identity);
    }

    /// <summary>
    /// Запустить плавание на указанной локальной позиции и повороте
    /// </summary>
    public void StartFloatingAtLocalPosition(Vector3 newBaseLocalPosition, Quaternion newBaseLocalRotation)
    {
        StopAllCoroutines();
        StartCoroutine(StartFloatingAtLocalPositionCoroutine(newBaseLocalPosition, newBaseLocalRotation));
    }

    /// <summary>
    /// Изменить базовую локальную позицию для плавания
    /// </summary>
    public void SetFloatingBaseLocalPosition(Vector3 newBaseLocalPosition, bool smoothTransition = true)
    {
        if (smoothTransition)
        {
            StopAllCoroutines();
            StartCoroutine(SmoothChangeBaseLocalPosition(newBaseLocalPosition, targetBaseLocalRotation));
        }
        else
        {
            targetBaseLocalPosition = newBaseLocalPosition;
        }
    }

    /// <summary>
    /// Изменить базовую локальную позицию и поворот для плавания
    /// </summary>
    public void SetFloatingBaseLocalPosition(Vector3 newBaseLocalPosition, Quaternion newBaseLocalRotation, bool smoothTransition = true)
    {
        if (smoothTransition)
        {
            StopAllCoroutines();
            StartCoroutine(SmoothChangeBaseLocalPosition(newBaseLocalPosition, newBaseLocalRotation));
        }
        else
        {
            targetBaseLocalPosition = newBaseLocalPosition;
            targetBaseLocalRotation = newBaseLocalRotation;
        }
    }

    /// <summary>
    /// Сбросить базовую позицию на изначальную
    /// </summary>
    public void ResetFloatingBaseLocalPosition()
    {
        SetFloatingBaseLocalPosition(startLocalPosition, startLocalRotation);
    }

    /// <summary>
    /// Пауза плавания (замирает на текущей позиции)
    /// </summary>
    public void PauseFloating()
    {
        isFloating = false;
    }

    /// <summary>
    /// Возобновить плавание
    /// </summary>
    public void ResumeFloating()
    {
        isFloating = true;
    }

    /// <summary>
    /// Проверить, активен ли эффект плавания
    /// </summary>
    public bool IsFloating()
    {
        return isFloating;
    }

    /// <summary>
    /// Получить текущую базовую локальную позицию
    /// </summary>
    public Vector3 GetCurrentBaseLocalPosition()
    {
        return targetBaseLocalPosition;
    }

    // Корoutine для плавного перехода к остановке
    private IEnumerator TransitionToLocalPosition(Vector3 targetLocalPos, Quaternion targetLocalRot, bool enableFloatingAfterTransition)
    {
        isTransitioning = true;
        isFloating = false;

        Vector3 startLocalPos = cachedTransform.localPosition;
        Quaternion startLocalRot = cachedTransform.localRotation;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            cachedTransform.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, smoothT);
            cachedTransform.localRotation = Quaternion.Slerp(startLocalRot, targetLocalRot, smoothT);

            yield return null;
        }

        cachedTransform.localPosition = targetLocalPos;
        cachedTransform.localRotation = targetLocalRot;

        isTransitioning = false;

        if (enableFloatingAfterTransition)
        {
            isFloating = true;
        }
    }

    // Корoutine для запуска плавания на текущей позиции
    private IEnumerator StartFloatingCoroutine()
    {
        if (Vector3.Distance(cachedTransform.localPosition, targetBaseLocalPosition) > 0.01f)
        {
            yield return StartCoroutine(TransitionToLocalPosition(targetBaseLocalPosition, targetBaseLocalRotation, true));
        }
        else
        {
            isFloating = true;
        }
    }

    // Корoutine для запуска плавания на новой локальной позиции
    private IEnumerator StartFloatingAtLocalPositionCoroutine(Vector3 newBaseLocalPosition, Quaternion newBaseLocalRotation)
    {
        Vector3 oldBaseLocalPosition = targetBaseLocalPosition;
        Quaternion oldBaseLocalRotation = targetBaseLocalRotation;

        targetBaseLocalPosition = newBaseLocalPosition;
        targetBaseLocalRotation = newBaseLocalRotation;

        if (Vector3.Distance(cachedTransform.localPosition, newBaseLocalPosition) > 0.01f)
        {
            yield return StartCoroutine(TransitionToLocalPosition(newBaseLocalPosition, newBaseLocalRotation, true));
        }
        else
        {
            isFloating = true;
        }
    }

    // Корoutine для плавного изменения базовой локальной позиции во время плавания
    private IEnumerator SmoothChangeBaseLocalPosition(Vector3 newBaseLocalPosition, Quaternion newBaseLocalRotation)
    {
        isTransitioning = true;

        Vector3 startBaseLocalPos = targetBaseLocalPosition;
        Quaternion startBaseLocalRot = targetBaseLocalRotation;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            targetBaseLocalPosition = Vector3.Lerp(startBaseLocalPos, newBaseLocalPosition, smoothT);
            targetBaseLocalRotation = Quaternion.Slerp(startBaseLocalRot, newBaseLocalRotation, smoothT);

            yield return null;
        }

        targetBaseLocalPosition = newBaseLocalPosition;
        targetBaseLocalRotation = newBaseLocalRotation;

        isTransitioning = false;
    }

    // Визуализация в Editor
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && cachedTransform != null)
        {
            // Показываем базовую локальную позицию
            Gizmos.color = Color.green;
            Vector3 worldBasePos = cachedTransform.parent != null ?
                cachedTransform.parent.TransformPoint(targetBaseLocalPosition) :
                targetBaseLocalPosition;
            Gizmos.DrawWireSphere(worldBasePos, 0.2f);

            // Показываем стартовую локальную позицию
            Gizmos.color = Color.blue;
            Vector3 worldStartPos = cachedTransform.parent != null ?
                cachedTransform.parent.TransformPoint(startLocalPosition) :
                startLocalPosition;
            Gizmos.DrawWireSphere(worldStartPos, 0.2f);
        }
    }
}