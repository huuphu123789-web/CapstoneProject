using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class QuestEvent : MonoBehaviour
{
    public static QuestEvent Instance { get; private set; }


    public enum QuestState
    {
        NotStarted,
        Active,
        Completed,
        Failed,
        Cancelled
    }


    [Serializable]
    public class QuestData
    {
        [Header("Quest")]
        public string questID;
        public string questName;
        [TextArea]
        public string description;

        [Header("Objective")]
        public int requiredAmount = 1;

        [HideInInspector]
        public int currentAmount = 0;

        [HideInInspector]
        public QuestState state =
            QuestState.NotStarted;

        [Header("Reward")]
        public int rewardXP = 100;
        public int rewardMoney = 100;
    }


    [Header("Quest Database")]
    [SerializeField]
    private List<QuestData> quests =
        new List<QuestData>();


    [Header("Unity Events")]

    public UnityEvent OnQuestStarted;

    public UnityEvent OnObjectiveUpdated;

    public UnityEvent OnQuestCompleted;

    public UnityEvent OnQuestFailed;

    public UnityEvent OnQuestCancelled;

    public UnityEvent OnQuestReward;


    public event Action<QuestData> QuestStarted;

    public event Action<QuestData> ObjectiveUpdated;

    public event Action<QuestData> QuestCompleted;

    public event Action<QuestData> QuestFailed;

    public event Action<QuestData> QuestCancelled;

    public event Action<QuestData> QuestReward;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }


    public void StartQuest(string questID)
    {
        QuestData quest =
            FindQuest(questID);

        if (quest == null)
            return;

        if (quest.state != QuestState.NotStarted)
        {
            Debug.LogWarning(
                "Quest đã được bắt đầu: " +
                questID
            );

            return;
        }

        quest.state =
            QuestState.Active;

        quest.currentAmount = 0;

        QuestStarted?.Invoke(quest);

        OnQuestStarted?.Invoke();

        Debug.Log(
            "Quest Started: " +
            quest.questName
        );
    }


    public void AddProgress(
        string questID,
        int amount = 1)
    {
        QuestData quest =
            FindQuest(questID);

        if (quest == null)
            return;

        if (quest.state != QuestState.Active)
            return;

        quest.currentAmount += amount;

        if (quest.currentAmount >
            quest.requiredAmount)
        {
            quest.currentAmount =
                quest.requiredAmount;
        }

        ObjectiveUpdated?.Invoke(quest);

        OnObjectiveUpdated?.Invoke();

        Debug.Log(
            "Quest Progress: " +
            quest.questName +
            " " +
            quest.currentAmount +
            "/" +
            quest.requiredAmount
        );

        if (quest.currentAmount >=
            quest.requiredAmount)
        {
            CompleteQuest(questID);
        }
    }


    public void CompleteQuest(
        string questID)
    {
        QuestData quest =
            FindQuest(questID);

        if (quest == null)
            return;

        if (quest.state != QuestState.Active)
            return;

        quest.state =
            QuestState.Completed;

        QuestCompleted?.Invoke(quest);

        OnQuestCompleted?.Invoke();

        GiveReward(quest);

        Debug.Log(
            "Quest Completed: " +
            quest.questName
        );
    }


    public void FailQuest(
        string questID)
    {
        QuestData quest =
            FindQuest(questID);

        if (quest == null)
            return;

        if (quest.state != QuestState.Active)
            return;

        quest.state =
            QuestState.Failed;

        QuestFailed?.Invoke(quest);

        OnQuestFailed?.Invoke();

        Debug.Log(
            "Quest Failed: " +
            quest.questName
        );
    }


    public void CancelQuest(
        string questID)
    {
        QuestData quest =
            FindQuest(questID);

        if (quest == null)
            return;

        if (quest.state != QuestState.Active)
            return;

        quest.state =
            QuestState.Cancelled;

        QuestCancelled?.Invoke(quest);

        OnQuestCancelled?.Invoke();

        Debug.Log(
            "Quest Cancelled: " +
            quest.questName
        );
    }


    private void GiveReward(
        QuestData quest)
    {
        Debug.Log(
            "Reward: " +
            quest.rewardXP +
            " XP, " +
            quest.rewardMoney +
            " Money"
        );

        QuestReward?.Invoke(quest);

        OnQuestReward?.Invoke();
    }


    public QuestData FindQuest(
        string questID)
    {
        foreach (QuestData quest in quests)
        {
            if (quest.questID == questID)
            {
                return quest;
            }
        }

        Debug.LogWarning(
            "Không tìm thấy Quest: " +
            questID
        );

        return null;
    }


    public int GetProgress(
        string questID)
    {
        QuestData quest =
            FindQuest(questID);

        if (quest == null)
            return 0;

        return quest.currentAmount;
    }


    public int GetRequiredAmount(
        string questID)
    {
        QuestData quest =
            FindQuest(questID);

        if (quest == null)
            return 0;

        return quest.requiredAmount;
    }


    public QuestState GetQuestState(
        string questID)
    {
        QuestData quest =
            FindQuest(questID);

        if (quest == null)
            return QuestState.NotStarted;

        return quest.state;
    }


    public void ResetQuest(
        string questID)
    {
        QuestData quest =
            FindQuest(questID);

        if (quest == null)
            return;

        quest.currentAmount = 0;

        quest.state =
            QuestState.NotStarted;
    }
}