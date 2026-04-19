using UnityEngine;
using System.Collections.Generic;

public class SimpleShortcutManager : MonoBehaviour
{
    [System.Serializable]
    public class ShortcutData
    {
        public string name;
        public int id;                          // ID ярлыка
        public RectTransform shortcutRect;      // Область ярлыка
        public GameObject[] objectsToToggle;    // Объекты для переключения
        public bool activateInsteadOfToggle = true; // Активировать (true) или переключать (false)
        public bool gameObjectMuteShortcut = false; // false = звук есть, true = без звука
        public AudioClip shortcutClip;          // Звук при нажатии на ярлик
    }

    [SerializeField] private MonitorCursor cursor;
    [SerializeField] private List<ShortcutData> shortcuts = new List<ShortcutData>();
    [SerializeField] private AudioClip clickSound;

    private void Start()
    {
        if (cursor == null)
            cursor = FindObjectOfType<MonitorCursor>();

        if (cursor != null)
            cursor.OnClickAtPosition += OnCursorClick;
    }

    private void OnCursorClick(Vector2 clickPos)
    {
        // Воспроизводим звук клика
        if (clickSound != null)
        {
            AudioSource.PlayClipAtPoint(clickSound, Camera.main.transform.position);
        }
        foreach (var shortcut in shortcuts)
        {
            if (IsPointInRect(clickPos, shortcut.shortcutRect))
            {
                HandleShortcut(shortcut);
                break;
            }
        }
    }

    private bool IsPointInRect(Vector2 point, RectTransform rect)
    {
        if (rect == null) return false;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rect,
            point,
            rect.GetComponentInParent<Canvas>().worldCamera,
            out localPoint
        );

        return rect.rect.Contains(localPoint);
    }

    private void HandleShortcut(ShortcutData shortcut)
    {
        ShortcutData shortcutData = shortcut as ShortcutData;

        // Проверяем: если НЕ замучен (false) ИЛИ окно НЕ активно
        if (shortcutData.id != -1)
        {
            if (shortcutData.gameObjectMuteShortcut == false)
            {
                // Воспроизводим звук
                if (shortcutData.shortcutClip != null)
                {
                    AudioSource.PlayClipAtPoint(shortcutData.shortcutClip, Camera.main.transform.position);
                }
            }
            else if (shortcutData.gameObjectMuteShortcut == true && shortcutData.objectsToToggle[shortcutData.id].activeSelf == false) 
            {
                // Воспроизводим звук
                if (shortcutData.shortcutClip != null)
                {
                    AudioSource.PlayClipAtPoint(shortcutData.shortcutClip, Camera.main.transform.position);
                }
            }
        }
        Debug.Log($"Нажат ярлык: {shortcut.name} (ID: {shortcut.id})");

        if (shortcutData.id != -1)
        {
            foreach (var obj in shortcut.objectsToToggle)
            {
                if (obj == null) continue;

                if (shortcut.activateInsteadOfToggle)
                    obj.SetActive(true);
                else
                    obj.SetActive(!obj.activeSelf);
            }

            // Можно добавить другие действия по ID
            switch (shortcut.id)
            {
                case 0:
                    // Открыть меню
                    break;
                case 1:
                    // Запустить программу
                    break;
                default:
                    break;
            }
        }
    }
}