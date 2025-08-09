using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class ARPointPlacer : MonoBehaviour
{
    public GameObject pointPrefab;
    public ARRaycastManager raycastManager;

    static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Awake()
    {
    }

    void Update()
    {
        if (Touchscreen.current == null || Touchscreen.current.primaryTouch.press.isPressed == false)
            return;

        var touch = Touchscreen.current.primaryTouch;

        const TrackableType trackableTypes = TrackableType.FeaturePoint | TrackableType.PlaneWithinPolygon;

        hits = new List<ARRaycastHit>();

        // Perform the raycast.
        if (raycastManager.Raycast(touch.position.ReadValue(), hits, trackableTypes))
        {
            // Raycast hits are sorted by distance, so the first one will be the closest hit.
            Pose hitPose = hits[0].pose;
            Instantiate(pointPrefab, hitPose.position, hitPose.rotation);

            Debug.Log("TOUCHED SCREEN!!!!!!!");
            Debug.Log(touch.position.ReadValue());

            Debug.DrawRay(Camera.main.ScreenPointToRay(touch.position.ReadValue()).origin, Camera.main.transform.forward * 10, Color.yellow, 1f);
        }
 
        

        Debug.Log("Update Cycle Running!!!!!!!!!!!!!!");
    }
}
