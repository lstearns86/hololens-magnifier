using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HoloLensCameraStream;
using UnityEngine.VR.WSA.Input;

public class HeadsUpDisplay : MonoBehaviour {

    const int hudWidth = 1268;
    const int hudHeight = 700;
    const int windowSizeDefault = 300;
    const int testMessageSize = 3000;
    int[] defaultFontSizes = new int[] { 60, 50, 44, 36, 28, 24 };
    int[] defaultTextPositions = new int[] { 130, 65, 5, -50, -100, -140 };

    public Canvas HeadsUpDisplayCanvas;
    public RawImage OuterMask;
    public RawImage Thumbnail;
    public Text DebugText;
    public Text TestMessage;

    Vector3 dragStartPosition, imageStartPosition;

    private float zoom = 1;
    private float minZoom = 1;
    private float scale = 1;
    private Vector2 resolution;
    private float dragStartZoom = 1, dragStartScale = 1;

    // Use this for initialization
    void Start () {

        DebugText.text = "";
        TestMessage.gameObject.SetActive(false);
    }

    public void SetDebugText(string text)
    {
        DebugText.text = text;
    }

    public void StartDragging(Vector3 handPosition)
    {
        dragStartPosition = handPosition;
        imageStartPosition = OuterMask.rectTransform.anchoredPosition;
        dragStartScale = scale;
        dragStartZoom = zoom;
    }

    public void SetResolution(int width, int height)
    {
        this.resolution = new Vector2(width, height);
        Thumbnail.texture = new Texture2D(width, height, TextureFormat.BGRA32, false);
        zoom = Math.Max((float)windowSizeDefault / width, (float)windowSizeDefault / height);
        minZoom = zoom;
        OuterMask.rectTransform.sizeDelta = new Vector2(windowSizeDefault * scale, windowSizeDefault * scale);
        Thumbnail.rectTransform.sizeDelta = new Vector2(width * scale * zoom, height * scale * zoom);
        //DebugText.text = "Zoom: " + zoom + ", Scale: " + scale + ", Width: " + width + ", Height: " + height;
    }

    private void ConstrainPosition()
    {
        //float x = OuterMask.rectTransform.anchoredPosition.x;
        //float y = OuterMask.rectTransform.anchoredPosition.y;
        //if (x < -hudWidth / 2) x = -hudWidth / 2;
        //if (x + OuterMask.rectTransform.rect.width > hudWidth / 2) x = hudWidth / 2 - OuterMask.rectTransform.rect.width;
        //if (y > hudHeight / 2) y = hudHeight / 2;
        //if (y - OuterMask.rectTransform.rect.height < -hudHeight / 2) y = -hudHeight / 2 + OuterMask.rectTransform.rect.height;
        //OuterMask.rectTransform.anchoredPosition = new Vector3(x, y);
    }

    public void ResetPosition()
    {
        //OuterMask.rectTransform.anchoredPosition = new Vector3(hudWidth / 2 - OuterMask.rectTransform.rect.width, hudHeight / 2);
        OuterMask.rectTransform.anchoredPosition = new Vector2(0.5f, 0.5f);
        //OuterMask.rectTransform.pos
    }

    public void UpdateDragging(Vector3 handPosition, MagnificationManager.DragMode dragMode)
    {
        Vector3 delta = handPosition - dragStartPosition;
        delta = Camera.main.transform.InverseTransformDirection(delta);

        if (dragMode == MagnificationManager.DragMode.Move)
        {
            float newX = imageStartPosition.x + delta.x * 3000;
            float newY = imageStartPosition.y + delta.y * 3000;
            SetPosition(new Vector2(newX, newY));
        }
        else if(dragMode == MagnificationManager.DragMode.Scale)
        {
            float dragFactor = 1;
            if(Math.Abs(delta.x) > Math.Abs(delta.y)) dragFactor = delta.x; else dragFactor = delta.y;
            dragFactor = (float)Math.Pow(2, dragFactor * 3);
            SetScale(dragStartScale * dragFactor);
            //DebugText.text = "Zoom: " + zoom + ", Scale: " + scale + ", Width: " + resolution.x + ", Height: " + resolution.y;
        }
        else if (dragMode == MagnificationManager.DragMode.Zoom)
        {
            float dragFactor = 1;
            if (Math.Abs(delta.x) > Math.Abs(delta.y)) dragFactor = delta.x; else dragFactor = delta.y;
            dragFactor = (float)Math.Pow(2, dragFactor * 3);
            SetZoom(dragStartZoom * dragFactor);
            //DebugText.text = "Zoom: " + zoom + ", Scale: " + scale + ", Width: " + resolution.x + ", Height: " + resolution.y;
        }
    }

    public void SetPosition(Vector2 position, bool anchorCenter = false)
    {
        if(anchorCenter)
            OuterMask.rectTransform.anchoredPosition = new Vector2(position.x - OuterMask.rectTransform.rect.width / 2, position.y + OuterMask.rectTransform.rect.height / 2);
        else
            OuterMask.rectTransform.anchoredPosition = position;
        ConstrainPosition();
    }

    public Vector2 GetPosition()
    {
        return OuterMask.rectTransform.anchoredPosition;
    }

    public void SetDistance(float distance)
    {
        HeadsUpDisplayCanvas.planeDistance = distance;
        //HeadsUpDisplayCanvas.scaleFactor = distance;
    }

    public float GetDistance()
    {
        return HeadsUpDisplayCanvas.planeDistance;
    }

    public void SetScale(float scale)
    {
        this.scale = scale;
        OuterMask.rectTransform.sizeDelta = new Vector2(windowSizeDefault * scale, windowSizeDefault * scale);
        Thumbnail.rectTransform.sizeDelta = new Vector2(resolution.x * scale * zoom, resolution.y * scale * zoom);
        TestMessage.rectTransform.localScale = new Vector3(scale, scale, 1);
        //TestMessage.rectTransform.sizeDelta = new Vector2(testMessageSize * scale, testMessageSize * scale);
        //Text[] lines = TestMessage.GetComponentsInChildren<Text>();
        //for(int i = 0; i < lines.Length; i++)
        //{
        //    Text line = lines[i];
        //    //line.rectTransform.localScale = new Vector3(1 / scale, 1 / scale, 1);
        //    line.fontSize = (int)Math.Round(defaultFontSizes[i] * scale);
        //    line.rectTransform.anchoredPosition = new Vector2(0, defaultTextPositions[i] * scale);
        //}
        ConstrainPosition();
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

    public void ShowTestMessage()
    {
        TestMessage.gameObject.SetActive(true);
        Thumbnail.gameObject.SetActive(false);
    }

    public void HideTestMessage()
    {
        TestMessage.gameObject.SetActive(false);
        Thumbnail.gameObject.SetActive(true);
    }
}
