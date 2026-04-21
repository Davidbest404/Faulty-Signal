using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoaderSimple : MonoBehaviour
{
    [Header("Целевая сцена")]
    [SerializeField] private string sceneName = "NextScene";

    /// <summary>
    /// Загрузить сцену
    /// </summary>
    public void LoadScene()
    {
        SceneManager.LoadScene(sceneName);
        Debug.Log($"Загрузка сцены: {sceneName}");
    }

    /// <summary>
    /// Загрузить сцену по имени
    /// </summary>
    public void LoadSceneByName(string name)
    {
        sceneName = name;
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Перезагрузить текущую сцену
    /// </summary>
    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Загрузить сцену по индексу
    /// </summary>
    public void LoadSceneByIndex(int index)
    {
        SceneManager.LoadScene(index);
    }
}