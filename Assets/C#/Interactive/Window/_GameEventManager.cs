using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class _GameEventManager : MonoBehaviour
{
    public static GameEventManager Instance { get; private set; }

    [Header("Настройки")]
    [SerializeField] private MailApp mailApp;
    
    [Header("Враги")]
    [SerializeField] private WindowMan windowMan;

    public int currentPhase = 0;
    private int emailsReadCount = 0;

    private float timer = 0f;
    private float tickInterval = 10f;

    [Header("Безопасная зона (Дверь)")]
    public float safeZoneTimer = 0f;

    private List<EmailData> processedEmails = new List<EmailData>();

    private void Start()
    {
        if (mailApp != null)
        {
            mailApp.OnEmailOpened += HandleEmailOpened;
        }
        else
        {
            mailApp = FindFirstObjectByType<MailApp>();
            if (mailApp != null) mailApp.OnEmailOpened += HandleEmailOpened;
        }
    }

    private void Update()
    {
        if (safeZoneTimer > 0)
        {
            safeZoneTimer -= Time.deltaTime;
        }

        timer += Time.deltaTime;
        if (timer >= tickInterval)
        {
            timer = 0f;
            ExecutePhaseTick();
        }
    }

    private void HandleEmailOpened(EmailData email)
    {
        if (!processedEmails.Contains(email))
        {
            processedEmails.Add(email);
            emailsReadCount++;
            StartCoroutine(WaitAndTriggerPhaseEvent(15f, emailsReadCount));
        }
    }

    private IEnumerator WaitAndTriggerPhaseEvent(float waitTime, int signalNumber)
    {
        yield return new WaitForSeconds(waitTime);

        Debug.Log($"[GameEventManager] Прошло 15 сек после сигнала #{signalNumber}. Обновляем фазу.");
        
        if (signalNumber == 1)
        {
            currentPhase = 1;
            TriggerWindowManAttack();
        }
        else if (signalNumber == 2)
        {
            currentPhase = 2;
            TriggerWindowManAttack();
        }
        else if (signalNumber == 3)
        {
            currentPhase = 3;
        }
        else if (signalNumber >= 4)
        {
            currentPhase = 4;
            TriggerPhase4Event();
        }
    }

    private void ExecutePhaseTick()
    {
        float roll = Random.Range(0f, 100f);
        float aggressionBoost = (currentPhase >= 3) ? 17f : 0f;

        if (currentPhase == 0)
        {
            //Минус свет
        }
        else if (currentPhase == 1 || currentPhase == 2 || currentPhase == 3)
        {
            // Шанс для монстра
            float monsterRoll = Random.Range(0f, 100f);
            if (monsterRoll < 10f + aggressionBoost)

            // Шанс для Window Man
            // ЗАЩИТА: Если он уже здесь, не бросаем кубик для него
            if (windowMan != null && windowMan.IsAttacking)
            {
                Debug.Log("[WindowMan Check] Пропуск тика: челик уже активен.");
            }
            else
            {
                float manRoll = Random.Range(0f, 100f);
                float manChance = 15f + aggressionBoost;
                
                Debug.Log($"[WindowMan Check] Фаза {currentPhase}. Шанс: {manChance}%. Ролл: {manRoll:F1}%");

                if (manRoll < manChance) 
                {
                    Debug.Log("[WindowMan Success] Ролл прошел! Запускаю появление.");
                    TriggerWindowManAttack();
                }
                else 
                {
                    Debug.Log("[WindowMan Fail] Ролл не прошел. Челик мог появиться, но не в этот раз.");
                }
            }
        }
    }

    private void TriggerWindowManAttack()
    {
        if (windowMan != null)
        {
            windowMan.StartAttack();
        }
    }

    private void TriggerPhase4Event()
    {
        TriggerWindowManAttack();
    }

    public void ActivateSafeZone()
    {
        safeZoneTimer = 30f;
        Debug.Log("Активирована безопасная зона (30 сек).");
    }

    private void OnDestroy()
    {
        if (mailApp != null)
        {
            mailApp.OnEmailOpened -= HandleEmailOpened;
        }
    }
}
