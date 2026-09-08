using UnityEngine;
/// <summary>
/// Interface cho bất kỳ vật thể nào có thể tương tác.
/// Implement interface này trên các script của vật thể trong game.
/// </summary>
public interface IInteractable
{
    /// <summary>Tên hiển thị trên UI (vd: "Mở cửa", "Nhặt vật phẩm").</summary>
    string InteractPrompt { get; }
    /// <summary>Được gọi khi người chơi nhấn chuột phải vào vật thể.</summary>
    /// <param name="interactor">GameObject của người chơi đang tương tác.</param>
    void Interact(GameObject interactor);
    /// <summary>Được gọi khi người chơi bắt đầu nhìn vào vật thể (dùng để highlight).</summary>
    void OnLookAt();
    /// <summary>Được gọi khi người chơi nhìn đi chỗ khác.</summary>
    void OnLookAway();
}
