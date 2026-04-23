using System.Collections;
using UnityEngine;

public class PathMarker : MonoBehaviour
{
    public float moveSpeed = 2f; // prêdkoœæ w jednostkach/sekundê
    private LineRenderer lineRenderer;
    private Renderer rend;

    void Awake()
    {
        rend = GetComponent<Renderer>();
    }

    public void StartMoving(LineRenderer pathLine)
    {
        if (pathLine == null || pathLine.positionCount == 0)
            return;

        rend.enabled = true;

        lineRenderer = pathLine;
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

        // Zbierz punkty z linii
        Vector3[] points = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(points);

        while (true) // Pêtla nieskoñczona
        {
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector3 startPos = points[i];
                Vector3 endPos = points[i + 1];

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

            // Po dojœciu do koñca wróæ na start
            transform.position = points[0];
        }
    }
}
