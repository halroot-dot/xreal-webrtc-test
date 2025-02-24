
using NRKernal;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NRKernal
{
    public static class GlassChecker
    {
        public enum GlassType
        {
            Light = 1,
            Air = 2,
            P55E = 3,
            P55F = 4,
            Flora = 5,
            Gina = 6,
            GF = 7,
        }
        private static int Nreal_GlassType_min = 2;
        private static int Nreal_GlassType_max = 999;
        private static AndroidJavaClass sNativeGlass;
        private static AndroidJavaObject sNativeObject;
        private static AndroidJavaObject NativeObject
        {
            get
            {
                if (sNativeObject == null)
                {
                    sNativeGlass = new AndroidJavaClass("com.xreal.glassesdisplayplugevent.GlassesInitSetting");
                    sNativeObject = sNativeGlass.CallStatic<AndroidJavaObject>("getInstance");
                }
                return sNativeObject;
            }
        }
        private static bool m_Inited = false;

        public static GlassType GetGlassType()
        {
            Init();
            GlassType glassType = (GlassType)checkGlassesType();
            Debug.Log($"[GlassChecker] GetGlassType: glassType={glassType}");
            return glassType;
        }
        public static int checkGlassesType()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return NativeObject.Call<int>("checkGlassesType");
#endif
            return 0;
        }

        private static void Init()
        {
            if (!m_Inited)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                NativeObject.Call("init", NRAppManager.CurrentActivity);
#endif
                m_Inited = true;
                Debug.Log("[GlassChecker] Init");
            }
        }

    }
}