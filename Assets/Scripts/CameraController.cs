using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float WBorder, SBorder, ABorder, DBorder, MaxHeight = 85f, MinHeight = 15f, moveSpeed = 5f;
    [SerializeField] private GameObject Camera;

    void Update()
    {
        float moveX = 0f, moveZ = 0f, moveY = 0f;

        // WS = forward/back (Z axis)
        if (Input.GetKey(KeyCode.W) && transform.position.z < WBorder)
            moveZ += 1f;

        if (Input.GetKey(KeyCode.S) && transform.position.z > SBorder)
            moveZ -= 1f;

        // AD = left/right (X axis)
        if (Input.GetKey(KeyCode.D) && transform.position.x < DBorder)
            moveX += 1f;

        if (Input.GetKey(KeyCode.A) && transform.position.x > ABorder)
            moveX -= 1f;

        // Space/Shift = up/down (Y axis)
        if (Input.GetKey(KeyCode.Space)     && transform.position.y < MaxHeight)
            moveY += 1f;

        if (Input.GetKey(KeyCode.LeftShift) && transform.position.y > MinHeight)
            moveY -= 1f;


        Vector3 movement = new Vector3(moveX, moveY, moveZ) * moveSpeed * Time.deltaTime;
        transform.Translate(movement, Space.World);
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
}
