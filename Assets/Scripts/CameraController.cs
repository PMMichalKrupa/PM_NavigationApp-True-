using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float WBorder, SBorder, ABorder, DBorder, MaxHeight = 85f, MinHeight = 15f, moveSpeed = 5f;
    [SerializeField] private GameObject Camera;

    public Joystick joystick;
    float lastPinchDistance = 0f;

    void Update()
    {
        float moveX = joystick.Horizontal;
        float moveZ = joystick.Vertical;

        float moveY = 0f;

        if (Input.GetKey(KeyCode.Space)) moveY += 1f;
        if (Input.GetKey(KeyCode.LeftShift)) moveY -= 1f;

        Vector3 movement = new Vector3(moveX, moveY, moveZ) * moveSpeed * Time.deltaTime;

        Vector3 newPos = transform.position + movement;

        newPos.x = Mathf.Clamp(newPos.x, ABorder, DBorder);
        newPos.y = Mathf.Clamp(newPos.y, MinHeight, MaxHeight);
        newPos.z = Mathf.Clamp(newPos.z, SBorder, WBorder);

        transform.position = newPos;
        HandlePinchZoom();
    }

    public void ShiftPosition(Vector3 positionStart, Vector3 positionEnd, List<Node> path)
    {
        float minX = Mathf.Infinity, maxX = -Mathf.Infinity, minZ = Mathf.Infinity, maxZ = -Mathf.Infinity;
        Debug.Log("Iloœæ nodów: " + path.Count);
        for (int i = 0; i < path.Count; i++)
        {
            Debug.Log(path[i].transform.position.x + " " + path[i].transform.position.z);
            if (path[i].transform.position.x < minX)
            {
                minX = path[i].transform.position.x;
            }
            if (path[i].transform.position.x > maxX)
            {
                maxX = path[i].transform.position.x;
            }
            if (path[i].transform.position.z < minZ)
            {
                minZ = path[i].transform.position.z;
            }
            if (path[i].transform.position.z > maxZ)
            {
                maxZ = path[i].transform.position.z;
            }
            Debug.Log("Przerobione Nody: " + (i+1) + " Wartoœci skrajne: minX:" + minX + " maxX:" + maxX + " minZ:" + minZ + " maxZ:" + maxZ);
        }
        Vector3 largestCords = new Vector3(maxX, 0, maxZ);
        Vector3 smallestCords = new Vector3(minX, 0, minZ);

        float distance = Vector3.Distance(largestCords, smallestCords);
        if (distance < MinHeight)
        {
            distance = MinHeight;
        }

        Camera.transform.position = new Vector3(    
            (minX + maxX) /2,
            distance,
            (minZ + maxZ) /2
        );
    }
    void HandlePinchZoom()
    {
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            float currentDistance = Vector2.Distance(t0.position, t1.position);

            // pierwsza klatka – inicjalizacja
            if (lastPinchDistance == 0f)
            {
                lastPinchDistance = currentDistance;
                return;
            }

            float delta = currentDistance - lastPinchDistance;

            // skalowanie (wa¿ne – bez tego bêdzie za szybkie)
            float zoomAmount = delta * 0.01f;

            Vector3 pos = transform.position;
            pos.y += zoomAmount * moveSpeed;

            // clamp wysokoœci
            pos.y = Mathf.Clamp(pos.y, MinHeight, MaxHeight);

            transform.position = pos;

            lastPinchDistance = currentDistance;
        }
        else
        {
            lastPinchDistance = 0f;
        }
    }
}
