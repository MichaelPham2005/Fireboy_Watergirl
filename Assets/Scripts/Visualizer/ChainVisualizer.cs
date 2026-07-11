using UnityEngine;
using System.Collections.Generic;

public class ChainVisualizer : MonoBehaviour
{
    [Header("Các điểm neo")]
    public Transform platformLeftHook;   
    public Transform anchorLeft;         
    public Transform anchorRight;        
    public Transform platformRightHook;  

    [Header("Danh sách mắt xích (Đúng 19 cái)")]
    public List<GameObject> chainLinks;
    public float rotationOffset = 90f; 

    [Header("Cấu hình Ròng rọc & Độ đè")]
    public float cornerRadius = 0.5f;
    public int curveResolution = 10; 
    
    [Tooltip("Khoảng cách thực tế giữa các tâm mắt xích. Giảm số này xuống để xích đè lên nhau nhiều hơn.")]
    public float linkSpacing = 0.6f; 

    // Mảng lưu trữ quỹ đạo dây
    private List<Vector3> pathPoints = new List<Vector3>();

    void Update()
    {
        if (chainLinks == null || chainLinks.Count == 0) return;

        BuildPath();
        PlaceChainLinks();
    }

    void BuildPath()
    {
        pathPoints.Clear();

        Vector3 dir1 = (anchorLeft.position - platformLeftHook.position).normalized;
        Vector3 dir2 = (anchorRight.position - anchorLeft.position).normalized;
        Vector3 dir3 = (platformRightHook.position - anchorRight.position).normalized;

        Vector3 leftCurveStart = anchorLeft.position - dir1 * cornerRadius;
        Vector3 leftCurveEnd = anchorLeft.position + dir2 * cornerRadius;

        Vector3 rightCurveStart = anchorRight.position - dir2 * cornerRadius;
        Vector3 rightCurveEnd = anchorRight.position + dir3 * cornerRadius;

        pathPoints.Add(platformLeftHook.position);

        for (int i = 0; i <= curveResolution; i++)
        {
            float t = i / (float)curveResolution;
            pathPoints.Add(CalculateQuadraticBezierPoint(t, leftCurveStart, anchorLeft.position, leftCurveEnd));
        }

        for (int i = 0; i <= curveResolution; i++)
        {
            float t = i / (float)curveResolution;
            pathPoints.Add(CalculateQuadraticBezierPoint(t, rightCurveStart, anchorRight.position, rightCurveEnd));
        }

        pathPoints.Add(platformRightHook.position);
    }

    Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        Vector3 p = uu * p0;
        p += 2 * u * t * p1;
        p += tt * p2;
        return p;
    }

    void PlaceChainLinks()
    {
        // 1. Tính toán quỹ đạo chuẩn (KHÔNG TRỪ HAO Ở ĐÂY)
        float[] distances = new float[pathPoints.Count];
        float totalPathLength = 0;
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            totalPathLength += Vector3.Distance(pathPoints[i], pathPoints[i + 1]);
            distances[i + 1] = totalPathLength;
        }

        // 2. Rải đều mắt xích với khoảng cách cố định (Fixed Spacing)
        for (int i = 0; i < chainLinks.Count; i++)
        {
            // Điểm mấu chốt: targetDist tính bằng chỉ số mắt xích nhân với khoảng cách mong muốn
            float targetDist = i * linkSpacing;

            // Ẩn các xích bị dư nếu khoảng cách tính toán lố ra khỏi chiều dài dây
            if (targetDist > totalPathLength)
            {
                chainLinks[i].SetActive(false);
                continue;
            }
            else
            {
                chainLinks[i].SetActive(true);
            }

            // Nội suy vị trí dựa trên targetDist chuẩn
            for (int j = 0; j < pathPoints.Count - 1; j++)
            {
                if (targetDist >= distances[j] && targetDist <= distances[j + 1])
                {
                    float segmentLength = distances[j + 1] - distances[j];
                    float t = (segmentLength == 0) ? 0 : (targetDist - distances[j]) / segmentLength;
                    
                    Vector3 pos = Vector3.Lerp(pathPoints[j], pathPoints[j + 1], t);
                    Vector3 dir = (pathPoints[j + 1] - pathPoints[j]).normalized;

                    chainLinks[i].transform.position = pos;
                    
                    if (dir != Vector3.zero)
                    {
                        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                        chainLinks[i].transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
                    }
                    break;
                }
            }
        }
    }
}