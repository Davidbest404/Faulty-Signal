using UnityEngine;
using System.Collections;

public class MoveDownObject : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float moveDownDistance = 1f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float resetSpeed = 3f;

    [Header("Звуки")]
    [SerializeField] private AudioClip moveDownSound;
    [SerializeField] private AudioClip moveUpSound;

    private Vector3 originalPosition;
    private Vector3 downPosition;
    private bool isDown = false;
    private bool isMoving = false;
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        originalPosition = transform.localPosition;
        downPosition = originalPosition + Vector3.down * moveDownDistance;
    }

    public void MoveDown()
    {
        if (isMoving || isDown) return;
        StartCoroutine(MoveToPosition(downPosition, moveSpeed, true));
    }

    public void MoveUp()
    {
        if (isMoving || !isDown) return;
        StartCoroutine(MoveToPosition(originalPosition, resetSpeed, false));
    }

    private IEnumerator MoveToPosition(Vector3 target, float speed, bool down)
    {
        isMoving = true;

        if (down && moveDownSound != null)
        {
            audioSource.PlayOneShot(moveDownSound);
        }
        else if (!down && moveUpSound != null)
        {
            audioSource.PlayOneShot(moveUpSound);
        }

        float elapsed = 0f;
        Vector3 start = transform.localPosition;
        float distance = Vector3.Distance(start, target);
        float duration = distance / speed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localPosition = Vector3.Lerp(start, target, t);
            yield return null;
        }

        transform.localPosition = target;
        isMoving = false;
        isDown = down;

        if (!down)
            Debug.Log($"Объект {gameObject.name} поднят!");
    }

    public bool IsDown() => isDown;
}