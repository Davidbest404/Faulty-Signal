using UnityEngine;
using System.Collections.Generic;

public class MovementInputHandler : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private PathMovementController movementController;

    [Header("Настройки")]
    [SerializeField] private bool enableDebugLogs = true;

    // Кэш для быстрого доступа
    private Dictionary<KeyCode, PointNode.Direction> keyToDirection = new Dictionary<KeyCode, PointNode.Direction>();
    private PointNode currentNode;

    private void Start()
    {
        if (movementController == null)
            movementController = GetComponent<PathMovementController>();
    }

    private void Update()
    {
        if (movementController == null || movementController.IsMoving()) return;

        // Обновляем текущую точку
        currentNode = movementController.GetCurrentNode();
        if (currentNode == null) return;

        // Обновляем кэш клавиш для текущей точки
        UpdateKeyCache();

        // Проверяем только те клавиши, которые есть в кэше
        foreach (var kvp in keyToDirection)
        {
            if (Input.GetKeyDown(kvp.Key))
            {
                TryMove(kvp.Value);
                break; // Обрабатываем только одно нажатие за кадр
            }
        }
    }

    private void UpdateKeyCache()
    {
        keyToDirection.Clear();

        // Получаем все доступные направления из текущей точки
        var directions = new PointNode.Direction[]
        {
            PointNode.Direction.Up,
            PointNode.Direction.Down,
            PointNode.Direction.Left,
            PointNode.Direction.Right
        };

        foreach (var dir in directions)
        {
            if (currentNode.IsDirectionAvailable(dir))
            {
                KeyCode key = currentNode.GetKeyForDirection(dir);
                if (key != KeyCode.None && !keyToDirection.ContainsKey(key))
                {
                    keyToDirection.Add(key, dir);
                }
            }
        }

        if (enableDebugLogs && keyToDirection.Count > 0)
        {
            string debugInfo = "Доступные клавиши: ";
            foreach (var kvp in keyToDirection)
            {
                debugInfo += $"{kvp.Key}→{kvp.Value} ";
            }
            Debug.Log(debugInfo);
        }
    }

    private void TryMove(PointNode.Direction direction)
    {
        if (currentNode.IsDirectionAvailable(direction))
        {
            if (enableDebugLogs)
                Debug.Log($"Движение {direction} из точки {currentNode.nodeName}");

            movementController.TryMove(direction);
        }
    }
}