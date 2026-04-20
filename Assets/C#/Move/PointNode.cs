using UnityEngine;
using System.Collections.Generic;

public class PointNode : MonoBehaviour
{
    [Header("Информация о точке")]
    public string nodeName = "Точка";
    public int nodeId = -1;

    [Header("Доступные пути из этой точки")]
    public List<ConnectedPath> connectedPaths = new List<ConnectedPath>();

    [Header("Визуализация (UI)")]
    public GameObject upArrow;
    public GameObject downArrow;
    public GameObject leftArrow;
    public GameObject rightArrow;

    [Header("Настройки Gizmos")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private float nodeRadius = 0.3f;
    [SerializeField] private float arrowSize = 0.5f;

    [System.Serializable]
    public class ConnectedPath
    {
        [Header("Направление")]
        public Direction direction;
        public KeyCode activationKey = KeyCode.W;

        [Header("Куда ведёт")]
        public int targetNodeId = -1;
        public string targetNodeName = "";

        [Header("Какой путь использовать")]
        public int pathId = -1;

        public bool isAvailable = true;

        public string GetKeyName()
        {
            return activationKey.ToString().Replace("Alpha", "").Replace("KeyCode", "");
        }
    }

    public enum Direction
    {
        Up, Down, Left, Right, None
    }

    private Dictionary<Direction, ConnectedPath> directionMap = new Dictionary<Direction, ConnectedPath>();

    // Кэш для Gizmos (чтобы не искать каждый раз)
    private static Dictionary<int, PointNode> allNodesCache;
    private static float lastCacheTime;

    private void Start()
    {
        BuildDirectionMap();
        UpdateArrows();
    }

    private void BuildDirectionMap()
    {
        directionMap.Clear();
        foreach (var path in connectedPaths)
        {
            if (!directionMap.ContainsKey(path.direction))
                directionMap[path.direction] = path;
        }
    }

    public bool IsDirectionAvailable(Direction dir)
    {
        return directionMap.ContainsKey(dir) && directionMap[dir].isAvailable;
    }

    public ConnectedPath GetConnectedPath(Direction dir)
    {
        if (directionMap.ContainsKey(dir) && directionMap[dir].isAvailable)
            return directionMap[dir];
        return null;
    }

    public int GetPathIdForDirection(Direction dir)
    {
        var path = GetConnectedPath(dir);
        return path != null ? path.pathId : -1;
    }

    public int GetTargetNodeId(Direction dir)
    {
        var path = GetConnectedPath(dir);
        return path != null ? path.targetNodeId : -1;
    }

    public KeyCode GetKeyForDirection(Direction dir)
    {
        var path = GetConnectedPath(dir);
        return path != null ? path.activationKey : KeyCode.None;
    }

    private void UpdateArrows()
    {
        UpdateArrow(upArrow, Direction.Up);
        UpdateArrow(downArrow, Direction.Down);
        UpdateArrow(leftArrow, Direction.Left);
        UpdateArrow(rightArrow, Direction.Right);
    }

    private void UpdateArrow(GameObject arrow, Direction dir)
    {
        if (arrow == null) return;

        bool isAvailable = IsDirectionAvailable(dir);
        arrow.SetActive(isAvailable);

        if (isAvailable)
        {
            var keyText = arrow.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (keyText != null)
            {
                keyText.text = GetKeyForDirection(dir).ToString().Replace("Alpha", "");
            }
        }
    }

    public void HideAllArrows()
    {
        if (upArrow != null) upArrow.SetActive(false);
        if (downArrow != null) downArrow.SetActive(false);
        if (leftArrow != null) leftArrow.SetActive(false);
        if (rightArrow != null) rightArrow.SetActive(false);
    }

    public void ShowAvailableArrows()
    {
        UpdateArrows();
    }

    // ========== GIZMOS ВИЗУАЛИЗАЦИЯ ==========

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Рисуем саму точку
        DrawNode();

        // Рисуем связи с другими точками
        DrawConnections();

        // Рисуем направление взгляда точки
        DrawLookDirection();
    }

    private void DrawNode()
    {
        // Основная сфера точки
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, nodeRadius);

        // Заливка точки (полупрозрачная)
        Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, nodeRadius - 0.05f);

        // ID точки
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f,
            $"{nodeName}\n(ID: {nodeId})");
