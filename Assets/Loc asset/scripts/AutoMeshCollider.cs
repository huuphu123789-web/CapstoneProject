using UnityEngine;

/// <summary>
/// Tự động tạo MeshCollider cho tất cả mesh con của object.
/// Gắn vào Guard Booth → Click chuột phải trên script → "Tạo Collider Tự Động".
/// </summary>
public class AutoMeshCollider : MonoBehaviour
{
    [ContextMenu("Tạo Collider Tự Động")]
    public void GenerateColliders()
    {
        // Xóa Box Collider cũ trên object gốc (cái 1x1x1)
        BoxCollider oldBox = GetComponent<BoxCollider>();
        if (oldBox != null)
        {
            DestroyImmediate(oldBox);
            Debug.Log("[AutoMeshCollider] Đã xóa Box Collider cũ trên " + gameObject.name);
        }

        // Tìm tất cả MeshFilter trong con
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        int count = 0;

        foreach (MeshFilter mf in meshFilters)
        {
            // Bỏ qua nếu đã có Collider
            if (mf.GetComponent<Collider>() != null)
                continue;

            // Thêm MeshCollider
            MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            count++;
        }

        Debug.Log($"[AutoMeshCollider] Đã tạo {count} MeshCollider cho \"{gameObject.name}\"!");
    }
}
