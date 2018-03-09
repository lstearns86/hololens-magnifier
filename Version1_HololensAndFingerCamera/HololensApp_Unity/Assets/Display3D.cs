using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HoloLensCameraStream;
using UnityEngine.VR.WSA.Input;
using HoloToolkit.Unity;
using HoloToolkit.Unity.SpatialMapping;

public class Display3D : MonoBehaviour {

    const int windowSizeDefault = 200;

    enum DisplayMode3D { Flat, Billboard, Tagalong }

    const float fixedDistance = 1;
    const float sphereRadius = 0.4f;
    const float moveSpeed = 5;

    private DisplayMode3D displayMode = DisplayMode3D.Billboard;

    public GameObject Display;
    public RawImage OuterMask;
    public RawImage Thumbnail;

    private Vector3 dragStartPosition, imageStartPosition;

    private float zoom = 1;
    private float minZoom = 1;
    private float scale = 1;
    private Vector2 resolution;
    private float dragStartZoom = 1, dragStartScale = 1;

    // Use this for initialization
    void Start () {
        
    }
	
	// Update is called once per frame
	void Update () {
        Vector3 directionToTarget;
		switch(displayMode)
        {
            case DisplayMode3D.Flat:
                directionToTarget = CameraCache.Main.transform.position - transform.position;
                if (directionToTarget.sqrMagnitude > 0.001f) // direction undefined if too close to the camera
                {
                    // Calculate and apply the rotation required to reorient the object
                    transform.rotation = Quaternion.LookRotation(-Vector3.up, -directionToTarget);
                    return;
                }
                break;
            case DisplayMode3D.Billboard:
                directionToTarget = CameraCache.Main.transform.position - transform.position;
                if (directionToTarget.sqrMagnitude > 0.001f) // direction undefined if too close to the camera
                {
                    // Calculate and apply the rotation required to reorient the object
                    transform.rotation = Quaternion.LookRotation(-directionToTarget);
                    return;
                }
                break;
            case DisplayMode3D.Tagalong:
                var optimalPosition = CameraCache.Main.transform.position + CameraCache.Main.transform.forward * fixedDistance;
                var offsetDir = transform.position - optimalPosition;

                if(offsetDir.magnitude > sphereRadius)
                {
                    var targetPosition = optimalPosition + offsetDir.normalized * sphereRadius;
                    float deltaTime = Time.unscaledDeltaTime;
                    transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * deltaTime);
                    //transform.position = targetPosition;
                }

                // rotate to face user (identical to billboard mode)
                directionToTarget = CameraCache.Main.transform.position - transform.position;
                if (directionToTarget.sqrMagnitude > 0.001f) // direction undefined if too close to the camera
                {
                    // Calculate and apply the rotation required to reorient the object
                    transform.rotation = Quaternion.LookRotation(-directionToTarget);
                    return;
                }
                break;
        }
	}

    public void SetResolution(int width, int height)
    {
        this.resolution = new Vector2(width, height);
        Thumbnail.texture = new Texture2D(width, height, TextureFormat.BGRA32, false);
        zoom = Math.Max((float)windowSizeDefault / width, (float)windowSizeDefault / height);
        minZoom = zoom;
        OuterMask.rectTransform.sizeDelta = new Vector2(windowSizeDefault * scale, windowSizeDefault * scale);
        Thumbnail.rectTransform.sizeDelta = new Vector2(width * scale * zoom, height * scale * zoom);
    }

    public void ResetPosition()
    {
        Display.transform.position = CameraCache.Main.transform.position + CameraCache.Main.transform.forward * fixedDistance;
    }

    public void SetPosition(Vector3 position)
    {
        Display.transform.position = position;
    }

    public Vector3 GetPosition()
    {
        return Display.transform.position;
    }

    public void SetScale(float scale)
    {
        this.scale = scale;
        OuterMask.rectTransform.sizeDelta = new Vector2(windowSizeDefault * scale, windowSizeDefault * scale);
        Thumbnail.rectTransform.sizeDelta = new Vector2(resolution.x * scale * zoom, resolution.y * scale * zoom);
    }

    public float GetScale()
    {
        return scale;
    }

    public void SetZoom(float zoom)
    {
        if (zoom < minZoom) zoom = minZoom;
        this.zoom = zoom;
        OuterMask.rectTransform.sizeDelta = new Vector2(windowSizeDefault * scale, windowSizeDefault * scale);
        Thumbnail.rectTransform.sizeDelta = new Vector2(resolution.x * scale * zoom, resolution.y * scale * zoom);
    }

    public float GetZoom()
    {
        return zoom;
    }

    public void StartDragging(Vector3 handPosition)
    {
        dragStartPosition = handPosition;
        imageStartPosition = Display.transform.position;
        dragStartScale = scale;
        dragStartZoom = zoom;
    }

    public void UpdateDragging(Vector3 handPosition, MagnificationManager.DragMode dragMode)
    {
        Vector3 delta = handPosition - dragStartPosition;
        //delta = Camera.main.worldToCameraMatrix.MultiplyVector(delta);
        //delta.z = -delta.z;

        if (dragMode == MagnificationManager.DragMode.Move)
        {
            if (displayMode != DisplayMode3D.Tagalong)
                Display.transform.position = imageStartPosition + delta * 2;
        }
        else if(dragMode == MagnificationManager.DragMode.Scale)
        {
            delta = Camera.main.transform.InverseTransformDirection(delta);
            float dragFactor = 1;
            if (Math.Abs(delta.x) > Math.Abs(delta.y)) dragFactor = delta.x; else dragFactor = delta.y;
            dragFactor = (float)Math.Pow(2, dragFactor * 3);
            SetScale(dragStartScale * dragFactor);
        }
        else if (dragMode == MagnificationManager.DragMode.Zoom)
        {
            delta = Camera.main.transform.InverseTransformDirection(delta);
            float dragFactor = 1;
            if (Math.Abs(delta.x) > Math.Abs(delta.y)) dragFactor = delta.x; else dragFactor = delta.y;
            dragFactor = (float)Math.Pow(2, dragFactor * 3);
            SetZoom(dragStartZoom * dragFactor);
        }
    }

    private bool ChildOfObject(GameObject targetObject, GameObject testObject)
    {
        GameObject parent = testObject;
        while (parent != null)
        {
            if (parent == targetObject) return true;
            try
            {
                parent = parent.transform.parent.gameObject;
            }
            catch { parent = null; }
        }

        return false;
    }

    public void SetDisplayModeFlat()
    {
        displayMode = DisplayMode3D.Flat;

        // TODO: find nearest plane and move to it
        //Display.transform.rotation = Quaternion.LookRotation(-(CameraCache.Main.transform.position - transform.position), Vector3.forward);
        //Display.transform.rotation = Quaternion.LookRotation(Vector3.up);

        if (Display.transform.position.y > CameraCache.Main.transform.position.y - 0.5) Display.transform.position = new Vector3(Display.transform.position.x, CameraCache.Main.transform.position.y - 0.5f, Display.transform.position.z);
    }

    public void SetDisplayModeBillboard()
    {
        displayMode = DisplayMode3D.Billboard;
    }

    public void SetDisplayModeTagalong()
    {
        displayMode = DisplayMode3D.Tagalong;
    }
}
