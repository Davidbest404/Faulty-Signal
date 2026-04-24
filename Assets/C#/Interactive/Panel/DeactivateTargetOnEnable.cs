using UnityEngine;

public class DeactivateTargetOnEnable : MonoBehaviour
{
    [Tooltip("Объект, который будет деактивирован, когда этот объект станет активным")]
    [SerializeField] private GameObject targetObject;
    [SerializeField] private bool turnOn = false;

    private void OnEnable()
    {
        // Когда этот объект становится активным - деактивируем целевой объект
        if (targetObject != null)
        {
            if (!turnOn)
            {
                targetObject.SetActive(false);
            }
            else
            {
                targetObject.SetActive(true);
            }
        }
        else
        {
            Debug.LogWarning("Целевой объект не назначен в инспекторе!", this);
        }
    }
}