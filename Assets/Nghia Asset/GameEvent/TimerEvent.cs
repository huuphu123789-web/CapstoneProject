using System;
using UnityEngine;
using UnityEngine.Events;

public class TimerEvent : MonoBehaviour
{
    [Header("Timer")]
    [SerializeField] private float duration = 60f;
    [SerializeField] private bool autoStart = false;

    [Header("Warning")]
    [SerializeField] private float warningTime = 10f;

    [Header("Unity Events")]
    public UnityEvent OnTimerStart;
    public UnityEvent OnTimerWarning;
    public UnityEvent OnTimerComplete;
    public UnityEvent OnTimerPause;
    public UnityEvent OnTimerResume;
    public UnityEvent OnTimerReset;

    public event Action<float> OnTimerTick;

    private float currentTime;
    private bool isRunning;
    private bool warningTriggered;

    public float CurrentTime => currentTime;
    public float Duration => duration;
    public bool IsRunning => isRunning;

    private void Start()
    {
        ResetTimer();

        if (autoStart)
        {
            StartTimer();
        }
    }

    private void Update()
    {
        if (!isRunning)
            return;

        currentTime -= Time.deltaTime;

        OnTimerTick?.Invoke(currentTime);

        if (!warningTriggered && currentTime <= warningTime)
        {
            warningTriggered = true;
            OnTimerWarning?.Invoke();

            Debug.Log("TIMER WARNING!");
        }

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            isRunning = false;

            OnTimerTick?.Invoke(0f);
            OnTimerComplete?.Invoke();

            Debug.Log("TIMER COMPLETE!");
        }
    }


    public void StartTimer()
    {
        if (isRunning)
            return;

        isRunning = true;

        OnTimerStart?.Invoke();

        Debug.Log("Timer Start");
    }


    public void PauseTimer()
    {
        if (!isRunning)
            return;

        isRunning = false;

        OnTimerPause?.Invoke();

        Debug.Log("Timer Pause");
    }


    public void ResumeTimer()
    {
        if (isRunning)
            return;

        if (currentTime <= 0f)
            return;

        isRunning = true;

        OnTimerResume?.Invoke();

        Debug.Log("Timer Resume");
    }


    public void ResetTimer()
    {
        currentTime = duration;
        isRunning = false;
        warningTriggered = false;

        OnTimerReset?.Invoke();

        Debug.Log("Timer Reset");
    }


    public void AddTime(float amount)
    {
        currentTime += amount;

        if (currentTime > duration)
            currentTime = duration;
    }


    public void RemoveTime(float amount)
    {
        currentTime -= amount;

        if (currentTime < 0f)
            currentTime = 0f;
    }


    public void SetTime(float time)
    {
        currentTime = Mathf.Clamp(time, 0f, duration);
    }
}