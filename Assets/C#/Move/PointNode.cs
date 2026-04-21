using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

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

    [Header("События при входе в точку")]
    public UnityEvent OnEnterNode;           // Событие при входе
    public List<NodeAction> onEnterActions = new List<NodeAction>();  // Действия при входе

    [Header("События при выходе из точки")]
    public UnityEvent OnExitNode;            // Событие при выходе
    public List<NodeAction> onExitActions = new List<NodeAction>();   // Действия при выходе

    [Header("Настройки Gizmos")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private float nodeRadius = 0.3f;
    [SerializeField] private float arrowSize = 0.5f;

    [System.Serializable]
    public class ConnectedPath
    {
        public Direction direction;
        public KeyCode activationKey = KeyCode.W;
        public int targetNodeId = -1;
        public string targetNodeName = "";
        public int pathId = -1;
        public bool isAvailable = true;

        public string GetKeyName()
        {
            return activationKey.ToString().Replace("Alpha", "").Replace("KeyCode", "");
        }
    }

    [System.Serializable]
    public class NodeAction
    {
        public string actionName = "Новое действие";
        public ActionType actionType = ActionType.ActivateGameObject;

        // Для ActivateGameObject / DeactivateGameObject
        public GameObject targetObject;

        // Для CallMethod
        public MonoBehaviour targetComponent;
        public string methodName = "";

        // Для SetBool / SetTrigger (Animator)
        public Animator targetAnimator;
        public string parameterName = "";
        public bool boolValue = true;

        // Для SendMessage
        public GameObject messageTarget;
        public string messageName = "";

        // Для задержки
        public float delay = 0f;
    }

    public enum ActionType
    {
        ActivateGameObject,     // Активировать объект
        DeactivateGameObject,   // Деактивировать объект
        CallMethod,             // Вызвать метод на компоненте
        SetBoolTrue,            // Установить bool параметр аниматора в true
        SetBoolFalse,           // Установить bool параметр аниматора в false
        SetTrigger,             // Запустить триггер аниматора
        SendMessage,            // Отправить сообщение
        PlaySound,              // Воспроизвести звук
        CustomEvent             // Пользовательское событие
    }

    public enum Direction
    {
        Up, Down, Left, Right, None
    }

    private Dictionary<Direction, ConnectedPath> directionMap = new Dictionary<Direction, ConnectedPath>();
    private static Dictionary<int, PointNode> allNodesCache;
    private static float lastCacheTime;
    private AudioSource audioSource;
    private bool hasTriggeredEnter = false;

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

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

    // Вызывается когда игрок ВХОДИТ в эту точку (из PathMovementController)
    public void TriggerEnterActions()
    {
        if (hasTriggeredEnter) return;
        hasTriggeredEnter = true;

        Debug.Log($"Вход в точку: {nodeName}");

        // Вызываем UnityEvent
        OnEnterNode?.Invoke();

        // Выполняем все действия
        foreach (var action in onEnterActions)
        {
            ExecuteAction(action);
        }
    }

    // Вызывается когда игрок ВЫХОДИТ из этой точки (перед перемещением)
    public void TriggerExitActions()
    {
        Debug.Log($"Выход из точки: {nodeName}");

        // Вызываем UnityEvent
        OnExitNode?.Invoke();

        // Выполняем все действия
        foreach (var action in onExitActions)
        {
            ExecuteAction(action);
        }

        hasTriggeredEnter = false;
    }

    private void ExecuteAction(NodeAction action)
    {
        if (action.delay > 0)
        {
            StartCoroutine(DelayedAction(action));
        }
        else
        {
            ExecuteActionImmediate(action);
        }
    }

    private System.Collections.IEnumerator DelayedAction(NodeAction action)
    {
        yield return new WaitForSeconds(action.delay);
        ExecuteActionImmediate(action);
    }

    private void ExecuteActionImmediate(NodeAction action)
    {
        switch (action.actionType)
        {
            case ActionType.ActivateGameObject:
                if (action.targetObject != null)
                    action.targetObject.SetActive(true);
                Debug.Log($"Активирован объект: {action.targetObject?.name}");
                break;

            case ActionType.DeactivateGameObject:
                if (action.targetObject != null)
                    action.targetObject.SetActive(false);
                Debug.Log($"Деактивирован объект: {action.targetObject?.name}");
                break;

            case ActionType.CallMethod:
                if (action.targetComponent != null && !string.IsNullOrEmpty(action.methodName))
                {
                    action.targetComponent.Invoke(action.methodName, 0f);
                    Debug.Log($"Вызван метод {action.methodName} на {action.targetComponent.name}");
                }
                break;

            case ActionType.SetBoolTrue:
                if (action.targetAnimator != null && !string.IsNullOrEmpty(action.parameterName))
                {
                    action.targetAnimator.SetBool(action.parameterName, true);
                    Debug.Log($"Animator.SetBool({action.parameterName}, true)");
                }
                break;

            case ActionType.SetBoolFalse:
                if (action.targetAnimator != null && !string.IsNullOrEmpty(action.parameterName))
                {
                    action.targetAnimator.SetBool(action.parameterName, false);
                    Debug.Log($"Animator.SetBool({action.parameterName}, false)");
                }
                break;

            case ActionType.SetTrigger:
                if (action.targetAnimator != null && !string.IsNullOrEmpty(action.parameterName))
                {
                    action.targetAnimator.SetTrigger(action.parameterName);
                    Debug.Log($"Animator.SetTrigger({action.parameterName})");
                }
                break;

            case ActionType.SendMessage:
                if (action.messageTarget != null && !string.IsNullOrEmpty(action.messageName))
                {
                    action.messageTarget.SendMessage(action.messageName, SendMessageOptions.DontRequireReceiver);
                    Debug.Log($"SendMessage({action.messageName}) на {action.messageTarget.name}");
                }
                break;

            case ActionType.PlaySound:
                // Можно расширить для воспроизведения звука
                break;

            case ActionType.CustomEvent:
                Debug.Log($"Пользовательское событие: {action.actionName}");
                break;
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

    // Поиск целевой точки
    private PointNode FindTargetNode(ConnectedPath path)
    {
        UpdateNodesCache();

        if (path.targetNodeId != -1 && allNodesCache.ContainsKey(path.targetNodeId))
            return allNodesCache[path.targetNodeId];

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

    // ========== GIZMOS ==========
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        DrawNode();
        DrawConnections();
        DrawLookDirection();
    }

    private void DrawNode()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, nodeRadius);
        Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, nodeRadius - 0.05f);

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

            PointNode targetNode = FindTargetNode(path);
            if (targetNode == null) continue;

            Color lineColor = GetDirectionColor(path.direction);
            Gizmos.color = lineColor;

            Vector3 startPos = transform.position;
            Vector3 endPos = targetNode.transform.position;
            Gizmos.DrawLine(startPos, endPos);
            DrawArrow(startPos, endPos, lineColor);
            DrawKeyLabel(startPos, endPos, path.activationKey);
        }
    }

    private void DrawArrow(Vector3 from, Vector3 to, Color color)
    {
        Vector3 direction = (to - from).normalized;
        float distance = Vector3.Distance(from, to);
        Vector3 arrowPos = from + direction * (distance * 0.7f);
        float arrowLength = Mathf.Min(0.5f, distance * 0.15f);
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

    private void DrawKeyLabel(Vector3 from, Vector3 to, KeyCode key)
    {
#if UNITY_EDITOR
        Vector3 midPoint = (from + to) / 2;
        string keyName = key.ToString().Replace("Alpha", "");

        GUIStyle style = new GUIStyle();
        style.normal.textColor = GetDirectionColorFromKey(key);
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 12;
        style.alignment = TextAnchor.MiddleCenter;

        UnityEditor.Handles.Label(midPoint + Vector3.up * 0.3f, $"  [{keyName}]  ", style);
#endif
    }

    private Color GetDirectionColor(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up: return Color.green;
            case Direction.Down: return new Color(1f, 0.5f, 0f);
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
            default: return Color.white;
        }
    }

    private void DrawLookDirection()
    {
        Gizmos.color = Color.blue;
        Vector3 forward = transform.forward;
        Gizmos.DrawRay(transform.position, forward * 1f);

        Vector3 arrowEnd = transform.position + forward * 1f;
        Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
        Gizmos.DrawLine(arrowEnd, arrowEnd - forward * 0.2f + right * 0.15f);
        Gizmos.DrawLine(arrowEnd, arrowEnd - forward * 0.2f - right * 0.15f);
        Gizmos.DrawWireSphere(arrowEnd, 0.1f);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
#if UNITY_EDITOR
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, nodeRadius + 0.1f);

        foreach (var path in connectedPaths)
        {
            if (!path.isAvailable) continue;
            string info = $"{path.direction}: {path.activationKey} -> ";
            info += path.targetNodeId != -1 ? $"Node {path.targetNodeId}" : path.targetNodeName;
            UnityEditor.Handles.Label(transform.position + Vector3.up * (0.8f + (float)path.direction * 0.3f), info);
        }
#endif
    }
}