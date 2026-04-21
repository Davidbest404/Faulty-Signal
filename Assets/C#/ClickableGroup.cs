using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class ClickableGroup : MonoBehaviour
{
    [Header("Настройки группы")]
    [SerializeField] private string groupName = "Группа";
    [SerializeField] private List<ClickableItem> items = new List<ClickableItem>();

    [Header("Общие действия")]
    [SerializeField] private GameObject[] globalObjectsToActivate;
    [SerializeField] private GameObject[] globalObjectsToDeactivate;

    [Header("События")]
    [SerializeField] private UnityEvent OnAllItemsClicked;
    [SerializeField] private UnityEvent OnAnyItemClicked;

    private int clickedCount = 0;
    private bool isComplete = false;

    [System.Serializable]
    public class ClickableItem
    {
        public string itemName;
        public GameObject targetObject;
        public GameObject[] objectsToActivate;
        public GameObject[] objectsToDeactivate;
        public AudioClip clickSound;
        public bool isClicked = false;
    }

    private void Start()
    {
        // Можно добавить инициализацию
    }

    public void OnItemClicked(ClickableItem item)
    {
        if (item.isClicked) return;

        item.isClicked = true;
        clickedCount++;

        // Активируем/деактивируем объекты для этого предмета
        foreach (var obj in item.objectsToActivate)
        {
            if (obj != null) obj.SetActive(true);
        }

        foreach (var obj in item.objectsToDeactivate)
        {
            if (obj != null) obj.SetActive(false);
        }

        Debug.Log($"Предмет {item.itemName} активирован! ({clickedCount}/{items.Count})");

        OnAnyItemClicked?.Invoke();

        // Проверяем, все ли предметы нажаты
        if (clickedCount >= items.Count && !isComplete)
        {
            isComplete = true;
            OnAllItemsClicked?.Invoke();

            // Глобальные действия
            foreach (var obj in globalObjectsToActivate)
            {
                if (obj != null) obj.SetActive(true);
            }

            foreach (var obj in globalObjectsToDeactivate)
            {
                if (obj != null) obj.SetActive(false);
            }

            Debug.Log($"Группа {groupName} полностью активирована!");
        }
    }
}