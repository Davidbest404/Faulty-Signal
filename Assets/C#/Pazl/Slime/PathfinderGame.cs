using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class PathfinderGame : MonoBehaviour
{
    [Header("Настройки сетки")]
    [SerializeField] private Transform gridContainer;
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private int gridWidth = 15;
    [SerializeField] private int gridHeight = 15;
    [SerializeField] private float cellSize = 40f;

    [Header("Пресеты (карты)")]
    [SerializeField] private List<LevelPreset> levelPresets = new List<LevelPreset>();
    [SerializeField] private int selectedPresetIndex = 0;
    [SerializeField] private bool randomizeLevelEachGame = true;

    [Header("Цвета клеток")]
    [SerializeField] private Color emptyColor = new Color(0.15f, 0.15f, 0.15f);
    [SerializeField] private Color wallColor = new Color(0.3f, 0.3f, 0.3f);
    [SerializeField] private Color blockedColor = new Color(0.5f, 0.2f, 0.2f);
    [SerializeField] private Color snakeColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color appleColor = new Color(0.9f, 0.2f, 0.2f);

    [Header("Звуки")]
    [SerializeField] private AudioClip moveSound;
    [SerializeField] private AudioClip appleSound;
    [SerializeField] private AudioClip failSound;
    [SerializeField] private AudioClip keyboardSound;

    [Header("События")]
    [SerializeField] public UnityEngine.Events.UnityEvent onLoadingComplete;

    private Cell[,] grid;
    private Vector2Int currentPosition;
    private Vector2Int currentStartPosition;
    private Vector2Int currentApplePosition;
    private HashSet<Vector2Int> blockedCells = new HashSet<Vector2Int>();
    private bool isGameActive = false;
    private bool isWin = false;
    private AudioSource audioSource;
    private List<Vector2Int> currentWalls = new List<Vector2Int>();
    private LevelPreset currentLevel;

    public System.Action OnGameWin;
    public System.Action OnGameLose;

    [System.Serializable]
    public class LevelPreset
    {
        [Header("Основная информация")]
        public string levelName = "Новый уровень";

        [Header("Позиции")]
        public Vector2Int startPosition = new Vector2Int(0, 0);
        public Vector2Int applePosition = new Vector2Int(14, 14);

        [Header("Стены")]
        public List<Vector2Int> walls = new List<Vector2Int>();
        public bool useRandomWalls = false;
        public int randomWallCount = 30;
        public int randomSeed = -1;

        [Header("Настройки уровня")]
        public Color levelColor = Color.white;  // Цвет для отличия уровней (опционально)
    }

    [System.Serializable]
    public class Cell
    {
        public Image image;
        public Vector2Int position;
        public CellType type;
    }

    public enum CellType
    {
        Empty,
        Wall,
        Blocked,
        Snake,
        Apple
    }

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        CreateGrid();
    }

    private void CreateGrid()
    {
        grid = new Cell[gridWidth, gridHeight];

        RectTransform containerRect = gridContainer.GetComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(gridWidth * cellSize, gridHeight * cellSize);

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GameObject cellObj = Instantiate(cellPrefab, gridContainer);
                RectTransform rect = cellObj.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(x * cellSize, -y * cellSize);
                rect.sizeDelta = new Vector2(cellSize - 2, cellSize - 2);

                Cell cell = new Cell();
                cell.image = cellObj.GetComponent<Image>();
                cell.position = new Vector2Int(x, y);
                cell.type = CellType.Empty;
                cell.image.color = emptyColor;

                grid[x, y] = cell;
            }
        }
    }

    private void LoadLevel()
    {
        // Выбираем уровень
        int levelIndex = selectedPresetIndex;
        if (randomizeLevelEachGame && levelPresets.Count > 0)
        {
            levelIndex = Random.Range(0, levelPresets.Count);
        }

        if (levelPresets.Count == 0 || levelIndex >= levelPresets.Count)
        {
            Debug.LogWarning("Нет пресетов уровней!");
            return;
        }

        currentLevel = levelPresets[levelIndex];
        Debug.Log($"Загружен уровень: {currentLevel.levelName}");

        // Устанавливаем позиции старта и яблока
        currentStartPosition = currentLevel.startPosition;
        currentApplePosition = currentLevel.applePosition;

        // Загружаем или генерируем стены
        LoadWalls();

        // Применяем уровень на сетку
        ApplyLevelToGrid();
    }

    private void LoadWalls()
    {
        currentWalls.Clear();

        if (currentLevel.useRandomWalls)
        {
            GenerateRandomWalls();
        }
        else
        {
            currentWalls = new List<Vector2Int>(currentLevel.walls);
        }
    }

    private void GenerateRandomWalls()
    {
        int seed = currentLevel.randomSeed;
        if (seed == -1)
            seed = Random.Range(0, 99999);

        System.Random random = new System.Random(seed);
        Debug.Log($"Генерация случайных стен для уровня '{currentLevel.levelName}' с сидом: {seed}");

        // Создаём список всех возможных позиций (исключая старт и яблоко)
        List<Vector2Int> availablePositions = new List<Vector2Int>();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (pos != currentStartPosition && pos != currentApplePosition)
                {
                    availablePositions.Add(pos);
                }
            }
        }

        // Перемешиваем список
        for (int i = 0; i < availablePositions.Count; i++)
        {
            int randomIndex = random.Next(i, availablePositions.Count);
            Vector2Int temp = availablePositions[i];
            availablePositions[i] = availablePositions[randomIndex];
            availablePositions[randomIndex] = temp;
        }

        // Берём первые N позиций как стены
        int wallCount = Mathf.Min(currentLevel.randomWallCount, availablePositions.Count);
        for (int i = 0; i < wallCount; i++)
        {
            currentWalls.Add(availablePositions[i]);
        }

        Debug.Log($"Сгенерировано {currentWalls.Count} случайных стен");
    }

    private void ApplyLevelToGrid()
    {
        // Очищаем все клетки
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                grid[x, y].type = CellType.Empty;
                grid[x, y].image.color = emptyColor;
            }
        }

        // Устанавливаем стены
        foreach (var wall in currentWalls)
        {
            if (IsValidPosition(wall) && wall != currentStartPosition && wall != currentApplePosition)
            {
                grid[wall.x, wall.y].type = CellType.Wall;
                grid[wall.x, wall.y].image.color = wallColor;
            }
        }

        // Убеждаемся, что старт и яблоко не заняты стенами
        if (grid[currentStartPosition.x, currentStartPosition.y].type == CellType.Wall)
        {
            grid[currentStartPosition.x, currentStartPosition.y].type = CellType.Empty;
            grid[currentStartPosition.x, currentStartPosition.y].image.color = emptyColor;
        }

        if (grid[currentApplePosition.x, currentApplePosition.y].type == CellType.Wall)
        {
            grid[currentApplePosition.x, currentApplePosition.y].type = CellType.Empty;
            grid[currentApplePosition.x, currentApplePosition.y].image.color = emptyColor;
        }

        // Устанавливаем яблоко
        grid[currentApplePosition.x, currentApplePosition.y].type = CellType.Apple;
        grid[currentApplePosition.x, currentApplePosition.y].image.color = appleColor;
    }

    private void Update()
    {
        if (!isGameActive || isWin) return;

        Vector2Int direction = Vector2Int.zero;

        // ИСПРАВЛЕННЫЕ НАПРАВЛЕНИЯ ДЛЯ UI CANVAS
        if (Input.GetKeyDown(KeyCode.UpArrow))
            direction = new Vector2Int(-1, 0);    // Вверх
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            direction = new Vector2Int(1, 0);   // Вниз
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            direction = new Vector2Int(0, -1);   // Влево
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            direction = new Vector2Int(0, 1);    // Вправо

        if (direction != Vector2Int.zero)
        {
            PlaySound(keyboardSound);
            TryMove(direction);
        }
    }

    private void TryMove(Vector2Int direction)
    {
        Vector2Int newPos = currentPosition + direction;

        if (!IsValidPosition(newPos))
        {
            PlaySound(failSound);
            RestartGame();
            return;
        }

        Cell targetCell = grid[newPos.x, newPos.y];

        if (targetCell.type == CellType.Wall)
        {
            PlaySound(failSound);
            RestartGame();
            return;
        }

        if (targetCell.type == CellType.Blocked)
        {
            PlaySound(failSound);
            RestartGame();
            return;
        }

        if (targetCell.type == CellType.Apple)
        {
            PlaySound(appleSound);
            MarkCurrentPositionAsBlocked();

            currentPosition = newPos;
            grid[currentPosition.x, currentPosition.y].type = CellType.Snake;
            grid[currentPosition.x, currentPosition.y].image.color = snakeColor;

            isWin = true;
            isGameActive = false;

            // Вызываем событие
            onLoadingComplete?.Invoke();

            Debug.Log($"ПОБЕДА на уровне '{currentLevel.levelName}'!");
            OnGameWin?.Invoke();
            return;
        }

        PlaySound(moveSound);
        MarkCurrentPositionAsBlocked();

        currentPosition = newPos;
        grid[currentPosition.x, currentPosition.y].type = CellType.Snake;
        grid[currentPosition.x, currentPosition.y].image.color = snakeColor;
    }

    private void MarkCurrentPositionAsBlocked()
    {
        grid[currentPosition.x, currentPosition.y].type = CellType.Blocked;
        grid[currentPosition.x, currentPosition.y].image.color = blockedColor;
        blockedCells.Add(currentPosition);
    }

    private bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // ========== ПУБЛИЧНЫЕ МЕТОДЫ ==========

    public void StartGame()
    {
        RestartGame();
    }

    public void RestartGame()
    {
        if (levelPresets.Count == 0)
        {
            Debug.LogError("Нет загруженных уровней!");
            return;
        }

        // Загружаем уровень
        LoadLevel();

        // Очищаем список заблокированных клеток
        blockedCells.Clear();

        // Устанавливаем стартовую позицию
        currentPosition = currentStartPosition;

        // Убеждаемся, что яблоко на месте
        if (grid[currentApplePosition.x, currentApplePosition.y].type != CellType.Apple)
        {
            grid[currentApplePosition.x, currentApplePosition.y].type = CellType.Apple;
            grid[currentApplePosition.x, currentApplePosition.y].image.color = appleColor;
        }

        // Устанавливаем змейку на старт
        grid[currentPosition.x, currentPosition.y].type = CellType.Snake;
        grid[currentPosition.x, currentPosition.y].image.color = snakeColor;

        isGameActive = true;
        isWin = false;

        Debug.Log($"Уровень '{currentLevel.levelName}' начат! Старт: {currentStartPosition}, Яблоко: {currentApplePosition}");
    }

    public void StopGame()
    {
        isGameActive = false;
    }

    public bool IsGameActive()
    {
        return isGameActive;
    }

    public bool IsWin()
    {
        return isWin;
    }

    // Выбор уровня по индексу
    public void SelectLevel(int index)
    {
        if (index >= 0 && index < levelPresets.Count)
        {
            selectedPresetIndex = index;
            if (!isGameActive)
            {
                RestartGame();
            }
        }
    }

    // Выбор уровня по имени
    public void SelectLevel(string name)
    {
        int index = levelPresets.FindIndex(l => l.levelName == name);
        if (index != -1)
        {
            selectedPresetIndex = index;
            if (!isGameActive)
            {
                RestartGame();
            }
        }
    }

    // Получить список названий уровней
    public List<string> GetLevelNames()
    {
        return levelPresets.Select(l => l.levelName).ToList();
    }

    // Получить информацию о текущем уровне
    public string GetCurrentLevelName()
    {
        return currentLevel != null ? currentLevel.levelName : "None";
    }

    public Vector2Int GetCurrentStartPosition()
    {
        return currentStartPosition;
    }

    public Vector2Int GetCurrentApplePosition()
    {
        return currentApplePosition;
    }
}