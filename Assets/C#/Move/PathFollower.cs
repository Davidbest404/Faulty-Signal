using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PathFollower : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private List<PathData> paths = new List<PathData>();  // Список всех путей
    [SerializeField] private int defaultPathIndex = 0;                     // Какой путь использовать по умолчанию
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float stopDistance = 0.1f;

    [Header("Настройки цикла")]
    [SerializeField] private bool loopPath = false;
    [SerializeField] private bool autoStart = false;  // Теперь не автостарт, нужно вызывать из другого скрипта

    [Header("Визуализация")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color pathColor = Color.green;
    [SerializeField] private Color pointColor = Color.red;
    [SerializeField] private Color lookDirectionColor = Color.blue;
    [SerializeField] private Color activePathColor = Color.yellow;

    private int currentPathIndex = -1;      // Какой путь сейчас активен
    private int currentPointIndex = 0;
    private bool isMoving = false;
    private Transform objectToMove;
    private Quaternion targetRotation;
    private PathData currentPath;
    private string currentPathName = "";

    [System.Serializable]
    public class PathPoint
    {
        public Transform pointTransform;
        public bool usePointRotation = true;
        public float waitTime = 0f;
        public bool isActive = true;

        [Header("Опционально")]
        public float customSpeed = -1f;
        public Vector3 customRotation;
    }

    [System.Serializable]
    public class PathData
    {
        [Header("Информация о пути")]
        public string pathName = "New Path";  // Имя пути для идентификации
        public int pathId = -1;               // Уникальный ID (можно не заполнять, авто-генерация)

        [Header("Точки пути")]
        public List<PathPoint> pathPoints = new List<PathPoint>();

        [Header("Настройки пути (переопределяют глобальные)")]
        public bool overrideGlobalSpeed = false;
        public float customMoveSpeed = 5f;
        public bool overrideGlobalRotationSpeed = false;
        public float customRotationSpeed = 5f;
        public bool overrideLoop = false;
        public bool customLoop = false;

        [Header("События пути")]
        public bool invokeEventOnComplete = false;
        public string onCompleteEventName = "";

        // Внутренние переменные
        [System.NonSerialized]
        public bool isCompleted = false;

        // Авто-генерация ID
        public void EnsureId(int generatedId)
        {
            if (pathId == -1)
                pathId = generatedId;
        }
    }

    private void Start()
    {
        objectToMove = transform;

        // Авто-генерация ID для путей
        for (int i = 0; i < paths.Count; i++)
        {
            paths[i].EnsureId(i);
        }

        // Если autoStart включён, запускаем путь по умолчанию
        if (autoStart && paths.Count > 0 && defaultPathIndex < paths.Count)
        {
            StartPath(defaultPathIndex);
        }
    }

    private void Update()
    {
        if (!isMoving || currentPath == null) return;
        if (currentPath.pathPoints == null || currentPath.pathPoints.Count == 0) return;

        PathPoint currentPoint = currentPath.pathPoints[currentPointIndex];

        if (!currentPoint.isActive)
        {
            MoveToNextPoint();
            return;
        }

        Vector3 targetPosition = currentPoint.pointTransform.position;

        // Движение к точке
        float currentSpeed = GetCurrentMoveSpeed();
        objectToMove.position = Vector3.MoveTowards(objectToMove.position, targetPosition, currentSpeed * Time.deltaTime);

        // Поворот
        if (currentPoint.usePointRotation)
        {
            targetRotation = currentPoint.pointTransform.rotation;
        }
        else
        {
            targetRotation = Quaternion.Euler(currentPoint.customRotation);
        }

        float currentRotSpeed = GetCurrentRotationSpeed();
        objectToMove.rotation = Quaternion.Slerp(objectToMove.rotation, targetRotation, currentRotSpeed * Time.deltaTime);

        // Проверка достижения точки
        if (Vector3.Distance(objectToMove.position, targetPosition) <= stopDistance)
        {
            OnReachedPoint(currentPointIndex);
        }
    }

    private float GetCurrentMoveSpeed()
    {
        PathPoint currentPoint = currentPath.pathPoints[currentPointIndex];
        if (currentPoint.customSpeed > 0)
            return currentPoint.customSpeed;

        if (currentPath.overrideGlobalSpeed)
            return currentPath.customMoveSpeed;

        return moveSpeed;
    }

    private float GetCurrentRotationSpeed()
    {
        if (currentPath.overrideGlobalRotationSpeed)
            return currentPath.customRotationSpeed;

        return rotationSpeed;
    }

    private bool GetCurrentLoopSetting()
    {
        if (currentPath.overrideLoop)
            return currentPath.customLoop;

        return loopPath;
    }

    private void OnReachedPoint(int pointIndex)
    {
        PathPoint reachedPoint = currentPath.pathPoints[pointIndex];

        // Точно устанавливаем поворот
        if (reachedPoint.usePointRotation)
        {
            objectToMove.rotation = reachedPoint.pointTransform.rotation;
        }
        else
        {
            objectToMove.rotation = Quaternion.Euler(reachedPoint.customRotation);
        }

        OnPointReached?.Invoke(currentPathIndex, pointIndex, reachedPoint.pointTransform);

        if (reachedPoint.waitTime > 0)
        {
            StartCoroutine(WaitAtPoint(reachedPoint.waitTime));
        }
        else
        {
            MoveToNextPoint();
        }
    }

    private System.Collections.IEnumerator WaitAtPoint(float waitTime)
    {
        isMoving = false;
        yield return new WaitForSeconds(waitTime);
        MoveToNextPoint();
    }

    private void MoveToNextPoint()
    {
        if (currentPointIndex + 1 < currentPath.pathPoints.Count)
        {
            currentPointIndex++;
            isMoving = true;
        }
        else if (GetCurrentLoopSetting())
        {
            currentPointIndex = 0;
            isMoving = true;
        }
        else
        {
            isMoving = false;
            currentPath.isCompleted = true;
            OnPathComplete?.Invoke(currentPathIndex, currentPath.pathName);

            // Вызываем событие пути
            if (currentPath.invokeEventOnComplete)
            {
                Debug.Log($"Путь '{currentPath.pathName}' завершён! Событие: {currentPath.onCompleteEventName}");
                OnAnyPathComplete?.Invoke(currentPathIndex, currentPath.pathName, currentPath.onCompleteEventName);
            }
        }
    }

    // ========== ПУБЛИЧНЫЕ МЕТОДЫ ДЛЯ УПРАВЛЕНИЯ ==========

    /// <summary>
    /// Запустить путь по индексу
    /// </summary>
    public void StartPath(int pathIndex)
    {
        if (pathIndex < 0 || pathIndex >= paths.Count)
        {
            Debug.LogError($"Путь с индексом {pathIndex} не найден!");
            return;
        }

        StartPathInternal(pathIndex);
    }

    /// <summary>
    /// Запустить путь по имени
    /// </summary>
    public void StartPath(string pathName)
    {
        int index = paths.FindIndex(p => p.pathName == pathName);
        if (index == -1)
        {
            Debug.LogError($"Путь с именем '{pathName}' не найден!");
            return;
        }

        StartPathInternal(index);
    }

    /// <summary>
    /// Запустить путь по ID
    /// </summary>
    public void StartPathById(int pathId)
    {
        int index = paths.FindIndex(p => p.pathId == pathId);
        if (index == -1)
        {
            Debug.LogError($"Путь с ID {pathId} не найден!");
            return;
        }

        StartPathInternal(index);
    }

    private void StartPathInternal(int index)
    {
        // Останавливаем текущее движение
        isMoving = false;

        currentPathIndex = index;
        currentPath = paths[currentPathIndex];
        currentPathName = currentPath.pathName;
        currentPointIndex = 0;
        currentPath.isCompleted = false;

        // Проверяем, есть ли точки
        if (currentPath.pathPoints == null || currentPath.pathPoints.Count == 0)
        {
            Debug.LogWarning($"Путь '{currentPathName}' не содержит точек!");
            return;
        }

        // Телепортируем на первую точку
        Transform firstPoint = currentPath.pathPoints[0].pointTransform;
        if (firstPoint != null)
        {
            objectToMove.position = firstPoint.position;

            // Устанавливаем поворот первой точки
            if (currentPath.pathPoints[0].usePointRotation)
                objectToMove.rotation = firstPoint.rotation;
            else
                objectToMove.rotation = Quaternion.Euler(currentPath.pathPoints[0].customRotation);
        }

        isMoving = true;
        Debug.Log($"Запущен путь: '{currentPathName}' (Индекс: {currentPathIndex}, ID: {currentPath.pathId})");
        OnPathStarted?.Invoke(currentPathIndex, currentPathName);
    }

    /// <summary>
    /// Остановить движение
    /// </summary>
    public void StopMovement()
    {
        isMoving = false;
        Debug.Log($"Движение остановлено на пути: {currentPathName}");
    }

    /// <summary>
    /// Возобновить движение
    /// </summary>
    public void ResumeMovement()
    {
        if (currentPath != null && !currentPath.isCompleted)
        {
            isMoving = true;
            Debug.Log($"Движение возобновлено на пути: {currentPathName}");
        }
    }

    /// <summary>
    /// Получить список всех путей
    /// </summary>
    public List<string> GetAllPathNames()
    {
        return paths.Select(p => p.pathName).ToList();
    }

    /// <summary>
    /// Получить текущий активный путь
    /// </summary>
    public string GetCurrentPathName()
    {
        return currentPathName;
    }

    /// <summary>
    /// Получить индекс текущего пути
    /// </summary>
    public int GetCurrentPathIndex()
    {
        return currentPathIndex;
    }

    /// <summary>
    /// Проверить, движется ли объект
    /// </summary>
    public bool IsMoving()
    {
        return isMoving;
    }

    /// <summary>
    /// Проверить, завершён ли текущий путь
    /// </summary>
    public bool IsCurrentPathComplete()
    {
        return currentPath != null && currentPath.isCompleted;
    }

    /// <summary>
    /// Активировать/деактивировать конкретную точку в конкретном пути
    /// </summary>
    public void SetPointActive(int pathIndex, int pointIndex, bool active)
    {
        if (pathIndex >= 0 && pathIndex < paths.Count)
        {
            if (pointIndex >= 0 && pointIndex < paths[pathIndex].pathPoints.Count)
            {
                paths[pathIndex].pathPoints[pointIndex].isActive = active;
            }
        }
    }

    /// <summary>
    /// Активировать/деактивировать точку в текущем пути
    /// </summary>
    public void SetCurrentPointActive(int pointIndex, bool active)
    {
        if (currentPath != null && pointIndex >= 0 && pointIndex < currentPath.pathPoints.Count)
        {
            currentPath.pathPoints[pointIndex].isActive = active;
        }
    }

    /// <summary>
    /// Добавить новый путь динамически
    /// </summary>
    public void AddPath(string pathName, List<Transform> points)
    {
        PathData newPath = new PathData();
        newPath.pathName = pathName;
        newPath.pathId = paths.Count;

        foreach (var point in points)
        {
            PathPoint newPoint = new PathPoint();
            newPoint.pointTransform = point;
            newPoint.usePointRotation = true;
            newPoint.isActive = true;
            newPath.pathPoints.Add(newPoint);
        }

        paths.Add(newPath);
        Debug.Log($"Добавлен новый путь: {pathName} с {points.Count} точками");
    }

    /// <summary>
    /// Удалить путь
    /// </summary>
    public void RemovePath(int pathIndex)
    {
        if (pathIndex >= 0 && pathIndex < paths.Count)
        {
            if (currentPathIndex == pathIndex)
            {
                StopMovement();
                currentPath = null;
            }
            paths.RemoveAt(pathIndex);
            Debug.Log($"Удалён путь с индексом {pathIndex}");
        }
    }

    // ========== СОБЫТИЯ ==========

    /// <summary>
    /// Событие при достижении точки (индекс пути, индекс точки, Transform точки)
    /// </summary>
    public System.Action<int, int, Transform> OnPointReached;

    /// <summary>
    /// Событие при завершении пути (индекс пути, имя пути)
    /// </summary>
    public System.Action<int, string> OnPathComplete;

    /// <summary>
    /// Событие при старте пути (индекс пути, имя пути)
    /// </summary>
    public System.Action<int, string> OnPathStarted;

    /// <summary>
    /// Событие при завершении ЛЮБОГО пути (индекс пути, имя пути, имя события)
    /// </summary>
    public System.Action<int, string, string> OnAnyPathComplete;

    // ========== ВИЗУАЛИЗАЦИЯ ==========

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        for (int p = 0; p < paths.Count; p++)
        {
            var path = paths[p];
            if (path.pathPoints == null || path.pathPoints.Count < 2) continue;

            // Выбираем цвет для текущего активного пути
            if (p == currentPathIndex && Application.isPlaying)
                Gizmos.color = activePathColor;
            else
                Gizmos.color = pathColor;

            // Рисуем линии между точками
            for (int i = 0; i < path.pathPoints.Count - 1; i++)
            {
                if (path.pathPoints[i].pointTransform != null && path.pathPoints[i + 1].pointTransform != null)
                {
                    Gizmos.DrawLine(
                        path.pathPoints[i].pointTransform.position,
                        path.pathPoints[i + 1].pointTransform.position
                    );
                }
            }

            // Рисуем точки
            foreach (var point in path.pathPoints)
            {
                if (point.pointTransform != null)
                {
                    Gizmos.color = point.isActive ? pointColor : Color.gray;
                    Gizmos.DrawWireSphere(point.pointTransform.position, 0.3f);

                    Gizmos.color = lookDirectionColor;
                    Vector3 forward = point.pointTransform.forward;
                    Gizmos.DrawRay(point.pointTransform.position, forward * 0.8f);

                    Vector3 arrowEnd = point.pointTransform.position + forward * 0.8f;
                    Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
                    Gizmos.DrawLine(arrowEnd, arrowEnd - forward * 0.2f + right * 0.15f);
                    Gizmos.DrawLine(arrowEnd, arrowEnd - forward * 0.2f - right * 0.15f);
                }
            }
        }
    }
}