using UnityEngine;

public class ClickableCube : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material normalMaterial;
    [SerializeField] private AudioClip hoverSound;

    private ElectricalPanel3D electricalPanel;
    private ElectricalPanel3D.BreakerCube cubeData;
    private Renderer cubeRenderer;
    private AudioSource audioSource;
    private bool isClickable = false;

    private void Start()
    {
        cubeRenderer = GetComponent<Renderer>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (electricalPanel == null)
            electricalPanel = FindObjectOfType<ElectricalPanel3D>();

        // Изначально не кликабелен
        SetClickable(false);
    }

    public void SetBreakerCube(ElectricalPanel3D.BreakerCube cube, ElectricalPanel3D panel)
    {
        cubeData = cube;
        electricalPanel = panel;
    }

    public void SetClickable(bool clickable)
    {
        isClickable = clickable;
        Debug.Log($"Кубик {gameObject.name} кликабелен: {clickable}");
    }

    private void OnMouseDown()
    {
        if (!isClickable)
        {
            Debug.Log($"Кубик {gameObject.name} ещё нельзя нажать (не опущен)");
            return;
        }

        if (electricalPanel != null && cubeData != null)
        {
            Debug.Log($"Клик по кубику {cubeData.cubeName}");
            electricalPanel.OnCubeClicked(cubeData);
        }
    }

    private void OnMouseEnter()
    {
        if (!isClickable) return;

        if (cubeRenderer != null && highlightMaterial != null)
        {
            cubeRenderer.material = highlightMaterial;
        }

        if (hoverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hoverSound, 0.3f);
        }
    }

    private void OnMouseExit()
    {
        if (!isClickable) return;

        if (cubeRenderer != null && normalMaterial != null)
        {
            cubeRenderer.material = normalMaterial;
        }
    }
}