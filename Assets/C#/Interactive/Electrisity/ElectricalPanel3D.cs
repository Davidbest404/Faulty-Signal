using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ElectricalPanel3D : MonoBehaviour
{
    [Header("Настройки щитка")]
    [SerializeField] private List<BreakerCube> breakerCubes = new List<BreakerCube>();

    [Header("Визуальные эффекты")]
    [SerializeField] private AudioClip breakerMoveSound;
    [SerializeField] private AudioClip allFixedSound;
    [SerializeField] private ParticleSystem sparkEffect;

    private AudioSource audioSource;
    private int fixedCount = 0;
    private bool isAllFixed = false;

    public System.Action OnAllBreakersFixed;

    [System.Serializable]
    public class BreakerCube
    {
        public string cubeName;
        public GameObject cubeObject;          // 3D кубик на щитке
        public GameObject connectedObject;     // Объект, который отключается
        public Vector3 upPosition;             // Верхняя позиция (включено)
        public Vector3 downPosition;            // Нижняя позиция (выключено)
        public float moveSpeed = 2f;
        public bool isFixed = false;            // Поднят ли обратно
        public bool isDown = false;             // Опущен ли

        [Header("Звуки")]
        public AudioClip clickSound;

        // Компоненты для кликов
        [HideInInspector] public Collider cubeCollider;
        [HideInInspector] public ClickableCube clickableScript;
    }

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Сохраняем начальные позиции кубиков и настраиваем коллайдеры
        foreach (var cube in breakerCubes)
        {
            if (cube.cubeObject != null)
            {
                cube.upPosition = cube.cubeObject.transform.localPosition;
                cube.isFixed = true;
                cube.isDown = false;

                // Настраиваем коллайдер
                cube.cubeCollider = cube.cubeObject.GetComponent<Collider>();
                if (cube.cubeCollider != null)
                {
                    cube.cubeCollider.enabled = true;
                }

                // Настраиваем скрипт кликабельности
                cube.clickableScript = cube.cubeObject.GetComponent<ClickableCube>();
                if (cube.clickableScript != null)
                {
                    cube.clickableScript.SetBreakerCube(cube, this);
                    cube.clickableScript.SetClickable(false); // Изначально не кликабелен
                }
            }
        }
    }

    // Вызывается из RandomDisabler для опускания случайных кубиков
    public void TriggerRandomBreakers(List<GameObject> disabledObjects)
    {
        List<BreakerCube> availableCubes = new List<BreakerCube>();

        // Находим кубики, соответствующие отключённым объектам
        foreach (var obj in disabledObjects)
        {
            var cube = breakerCubes.Find(c => c.connectedObject == obj);
            if (cube != null && cube.isFixed && !cube.isDown)
            {
                availableCubes.Add(cube);
            }
        }

        Debug.Log($"Найдено кубиков для опускания: {availableCubes.Count}");

        // Опускаем кубики
        foreach (var cube in availableCubes)
        {
            StartCoroutine(LowerCube(cube));
        }
    }

    private IEnumerator LowerCube(BreakerCube cube)
    {
        // Случайная задержка перед опусканием
        float delay = Random.Range(0.2f, 0.8f);
        yield return new WaitForSeconds(delay);

        cube.isFixed = false;

        // Звук
        if (cube.clickSound != null)
            audioSource.PlayOneShot(cube.clickSound);
        else if (breakerMoveSound != null)
            audioSource.PlayOneShot(breakerMoveSound);

        // Искры
        if (sparkEffect != null)
        {
            ParticleSystem sparks = Instantiate(sparkEffect, cube.cubeObject.transform.position, Quaternion.identity);
            Destroy(sparks.gameObject, 0.5f);
        }

        // Анимация опускания
        yield return StartCoroutine(MoveCube(cube.cubeObject, cube.downPosition, cube.moveSpeed));

        cube.isDown = true;

        // Делаем кубик кликабельным ТОЛЬКО после полного опускания
        if (cube.clickableScript != null)
        {
            cube.clickableScript.SetClickable(true);
        }

        Debug.Log($"Кубик {cube.cubeName} опущен! Теперь кликабелен.");
    }

    // Вызывается при клике мышью по кубику
    public void OnCubeClicked(BreakerCube cube)
    {
        if (cube.isFixed)
        {
            Debug.Log($"Кубик {cube.cubeName} уже поднят");
            return;
        }

        if (!cube.isDown)
        {
            Debug.Log($"Кубик {cube.cubeName} ещё не опущен");
            return;
        }

        StartCoroutine(RaiseCube(cube));
    }

    private IEnumerator RaiseCube(BreakerCube cube)
    {
        // Звук
        if (cube.clickSound != null)
            audioSource.PlayOneShot(cube.clickSound);

        // Анимация поднятия
        yield return StartCoroutine(MoveCube(cube.cubeObject, cube.upPosition, cube.moveSpeed));

        cube.isFixed = true;
        cube.isDown = false;
        fixedCount++;

        // Делаем кубик некликабельным
        if (cube.clickableScript != null)
        {
            cube.clickableScript.SetClickable(false);
        }

        Debug.Log($"Кубик {cube.cubeName} поднят! ({fixedCount}/{breakerCubes.Count})");

        // Включаем связанный объект обратно
        if (cube.connectedObject != null)
        {
            cube.connectedObject.SetActive(true);
            Debug.Log($"Объект {cube.connectedObject.name} включён обратно!");
        }

        // Проверяем, все ли кубики подняты
        if (fixedCount >= breakerCubes.Count && !isAllFixed)
        {
            isAllFixed = true;
            yield return new WaitForSeconds(0.5f);

            if (allFixedSound != null)
                audioSource.PlayOneShot(allFixedSound);

            Debug.Log("ВСЕ КУБИКИ ПОДНЯТЫ! Питание восстановлено!");
            OnAllBreakersFixed?.Invoke();
        }
    }

    private IEnumerator MoveCube(GameObject cube, Vector3 targetPosition, float speed)
    {
        float elapsed = 0f;
        Vector3 startPosition = cube.transform.localPosition;
        float distance = Vector3.Distance(startPosition, targetPosition);
        float duration = distance / speed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            cube.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        cube.transform.localPosition = targetPosition;
    }

    public void ResetAllBreakers()
    {
        fixedCount = 0;
        isAllFixed = false;

        foreach (var cube in breakerCubes)
        {
            cube.isFixed = true;
            cube.isDown = false;
            if (cube.cubeObject != null)
            {
                cube.cubeObject.transform.localPosition = cube.upPosition;
                if (cube.clickableScript != null)
                    cube.clickableScript.SetClickable(false);
            }
        }
    }

    public int GetFixedCount() => fixedCount;
    public int GetTotalCount() => breakerCubes.Count;
    public bool IsAllFixed() => isAllFixed;
}