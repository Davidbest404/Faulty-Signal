using UnityEngine;

public class MonitorGameWindow : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private PathfinderGame pathfinderGame;
    [SerializeField] private GameObject gameWindow;           // Окно с игрой
    [SerializeField] private MonitorCursor monitorCursor;     // Ваш курсор

    [Header("Настройки")]
    [SerializeField] private KeyCode openGameKey = KeyCode.E; // Клавиша открытия

    private bool isWindowOpen = false;

    private void Start()
    {
        if (pathfinderGame == null)
            pathfinderGame = FindObjectOfType<PathfinderGame>();

        if (monitorCursor == null)
            monitorCursor = FindObjectOfType<MonitorCursor>();

        // Закрываем окно при старте
        if (gameWindow != null)
            gameWindow.SetActive(false);

        // Подписываемся на события игры
        if (pathfinderGame != null)
        {
            pathfinderGame.OnGameWin += OnGameWin;
            pathfinderGame.OnGameLose += OnGameLose;
        }
    }

    private void Update()
    {
        // Открытие/закрытие окна
        if (Input.GetKeyDown(openGameKey))
        {
            if (isWindowOpen)
                CloseWindow();
            else
                OpenWindow();
        }
    }

    private void OpenWindow()
    {
        isWindowOpen = true;

        if (gameWindow != null)
            gameWindow.SetActive(true);

        if (pathfinderGame != null)
            pathfinderGame.StartGame();

        // Отключаем курсор во время игры
        //if (monitorCursor != null)
        //    monitorCursor.SetCursorActive(false);

        Debug.Log("Окно игры открыто");
    }

    private void CloseWindow()
    {
        isWindowOpen = false;

        if (gameWindow != null)
            gameWindow.SetActive(false);

        if (pathfinderGame != null)
            pathfinderGame.StopGame();

        // Включаем курсор обратно
        if (monitorCursor != null)
            monitorCursor.SetCursorActive(true);

        Debug.Log("Окно игры закрыто");
    }

    private void OnGameWin()
    {
        Debug.Log("ПОБЕДА! Окно закроется через 1 секунду...");
        Invoke(nameof(CloseWindowAfterWin), 1f);
    }

    private void OnGameLose()
    {
        // При проигрыше игра уже перезапустилась
        Debug.Log("ПРОИГРЫШ! Игра перезапущена");
    }

    private void CloseWindowAfterWin()
    {
        CloseWindow();
    }

    private void OnDestroy()
    {
        if (pathfinderGame != null)
        {
            pathfinderGame.OnGameWin -= OnGameWin;
            pathfinderGame.OnGameLose -= OnGameLose;
        }
    }
}