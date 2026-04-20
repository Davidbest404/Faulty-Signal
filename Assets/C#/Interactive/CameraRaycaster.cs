using UnityEngine;

public class CameraRaycaster : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private LayerMask clickableLayers;
    [SerializeField] private float rayDistance = 100f;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, rayDistance, clickableLayers))
            {
                var clickable = hit.collider.GetComponent<ClickableObject>();
                if (clickable != null)
                {
                    // Клик уже обработан в ClickableObject.OnMouseDown
                    Debug.Log($"Raycast клик по: {hit.collider.name}");
                }
            }
        }
    }
}