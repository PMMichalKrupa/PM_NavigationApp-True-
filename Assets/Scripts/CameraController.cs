using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 5f;

    void Update()
    {
        float moveX = 0f, moveZ = 0f, moveY = 0f;

        // WS = forward/back (Z axis)
        if (Input.GetKey(KeyCode.W))
            moveZ += 1f;

        if (Input.GetKey(KeyCode.S))
            moveZ -= 1f;

        // AD = left/right (X axis)
        if (Input.GetKey(KeyCode.D))
            moveX += 1f;

        if (Input.GetKey(KeyCode.A))
            moveX -= 1f;

        // Space/Shift = up/down (Y axis)
        if (Input.GetKey(KeyCode.Space)     && transform.position.y < 85f)
            moveY += 1f;

        if (Input.GetKey(KeyCode.LeftShift) && transform.position.y > 15f)
            moveY -= 1f;


        Vector3 movement = new Vector3(moveX, moveY, moveZ) * moveSpeed * Time.deltaTime;
        transform.Translate(movement, Space.World);
    }
}
