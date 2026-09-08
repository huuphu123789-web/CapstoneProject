using UnityEngine;

public class InspectionZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem GameObject đi vào có chứa script NPCWeirdBehaviors không
        NPCWeirdBehaviors npc = other.GetComponent<NPCWeirdBehaviors>();
        if (npc != null)
        {
            // Ra lệnh cho NPC kích hoạt hành động kì dị!
            npc.StartInspection();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        NPCWeirdBehaviors npc = other.GetComponent<NPCWeirdBehaviors>();
        if (npc != null)
        {
            // Tắt hành động kì dị khi NPC đi ra khỏi vạch
            npc.StopInspection();
        }
    }
}