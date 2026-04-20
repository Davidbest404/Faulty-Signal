using UnityEngine;
using UnityEngine.UI;

public class LoadingTrigger : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private LoadingWindow loadingWindow;
    [SerializeField] private Button loadButton;

    [Header("Действие после загрузки")]
    [SerializeField] private GameObject[] objectsToActivate;
    [SerializeField] private GameObject[] objectsToDeactivate;

    private void Start()
    {
        if (loadButton != null)
            loadButton.onClick.AddListener(StartLoading);
    }

    public void StartLoading()
    {
        if (loadingWindow != null)
        {
            loadingWindow.StartLoading();
            loadingWindow.onLoadingComplete.AddListener(OnLoadingComplete);
        }
    }

    private void OnLoadingComplete()
    {
        Debug.Log("Загрузка завершена! Активируем объекты...");

        foreach (var obj in objectsToActivate)
        {
            if (obj != null) obj.SetActive(true);
        }

        foreach (var obj in objectsToDeactivate)
        {
            if (obj != null) obj.SetActive(false);
        }
    }
}