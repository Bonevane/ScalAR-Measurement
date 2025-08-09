using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System;

public class ARMeasureTool : MonoBehaviour
{
    [Header("AR Components")]
    public ARRaycastManager raycastManager;
    public AROcclusionManager occlusionManager; // NEW for depth
    private Texture2D depthTexture;
    private short[] depthArray;
    private int depthWidth;
    private int depthHeight;

    [Header("UI & Prefabs")]
    public GameObject pointPrefab;
    public GameObject previewPoint;
    public Button placeButton;
    public Button resetButton;
    public LineRenderer previewLineRenderer;

    public GameObject worldTextCanvasInstance;
    public RectTransform worldDistanceTextInstance;

    [Header("Line Renderer Settings")]
    public Material lineMaterial;
    public float lineWidth = 0.005f;

    [Header("Snapping")]
    public float snapDistanceThreshold = 0.05f;
    private Vector3? snappedPosition = null;
    private Vector3? lastSnappedPosition = null;

    private Vector3 sphereTargetPosition;
    private Quaternion previewTargetRotation;
    private float snapSpeed = 10f;

    private Transform sphereTransform;
    private Transform ringTransform;

    private Vector3? currentStartPoint = null;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    [Header("Animation")]
    public Animator animator;

    private class Measurement
    {
        public GameObject pointA;
        public GameObject pointB;
        public LineRenderer line;
        public GameObject labelRoot;
        public TMP_Text distanceText;
        public RectTransform textRectTransform;
    }

    private List<Measurement> measurements = new List<Measurement>();

    private const float MillimeterToMeter = 0.001f;
    private const float InvalidDepthValue = -1f;

    void Start()
    {
        placeButton.onClick.AddListener(PlacePoint);
        resetButton.onClick.AddListener(ResetMeasurements);
        previewLineRenderer.positionCount = 0;

        sphereTransform = previewPoint.transform.Find("Sphere");
        ringTransform = previewPoint.transform.Find("Ring");
    }

    void Update()
    {
        UpdateEnvironmentDepthImage();
        UpdatePreviewPoint();
        UpdatePreviewLine();
        BillboardAllTexts();

        if (sphereTransform != null)
        {
            sphereTransform.position = Vector3.Lerp(
                sphereTransform.position,
                sphereTargetPosition,
                Time.deltaTime * snapSpeed
            );
        }

        if (previewPoint != null)
        {
            previewPoint.transform.rotation = Quaternion.Slerp(
                previewPoint.transform.rotation,
                previewTargetRotation,
                Time.deltaTime * snapSpeed
            );
        }
    }

    // Get and store environment depth texture
    void UpdateEnvironmentDepthImage()
    {
        if (occlusionManager &&
            occlusionManager.TryAcquireEnvironmentDepthCpuImage(out XRCpuImage image))
        {
            using (image)
            {
                if (depthTexture == null || depthTexture.width != image.width || depthTexture.height != image.height)
                {
                    depthTexture = new Texture2D(image.width, image.height, TextureFormat.R16, false);
                    depthArray = new short[image.width * image.height];
                }

                var conversionParams = new XRCpuImage.ConversionParams
                {
                    inputRect = new RectInt(0, 0, image.width, image.height),
                    outputDimensions = new Vector2Int(image.width, image.height),
                    outputFormat = TextureFormat.R16,
                    transformation = XRCpuImage.Transformation.None
                };

                var rawTextureData = depthTexture.GetRawTextureData<byte>();
                image.Convert(conversionParams, rawTextureData);
                depthTexture.Apply();

                depthWidth = image.width;
                depthHeight = image.height;

                var byteBuffer = depthTexture.GetRawTextureData();
                Buffer.BlockCopy(byteBuffer, 0, depthArray, 0, byteBuffer.Length);
            }
        }
    }

    // Depth-based lookup
    float GetDepthFromUV(Vector2 uv)
    {
        int depthX = (int)(uv.x * (depthWidth - 1));
        int depthY = (int)(uv.y * (depthHeight - 1));

        if (depthX >= depthWidth || depthX < 0 || depthY >= depthHeight || depthY < 0)
            return InvalidDepthValue;

        var depthIndex = (depthY * depthWidth) + depthX;
        var depthInShort = depthArray[depthIndex];
        return depthInShort * MillimeterToMeter;
    }

    void UpdatePreviewPoint()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        bool foundPoint = false;
        Pose hitPose = new Pose();

        // Try depth raycast first
        if (depthArray != null && depthArray.Length > 0)
        {
            float depthMeters = GetDepthFromUV(new Vector2(screenCenter.x / Screen.width, screenCenter.y / Screen.height));
            if (depthMeters > 0)
            {
                Vector3 screenPos = new Vector3(screenCenter.x, screenCenter.y, depthMeters);
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
                hitPose = new Pose(worldPos, Quaternion.identity);
                foundPoint = true;
            }
        }

        // Fallback to ARRaycast
        if (!foundPoint && raycastManager.Raycast(screenCenter, hits,
            TrackableType.FeaturePoint | TrackableType.Depth | TrackableType.PlaneWithinPolygon))
        {
            hitPose = hits[0].pose;
            foundPoint = true;
        }

