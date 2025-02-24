/****************************************************************************
* Copyright 2019 Xreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.xreal.com/        
* 
*****************************************************************************/

#if USING_XR_MANAGEMENT && USING_XR_SDK_XREAL
#define USING_XR_SDK
#endif

using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace NRKernal
{
    [StructLayout(LayoutKind.Sequential)]
    public struct XREALHandles
    {
        public ulong _nativeAPIHandle;
        public ulong _renderingHandle;
        public ulong _hmdHandle;
        public ulong _perceptionGroupHandle;
        public ulong _perceptionHandle;
        public ulong _headTrackingHandle;
    }
    public static class NativeSessionManager
    {
        const string SESSION_LIB = "libXREALNativeSessionManager";
        public static XREALHandles Handles;

        public static void SetRenderingHandle(ulong handle)
        {
            Handles._renderingHandle = handle;
            SetNRSDKHandles(ref Handles);
        }

        public static void SetHMDHandle(ulong handle)
        {
            Handles._hmdHandle = handle;
            SetNRSDKHandles(ref Handles);
        }

        public static void SetPerceptionHandle(ulong groupHandle, ulong perceptionHandle, ulong headTrackingHandle)
        {
            Handles._perceptionGroupHandle = groupHandle;
            Handles._perceptionHandle = perceptionHandle;
            Handles._headTrackingHandle = headTrackingHandle;
            SetNRSDKHandles(ref Handles);

        }

        [DllImport(SESSION_LIB)]
        internal extern static void SetSessionState(SessionState state);

        [DllImport(SESSION_LIB)]
        private extern static void SetNRSDKHandles(ref XREALHandles handles);

        [DllImport(SESSION_LIB)]
        internal extern static bool LoadConsumerModule(string modulePath);
    }
}