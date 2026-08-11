using System;
using UnityEngine;
using UnityEngine.UIElements;

public class AutoScroll : MonoBehaviour
{
    public float scrollSpeed;
    private float  length;
    private float startPos;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPos = transform.position.x;
        length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    // Update is called once per frame
    void Update()
    {
        //*tính toán vị trí mới cập nhật theo thời gian
        float newPos = Mathf.Repeat(Time.time * scrollSpeed,length);
        //*cập nhật lại toạ độ của đối tượng
        transform.position = new Vector3(startPos - newPos,transform.position.y,transform.position.z);
    }
}
