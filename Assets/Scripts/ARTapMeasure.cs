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

    [Header("Animation")]
    public Animator animator;
    private bool foundpoint = false;

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
        UpdatePreviewPoint();
        UpdatePreviewLine();
        BillboardAllTexts();

        if (sphereTransform != null)
        {
            sphereTransform.localPosition = Vector3.Lerp(
                sphereTransform.localPosition,
                sphereTargetPosition,
                Time.deltaTime * snapSpeed
           );
        }

        if (snappedPosition.HasValue)
        {

            ringTransform.rotation = Quaternion.Lerp(
                ringTransform.rotation,
                previewTargetRotation,
                Time.deltaTime * snapSpeed
            );
        }
        else
        {
            ringTransform.localRotation = Quaternion.Lerp(
                ringTransform.localRotation,
                Quaternion.Euler(-90f, 0f, 0f),
                Time.deltaTime * snapSpeed
            );
        }
    }

    void UpdatePreviewPoint()
    {
        if (!foundpoint)
        {
            List<ARRaycastHit> tempHits = new List<ARRaycastHit>();
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            if (raycastManager.Raycast(screenCenter, tempHits,
                TrackableType.FeaturePoint | TrackableType.Depth | TrackableType.PlaneWithinPolygon))
            {
                foundpoint = true;
                animator.SetBool("FoundPlane", true);
            }
        }
        
        // No longer doing manual depth calculations. Relying on AR Core Lab's implementation
        Vector3 targetPosition = previewPoint.transform.position;

        snappedPosition = null;
        float closestDist = snapDistanceThreshold;
        Vector3 closestSnap = Vector3.zero;

        // Check against all measurement endpoints
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

        // Check against midpoints of measurements
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

        // Apply snapping or reset
        if (snappedPosition.HasValue)
        {
            sphereTargetPosition = previewPoint.transform.InverseTransformPoint(snappedPosition.Value);

            if (!lastSnappedPosition.HasValue ||
                Vector3.Distance(lastSnappedPosition.Value, snappedPosition.Value) > 0.001f)
            {
                RDG.Vibration.Vibrate(25, 100);
                lastSnappedPosition = snappedPosition;
            }

            previewTargetRotation = Quaternion.LookRotation(
                Camera.main.transform.position - ringTransform.transform.position
            );
        }
        else
        {
            sphereTargetPosition = Vector3.zero;
            lastSnappedPosition = null;
        }

        previewPoint.SetActive(true);
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