using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathMarker : MonoBehaviour
{
    public float moveSpeed = 2f; // prêdkoœæ w jednostkach/sekundê
    private LineRenderer lineRenderer;
    private Renderer rend;
    private List<Node> pathNodes;

    void Awake()
    {
        rend = GetComponent<Renderer>();
    }

    public void StartMoving(LineRenderer pathLine, List<Node> fullPath)
    {
        if (pathLine == null || pathLine.positionCount == 0 || fullPath == null || fullPath.Count == 0)
            return;

        rend.enabled = true;

        lineRenderer = pathLine;
        pathNodes = fullPath;

        StopAllCoroutines();
        StartCoroutine(MoveAlongPathLoop());
    }

    public void StopMoving()
    {
        StopAllCoroutines();
        rend.enabled = false;
    }

    private IEnumerator MoveAlongPathLoop()
    {
        while (true)
        {
            for (int i = 0; i < pathNodes.Count - 1; i++)
            {
                Node currentNode = pathNodes[i];
                Node nextNode = pathNodes[i + 1];

                Vector3 startPos = currentNode.transform.position;
                Vector3 endPos = nextNode.transform.position;

                bool isGhost = currentNode.ghostNeighbors.Contains(nextNode);

                if (isGhost)
                {
                    transform.position = endPos;
                    yield return null;
                    continue;
                }

                float t = 0f;
                float segmentLength = Vector3.Distance(startPos, endPos);
                float duration = segmentLength / moveSpeed;

                while (t < 1f)
                {
                    t += Time.deltaTime / duration;
                    transform.position = Vector3.Lerp(startPos, endPos, t);
                    yield return null;
                }
            }

            transform.position = pathNodes[0].transform.position;
        }
    }
}
