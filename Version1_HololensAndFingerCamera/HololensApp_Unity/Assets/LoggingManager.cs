﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if !UNITY_EDITOR
using Windows.Foundation.Diagnostics;
#endif

public class LoggingManager : MonoBehaviour {

#if !UNITY_EDITOR
    static LoggingChannel portalLogger = new LoggingChannel("HandSight Magnifier", null, new Guid("b653de4e-21fe-4893-8d6d-ec05f0c4346a"));
#endif


    // Use this for initialization
    void Start () { }

    // Update is called once per frame
    void Update () { }

    public static void Log(string message)
    {
#if !UNITY_EDITOR
        portalLogger.LogMessage(message, LoggingLevel.Information);
#endif
    }

    public static void LogError(string message)
    {
#if !UNITY_EDITOR
        portalLogger.LogMessage(message, LoggingLevel.Error);
#endif
    }
}
