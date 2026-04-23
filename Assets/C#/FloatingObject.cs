using UnityEngine;

public class FloatingObject : MonoBehaviour
{
    [Header("Vertical Bobbing")]
    [SerializeField] private float floatAmplitude = 0.5f;    // Высота подъёма/опускания
    [SerializeField] private float floatSpeed = 1.5f;        // Скорость вертикального движения

    [Header("Horizontal Sway")]
    [SerializeField] private float swayAmplitude = 0.3f;     // Амплитуда покачивания в стороны
    [SerializeField] private float swaySpeed = 1.2f;         // Скорость покачивания

    [Header("Rotation")]
    [SerializeField] private float rotateAmplitude = 15f;     // Амплитуда вращения (в градусах)
    [SerializeField] private float rotateSpeed = 1f;          // Скорость вращения

    [Header("Smoothing")]
    [SerializeField] private float smoothness = 5f;           // Плавность движения

    [Header("Randomization")]
    [SerializeField] private bool useRandomOffsets = true;    // Использовать случайные смещения

    // Смещения для каждого объекта (чтобы они двигались по-разному)
    private float positionOffsetX;
    private float positionOffsetZ;
    private float rotationOffsetX;
    private float rotationOffsetZ;
    private float verticalOffset;

    // Исходные позиция и поворот
    private Vector3 startPosition;
    private Quaternion startRotation;

    private void Start()
    {
        // Сохраняем начальные значения
        startPosition = transform.position;
        startRotation = transform.rotation;

        // Генерируем случайные смещения
        if (useRandomOffsets)
        {
            positionOffsetX = Random.Range(0f, Mathf.PI * 2);
            positionOffsetZ = Random.Range(0f, Mathf.PI * 2);
            rotationOffsetX = Random.Range(0f, Mathf.PI * 2);
            rotationOffsetZ = Random.Range(0f, Mathf.PI * 2);
            verticalOffset = Random.Range(0f, Mathf.PI * 2);
        }
        else
        {
            positionOffsetX = 0;
            positionOffsetZ = 0;
            rotationOffsetX = 0;
            rotationOffsetZ = 0;
            verticalOffset = 0;
        }
    }

    private void Update()
    {
        // Вертикальное движение (вверх-вниз)
        float verticalMove = Mathf.Sin(Time.time * floatSpeed + verticalOffset) * floatAmplitude;

        // Горизонтальное покачивание (вперёд-назад и влево-вправо)
        float horizontalMoveX = Mathf.Sin(Time.time * swaySpeed + positionOffsetX) * swayAmplitude;
        float horizontalMoveZ = Mathf.Cos(Time.time * swaySpeed + positionOffsetZ) * swayAmplitude;

        // Новое положение
        Vector3 newPosition = startPosition + new Vector3(horizontalMoveX, verticalMove, horizontalMoveZ);

        // Плавное перемещение
        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * smoothness);

        // Вращение (наклон в разные стороны)
        float rotateX = Mathf.Sin(Time.time * rotateSpeed + rotationOffsetX) * rotateAmplitude;
        float rotateZ = Mathf.Cos(Time.time * rotateSpeed + rotationOffsetZ) * rotateAmplitude;
        float rotateY = Mathf.Sin(Time.time * rotateSpeed * 0.7f + rotationOffsetX) * (rotateAmplitude * 0.5f);

        // Новое вращение
        Quaternion newRotation = startRotation * Quaternion.Euler(rotateX, rotateY, rotateZ);

        // Плавный поворот
        transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, Time.deltaTime * smoothness);
    }
}