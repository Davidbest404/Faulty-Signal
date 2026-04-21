using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PathMovementController : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float defaultMoveSpeed = 5f;
    [SerializeField] private float defaultRotationSpeed = 360f;
    [SerializeField] private float stopDistance = 0.05f;

    [Header("Ссылки")]
    [SerializeField] private Transform objectToMove;
    [SerializeField] private PointNode startNode;

    [Header("Список всех путей")]
    [SerializeField] private List<MovementPath> allPaths = new List<MovementPath>();

    [Header("Настройки Gizmos")]
    [SerializeField] private bool showPathGizmos = true;
    [SerializeField] private Color pathColor = Color.yellow;
    [SerializeField] private Color waypointColor = Color.cyan;

    // Состояние
    private bool isMoving = false;
    private MovementPath currentPath;
    private int currentPointIndex = 0;
    private PointNode currentNode;
    private PointNode targetNode;

    // События
    public System.Action<PointNode> OnNodeReached;
    public System.Action<int> OnPathStarted;
    public System.Action<int> OnPathCompleted;

    [System.Serializable]
    public class MovementPath
    {
        public int pathId = -1;
        public string pathName = "New Path";
        public int sourceNodeId = -1;
        public int targetNodeId = -1;
        public List<PathWaypoint> waypoints = new List<PathWaypoint>();
        public bool overrideSpeed = false;
        public float customMoveSpeed = 5f;
        public float customRotationSpeed = 360f;

        public void EnsureId(int id)
        {
            if (pathId == -1)
                pathId = id;
        }
    }

    [System.Serializable]
    public class PathWaypoint
    {
        public Transform pointTransform;
        public bool usePointRotation = true;
        public Vector3 customRotation;
        public float waitTime = 0f;
    }

    private void Start()
    {
        if (objectToMove == null)
            objectToMove = transform;

        // Генерация ID для путей
        for (int i = 0; i < allPaths.Count; i++)
        {
            allPaths[i].EnsureId(i);
        }

        // Устанавливаем начальную позицию
        if (startNode != null)
        {
            SetCurrentNode(startNode);
        }
    }

    private void Update()
    {
        if (!isMoving || currentPath == null) return;

        if (currentPointIndex >= currentPath.waypoints.Count)
        {
            CompleteMovement();
            return;
        }

        PathWaypoint currentWaypoint = currentPath.waypoints[currentPointIndex];
        if (currentWaypoint.pointTransform == null)
        {
            currentPointIndex++;
            return;
        }

        Vector3 targetPosition = currentWaypoint.pointTransform.position;
        float currentSpeed = GetCurrentMoveSpeed();

        // Движение
        objectToMove.position = Vector3.MoveTowards(objectToMove.position, targetPosition, currentSpeed * Time.deltaTime);

        // Поворот в сторону точки
        if (currentWaypoint.usePointRotation && currentWaypoint.pointTransform != null)
        {
            float currentRotSpeed = GetCurrentRotationSpeed();
            Quaternion targetRotation = currentWaypoint.pointTransform.rotation;
            objectToMove.rotation = Quaternion.RotateTowards(objectToMove.rotation, targetRotation, currentRotSpeed * Time.deltaTime);
        }
        else if (!currentWaypoint.usePointRotation)
        {
            Quaternion targetRotation = Quaternion.Euler(currentWaypoint.customRotation);
            float currentRotSpeed = GetCurrentRotationSpeed();
            objectToMove.rotation = Quaternion.RotateTowards(objectToMove.rotation, targetRotation, currentRotSpeed * Time.deltaTime);
        }

        // Проверка достижения точки
        if (Vector3.Distance(objectToMove.position, targetPosition) <= stopDistance)
        {
            if (currentWaypoint.waitTime > 0)
            {
                StartCoroutine(WaitAtWaypoint(currentWaypoint.waitTime));
            }
            else
            {
                currentPointIndex++;
            }
        }
    }

    private System.Collections.IEnumerator WaitAtWaypoint(float waitTime)
    {
        isMoving = false;
        yield return new WaitForSeconds(waitTime);
        isMoving = true;
        currentPointIndex++;
    }

    private float GetCurrentMoveSpeed()
    {
        if (currentPath.overrideSpeed)
            return currentPath.customMoveSpeed;
        return defaultMoveSpeed;
    }

    private float GetCurrentRotationSpeed()
    {
        if (currentPath.overrideSpeed)
            return currentPath.customRotationSpeed;
        return defaultRotationSpeed;
    }

    private void CompleteMovement()
    {
        isMoving = false;

        // Находим целевую точку
        targetNode = FindNodeById(currentPath.targetNodeId);

        if (targetNode != null)
        {
            // Вызываем событие ВЫХОДА из старой точки
            if (currentNode != null)
            {
                currentNode.TriggerExitActions();
            }

            // Устанавливаем новую точку
            SetCurrentNode(targetNode);

            // Вызываем событие ВХОДА в новую точку
            if (currentNode != null)
            {
                currentNode.TriggerEnterActions();
            }

            OnNodeReached?.Invoke(currentNode);
        }

        OnPathCompleted?.Invoke(currentPath.pathId);

        Debug.Log($"Путь '{currentPath.pathName}' завершён! Текущая точка: {(currentNode != null ? currentNode.nodeName : "None")}");
    }

    private PointNode FindNodeById(int nodeId)
    {
        PointNode[] allNodes = FindObjectsOfType<PointNode>();
        return allNodes.FirstOrDefault(n => n.nodeId == nodeId);
    }

    private MovementPath FindPathById(int pathId)
    {
        return allPaths.FirstOrDefault(p => p.pathId == pathId);
    }

    // ========== ПУБЛИЧНЫЕ МЕТОДЫ ==========

    /// <summary>
    /// Попытаться переместиться в указанном направлении
    /// </summary>
    public bool TryMove(PointNode.Direction direction)
    {
        if (isMoving)
        {
            Debug.Log("Уже движется!");
            return false;
        }

        if (currentNode == null)
        {
            Debug.LogError("Нет текущей точки!");
            return false;
        }

        var connectedPath = currentNode.GetConnectedPath(direction);
        if (connectedPath == null || !connectedPath.isAvailable)
        {
            Debug.Log($"Направление {direction} недоступно!");
            return false;
        }

        MovementPath targetPath = FindPathById(connectedPath.pathId);
        if (targetPath == null)
        {
            Debug.LogError($"Путь с ID {connectedPath.pathId} не найден!");
            return false;
        }

        // Запускаем движение
        StartMovement(targetPath);
        return true;
    }

    /// <summary>
    /// Запустить движение по пути
    /// </summary>
    public void StartMovement(MovementPath path)
    {
        if (isMoving) return;

        currentPath = path;
        currentPointIndex = 0;
        isMoving = true;

        // Скрываем стрелки на стартовой точке
        if (currentNode != null)
            currentNode.HideAllArrows();

        OnPathStarted?.Invoke(currentPath.pathId);

        Debug.Log($"Запущен путь: {currentPath.pathName} (ID: {currentPath.pathId})");

        // Вызываем событие ВЫХОДА из текущей точки (перед началом движения)
        if (currentNode != null)
        {
            currentNode.TriggerExitActions();
        }

        currentPath = path;
        currentPointIndex = 0;
        isMoving = true;

        if (currentNode != null)
            currentNode.HideAllArrows();

        OnPathStarted?.Invoke(currentPath.pathId);
        Debug.Log($"Запущен путь: {currentPath.pathName} (ID: {currentPath.pathId})");
    }

    /// <summary>
    /// Запустить путь по ID
    /// </summary>
    public void StartPathById(int pathId)
    {
        MovementPath path = FindPathById(pathId);
        if (path != null)
            StartMovement(path);
        else
            Debug.LogError($"Путь с ID {pathId} не найден!");
    }

    /// <summary>
    /// Установить текущую точку (при загрузке или телепортации)
    /// </summary>
    public void SetCurrentNode(PointNode node)
    {
        currentNode = node;
        objectToMove.position = node.transform.position;
        objectToMove.rotation = node.transform.rotation;
        currentNode.ShowAvailableArrows();

        Debug.Log($"Установлена точка: {currentNode.nodeName}");
    }

    /// <summary>
    /// Остановить движение
    /// </summary>
    public void StopMovement()
    {
        isMoving = false;
    }

    /// <summary>
    /// Проверить, движется ли объект
    /// </summary>
    public bool IsMoving()
    {
        return isMoving;
    }

    /// <summary>
    /// Получить текущую точку
    /// </summary>
    public PointNode GetCurrentNode()
    {
        return currentNode;
    }

    /// <summary>
    /// Получить все пути
    /// </summary>
    public List<MovementPath> GetAllPaths()
    {
        return allPaths;
    }

    // ========== GIZMOS ВИЗУАЛИЗАЦИЯ ПУТЕЙ ==========

    private void OnDrawGizmos()
    {
        if (!showPathGizmos) return;

        foreach (var path in allPaths)
        {
            DrawPath(path);
        }
    }

    private void DrawPath(MovementPath path)
    {
        if (path.waypoints == null || path.waypoints.Count < 2) return;

        // Рисуем линии между всеми точками пути
        for (int i = 0; i < path.waypoints.Count - 1; i++)
        {
            var current = path.waypoints[i];
            var next = path.waypoints[i + 1];

            if (current.pointTransform != null && next.pointTransform != null)
            {
                Gizmos.color = pathColor;
                Gizmos.DrawLine(current.pointTransform.position, next.pointTransform.position);
                DrawPathArrow(current.pointTransform.position, next.pointTransform.position, pathColor);
            }
        }

        // Рисуем все вейпоинты
        foreach (var waypoint in path.waypoints)
        {
            if (waypoint.pointTransform != null)
            {
                Gizmos.color = waypointColor;
                Gizmos.DrawWireSphere(waypoint.pointTransform.position, 0.2f);

                if (waypoint.usePointRotation)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(waypoint.pointTransform.position, waypoint.pointTransform.forward * 0.5f);
                }
            }
        }

        // Подпись пути
#if UNITY_EDITOR
        if (path.waypoints.Count > 0 && path.waypoints[0].pointTransform != null)
        {
            UnityEditor.Handles.Label(
                path.waypoints[0].pointTransform.position + Vector3.up * 0.5f,
                $"Path: {path.pathName} (ID: {path.pathId})"
            );
        }
#endif
    }

    private void DrawPathArrow(Vector3 from, Vector3 to, Color color)
    {
        Vector3 direction = (to - from).normalized;
        float distance = Vector3.Distance(from, to);

        Vector3 arrowPos = from + direction * (distance * 0.5f);
        float arrowLength = Mathf.Min(0.3f, distance * 0.2f);
        float arrowWidth = arrowLength * 0.5f;

        Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;

        Vector3 arrowTip = arrowPos + direction * arrowLength;
        Vector3 arrowLeft = arrowPos - direction * arrowLength * 0.3f + right * arrowWidth;
        Vector3 arrowRight = arrowPos - direction * arrowLength * 0.3f - right * arrowWidth;

        Gizmos.color = color;
        Gizmos.DrawLine(arrowTip, arrowLeft);
        Gizmos.DrawLine(arrowTip, arrowRight);
        Gizmos.DrawLine(arrowLeft, arrowRight);
    }
}