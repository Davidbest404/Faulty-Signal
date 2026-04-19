using UnityEngine;

public class SimplePathTrigger : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private PathFollower pathFollower;

    [Header("Какой путь запустить")]
    [SerializeField] private int pathId = 0;
    [SerializeField] private string pathName = "";

    [Header("Когда запускать")]
    [SerializeField] private bool onStart = false;
    [SerializeField] private bool onTriggerEnter = true;
    [SerializeField] private bool onCollisionEnter = false;

    private void Start()
    {
        if (pathFollower == null)
            pathFollower = FindObjectOfType<PathFollower>();

        if (onStart)
            StartPath();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (onTriggerEnter && other.CompareTag("Player"))
            StartPath();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (onCollisionEnter && collision.gameObject.CompareTag("Player"))
            StartPath();
    }

    private void StartPath()
    {
        if (pathFollower == null) return;

        if (!string.IsNullOrEmpty(pathName))
            pathFollower.StartPath(pathName);
        else
            pathFollower.StartPathById(pathId);

        Debug.Log($"Запущен путь: {(string.IsNullOrEmpty(pathName) ? $"ID {pathId}" : pathName)}");
    }
}