        if (foundPoint)
        {
            animator.SetBool("FoundPlane", true);
            Vector3 targetPosition = hitPose.position;

            snappedPosition = null;
            float closestDist = snapDistanceThreshold;
            Vector3 closestSnap = Vector3.zero;

            previewPoint.transform.position = hitPose.position;

            foreach (var m in measurements)
            {
                float distA = Vector3.Distance(targetPosition, m.pointA.transform.position);
                if (distA < closestDist)
                {
                    closestDist = distA;
                    closestSnap = m.pointA.transform.position;
                    snappedPosition = closestSnap;
                }

                float distB = Vector3.Distance(targetPosition, m.pointB.transform.position);
                if (distB < closestDist)
                {
                    closestDist = distB;
                    closestSnap = m.pointB.transform.position;
                    snappedPosition = closestSnap;
                }
            }

            foreach (var m in measurements)
            {
                Vector3 midpoint = (m.pointA.transform.position + m.pointB.transform.position) / 2f;
                float distMid = Vector3.Distance(targetPosition, midpoint);
                if (distMid < closestDist)
                {
                    closestDist = distMid;
                    closestSnap = midpoint;
                    snappedPosition = closestSnap;
                }
            }

            if (snappedPosition.HasValue)
            {
                sphereTargetPosition = snappedPosition.Value;
                if (!lastSnappedPosition.HasValue ||
                    Vector3.Distance(lastSnappedPosition.Value, snappedPosition.Value) > 0.001f)
                {
                    RDG.Vibration.Vibrate(25, 100);
                    lastSnappedPosition = snappedPosition;
                }
                previewTargetRotation = Quaternion.LookRotation(
                    Camera.main.transform.position - previewPoint.transform.position
                ) * Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                previewTargetRotation = hitPose.rotation;
                sphereTargetPosition = targetPosition;
                lastSnappedPosition = null;
            }

            previewPoint.SetActive(true);
        }
        else
        {
            previewPoint.SetActive(false);
            snappedPosition = null;
        }
    }

    void PlacePoint()
    {
        if (!previewPoint.activeSelf) return;

        Vector3 placedPos = snappedPosition ?? sphereTransform.transform.position;
        GameObject placedSphere = Instantiate(pointPrefab, placedPos, Quaternion.identity);

        if (currentStartPoint == null)
        {
            currentStartPoint = placedPos;
        }
        else
        {
            Measurement m = new Measurement();
            m.pointA = Instantiate(pointPrefab, currentStartPoint.Value, Quaternion.identity);
            m.pointB = placedSphere;

            GameObject lineObj = new GameObject("Line");
            LineRenderer newLine = lineObj.AddComponent<LineRenderer>();
            newLine.material = lineMaterial;
            newLine.startWidth = lineWidth;
            newLine.endWidth = lineWidth;
            newLine.positionCount = 2;
            newLine.useWorldSpace = true;
            newLine.SetPositions(new Vector3[] { currentStartPoint.Value, placedPos });
            m.line = newLine;

            GameObject newLabel = Instantiate(worldDistanceTextInstance.gameObject,
                                              worldDistanceTextInstance.parent);
            TMP_Text newText = newLabel.GetComponentInChildren<TMP_Text>();
            RectTransform textRect = newLabel.GetComponent<RectTransform>();

            float dist = Vector3.Distance(currentStartPoint.Value, placedPos);
            newText.text = dist > 1f ? $"{dist:F2} m" : $"{(int)(dist * 100f)} cm";

            Vector3 mid = (currentStartPoint.Value + placedPos) / 2f;
            textRect.position = mid + (Vector3.up * 0.02f);

            m.labelRoot = newLabel;
            m.distanceText = newText;
            m.textRectTransform = textRect;

            measurements.Add(m);

            currentStartPoint = null;
        }
    }

    void UpdatePreviewLine()
    {
        if (currentStartPoint.HasValue && previewPoint.activeSelf)
        {
            Vector3 p1 = currentStartPoint.Value;
            Vector3 p2 = sphereTransform.transform.position;

            previewLineRenderer.positionCount = 2;
            previewLineRenderer.SetPositions(new Vector3[] { p1, p2 });

            float dist = Vector3.Distance(p1, p2);
            TMP_Text tmp = worldDistanceTextInstance.GetComponentInChildren<TMP_Text>();
            tmp.text = dist > 1f ? $"{dist:F2} m" : $"{(int)(dist * 100f)} cm";

            Vector3 mid = (p1 + p2) / 2f;
            worldDistanceTextInstance.position = mid + (Vector3.up * 0.02f);
            worldDistanceTextInstance.gameObject.SetActive(true);
        }
        else
        {
            previewLineRenderer.positionCount = 0;
            worldDistanceTextInstance.gameObject.SetActive(false);
        }
    }

    void BillboardAllTexts()
    {
        Camera cam = Camera.main;

        if (worldTextCanvasInstance.activeSelf)
        {
            worldDistanceTextInstance.rotation = Quaternion.LookRotation(
                worldDistanceTextInstance.position - cam.transform.position
            );
        }

        foreach (var m in measurements)
        {
            if (m.textRectTransform != null)
            {
                m.textRectTransform.rotation = Quaternion.LookRotation(
                    m.textRectTransform.position - cam.transform.position
                );
            }
        }
    }

    void ResetMeasurements()
    {
        foreach (var m in measurements)
        {
            if (m.pointA) Destroy(m.pointA);
            if (m.pointB) Destroy(m.pointB);
            if (m.line) Destroy(m.line.gameObject);
            if (m.labelRoot) Destroy(m.labelRoot);
        }
        foreach (var g in GameObject.FindGameObjectsWithTag("Sphere"))
        {
            Destroy(g);
        }

        measurements.Clear();
        currentStartPoint = null;
        snappedPosition = null;
        lastSnappedPosition = null;

        previewLineRenderer.positionCount = 0;
        previewPoint.SetActive(false);
        worldDistanceTextInstance.gameObject.SetActive(false);
    }
}