#endif
    }

    private void DrawConnections()
    {
        foreach (var path in connectedPaths)
        {
            if (!path.isAvailable) continue;

            // Находим целевую точку
            PointNode targetNode = FindTargetNode(path);
            if (targetNode == null) continue;

            // Цвет линии зависит от направления
            Color lineColor = GetDirectionColor(path.direction);
            Gizmos.color = lineColor;

            // Рисуем линию между точками
            Vector3 startPos = transform.position;
            Vector3 endPos = targetNode.transform.position;
            Gizmos.DrawLine(startPos, endPos);

            // Рисуем стрелку направления (от текущей точки к целевой)
            DrawArrow(startPos, endPos, lineColor);

            // Рисуем метку с клавишей посередине
            DrawKeyLabel(startPos, endPos, path.activationKey);
        }
    }

    private void DrawArrow(Vector3 from, Vector3 to, Color color)
    {
        Vector3 direction = (to - from).normalized;
        float distance = Vector3.Distance(from, to);

        // Стрелка на 70% расстояния (ближе к целевой точке)
        Vector3 arrowPos = from + direction * (distance * 0.7f);

        // Размер стрелки зависит от расстояния
        float arrowLength = Mathf.Min(0.5f, distance * 0.15f);
        float arrowWidth = arrowLength * 0.5f;

        // Перпендикулярные векторы для стрелки
        Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
        Vector3 up = Vector3.Cross(right, direction).normalized;

        // Точки стрелки
        Vector3 arrowTip = arrowPos + direction * arrowLength;
        Vector3 arrowLeft = arrowPos - direction * arrowLength * 0.3f + right * arrowWidth;
        Vector3 arrowRight = arrowPos - direction * arrowLength * 0.3f - right * arrowWidth;

        Gizmos.color = color;
        Gizmos.DrawLine(arrowTip, arrowLeft);
        Gizmos.DrawLine(arrowTip, arrowRight);
        Gizmos.DrawLine(arrowLeft, arrowRight);
    }

    private void DrawKeyLabel(Vector3 from, Vector3 to, KeyCode key)
    {
#if UNITY_EDITOR
        Vector3 midPoint = (from + to) / 2;
        string keyName = key.ToString().Replace("Alpha", "");

        // Рисуем фон для текста
        UnityEditor.Handles.BeginGUI();

        // Получаем позицию в экранных координатах
        Vector3 screenPos = Camera.current.WorldToScreenPoint(midPoint);

        // Сохраняем текущий цвет GUI
        var originalColor = GUI.color;
        GUI.color = GetDirectionColorFromKey(key);

        // Рисуем текст
        GUIStyle style = new GUIStyle();
        style.normal.textColor = GetDirectionColorFromKey(key);
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 12;
        style.alignment = TextAnchor.MiddleCenter;

        // Тень для текста
        var shadowStyle = new GUIStyle(style);
        shadowStyle.normal.textColor = Color.black;

        UnityEditor.Handles.Label(midPoint + Vector3.up * 0.3f, $"  [{keyName}]  ", style);

        GUI.color = originalColor;
        UnityEditor.Handles.EndGUI();
#endif
    }

    private Color GetDirectionColor(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up: return Color.green;
            case Direction.Down: return new Color(1f, 0.5f, 0f); // Оранжевый
            case Direction.Left: return Color.cyan;
            case Direction.Right: return Color.magenta;
            default: return Color.gray;
        }
    }

    private Color GetDirectionColorFromKey(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.W: return Color.green;
            case KeyCode.S: return new Color(1f, 0.5f, 0f);
            case KeyCode.A: return Color.cyan;
            case KeyCode.D: return Color.magenta;
            case KeyCode.UpArrow: return Color.green;
            case KeyCode.DownArrow: return new Color(1f, 0.5f, 0f);
            case KeyCode.LeftArrow: return Color.cyan;
            case KeyCode.RightArrow: return Color.magenta;
            default: return Color.white;
        }
    }

    private void DrawLookDirection()
    {
        Gizmos.color = Color.blue;
        Vector3 forward = transform.forward;

        // Рисуем линию направления
        Gizmos.DrawRay(transform.position, forward * 1f);

        // Рисуем стрелку направления
        Vector3 arrowEnd = transform.position + forward * 1f;
        Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;

        Gizmos.DrawLine(arrowEnd, arrowEnd - forward * 0.2f + right * 0.15f);
        Gizmos.DrawLine(arrowEnd, arrowEnd - forward * 0.2f - right * 0.15f);

        // Сфера на конце
        Gizmos.DrawWireSphere(arrowEnd, 0.1f);
    }

    private PointNode FindTargetNode(ConnectedPath path)
    {
        // Обновляем кэш точек
        UpdateNodesCache();

        // Ищем по ID
        if (path.targetNodeId != -1 && allNodesCache.ContainsKey(path.targetNodeId))
            return allNodesCache[path.targetNodeId];

        // Ищем по имени
        if (!string.IsNullOrEmpty(path.targetNodeName))
        {
            foreach (var node in allNodesCache.Values)
            {
                if (node.nodeName == path.targetNodeName)
                    return node;
            }
        }

        return null;
    }

    private void UpdateNodesCache()
    {
        // Обновляем кэш раз в секунду
        if (allNodesCache == null || Time.time - lastCacheTime > 1f)
        {
            allNodesCache = new Dictionary<int, PointNode>();
            PointNode[] allNodes = FindObjectsOfType<PointNode>();
            foreach (var node in allNodes)
            {
                if (!allNodesCache.ContainsKey(node.nodeId))
                    allNodesCache[node.nodeId] = node;
            }
            lastCacheTime = Time.time;
        }
    }

    // ========== ОТЛАДОЧНАЯ ВИЗУАЛИЗАЦИЯ В РЕДАКТОРЕ ==========

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        // При выборе объекта показываем дополнительную информацию
#if UNITY_EDITOR
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, nodeRadius + 0.1f);

        // Показываем информацию о всех связях
        foreach (var path in connectedPaths)
        {
            if (!path.isAvailable) continue;

            string info = $"{path.direction}: {path.activationKey} -> ";
            if (path.targetNodeId != -1)
                info += $"Node {path.targetNodeId}";
            else
                info += path.targetNodeName;

            UnityEditor.Handles.Label(transform.position + Vector3.up * (0.8f + (float)path.direction * 0.3f), info);
        }
#endif
    }
}