using UnityEngine;

public class BackGhostAppear : MonoBehaviour
{
    public Transform player;          // nhân vật
    public Camera playerCamera;       // camera người chơi
    public GameObject ghost;          // prefab ma

    public float spawnDistance = 2f;  // cách sau lưng bao xa
    public float viewAngle = 60f;     // góc nhìn camera

    private bool spawned = false;

    void Update()
    {
        if (spawned) return;

        // hướng từ camera tới vị trí trigger
        Vector3 dir = (transform.position - playerCamera.transform.position).normalized;

        // nếu camera KHÔNG nhìn vào trigger
        float angle = Vector3.Angle(playerCamera.transform.forward, dir);

        if (angle > viewAngle)
        {
            SpawnBehindPlayer();
        }
    }

    void SpawnBehindPlayer()
    {
        spawned = true;

        // vị trí phía sau lưng người chơi
        Vector3 pos = player.position - player.forward * spawnDistance;
        pos.y = player.position.y;

        ghost.transform.position = pos;

        // ma nhìn vào người chơi
        ghost.transform.LookAt(player);

        ghost.SetActive(true);
    }
}