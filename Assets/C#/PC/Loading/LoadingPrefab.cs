using UnityEngine;

public class LoadingPrefab : MonoBehaviour
{
    [Header("Анимация")]
    [SerializeField] private bool rotate = true;
    [SerializeField] private float rotateSpeed = 90f;
    [SerializeField] private bool bounce = true;
    [SerializeField] private float bounceHeight = 10f;
    [SerializeField] private float bounceSpeed = 2f;

    private Vector3 startPosition;
    private RectTransform rectTransform;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
            startPosition = rectTransform.anchoredPosition3D;
    }

    private void Update()
    {
        if (rectTransform == null) return;

        if (rotate)
        {
            rectTransform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
        }

        if (bounce)
        {
            float offsetY = Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
            rectTransform.anchoredPosition3D = startPosition + new Vector3(0, offsetY, 0);
        }
    }
}