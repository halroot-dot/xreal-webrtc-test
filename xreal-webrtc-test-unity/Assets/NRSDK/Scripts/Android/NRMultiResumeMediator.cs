/****************************************************************************
* Copyright 2019 Xreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.xreal.com/         
* 
*****************************************************************************/

namespace NRKernal
{
    using System;
    using UnityEngine;

    public class XRDisplayListener : IXRDisplayListener
    {
        public delegate void onXRDisplayAddedCB(int i, int width, int height);
        public delegate void onXRDisplayRemovedCB(int i);

        private onXRDisplayAddedCB onDisplayAdded;
        private onXRDisplayRemovedCB onDisplayRemoved;
        public XRDisplayListener(onXRDisplayAddedCB onAdded, onXRDisplayRemovedCB onRemoved)
        {
            onDisplayAdded = onAdded;
            onDisplayRemoved = onRemoved;
        }

        public void onXRDisplayAdded(int i, int width, int height)
        {
            onDisplayAdded?.Invoke(i, width, height);
        }

        public void onXRDisplayRemoved(int i)
        {
            onDisplayRemoved?.Invoke(i);
        }
    }

    /// <summary>
    /// The singleton mediator of native. It is used to accept message from native. </summary>
    public class NRMultiResumeMediator : SingletonBehaviour<NRMultiResumeMediator>
    {
        /// <summary> Whether is in the multi-resume background state. </summary>
        public static bool isMultiResumeBackground = false;

        /// <summary> FloatingWindow show/hide state change event </summary>
        public static event Action<bool> FloatingWindowStateChanged;
        /// <summary> FloatingWindow click event </summary>
        public static event Action FloatingWindowClicked;

        private static AndroidJavaClass mMultiResumeNativeInstance;
        private static XRDisplayProxy mXRDisplayProxy = null;

        static AndroidJavaClass nativeInstance
        {
            get
            {
                if (mMultiResumeNativeInstance == null)
                    mMultiResumeNativeInstance = new AndroidJavaClass("ai.nreal.activitylife.NRXRApp");
                return mMultiResumeNativeInstance;
            }
        }

        private class FloatingManagerListener : AndroidJavaProxy
        {
            public FloatingManagerListener() : base("ai.nreal.activitylife.IFloatingManagerCallback")
            {

            }
            public void onFloatingViewShown()
            {
                NRMultiResumeMediator.FloatingWindowStateChanged?.Invoke(true);
            }

            public void onFloatingViewDismissed()
            {
                NRMultiResumeMediator.FloatingWindowStateChanged?.Invoke(false);
            }

            public void onFloatingViewClicked()
            {
                NRMultiResumeMediator.FloatingWindowClicked?.Invoke();
            }
        }

#if !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ListenFloatingManager()
        {
            try
            {
                var cls = new AndroidJavaClass("ai.nreal.activitylife.FloatingManager");
                cls.CallStatic("setNRXRAppCallback", new FloatingManagerListener());
            }
            catch (Exception e)
            {
            }
        }
#endif

        /// <summary> Set the state of multi-resume background. </summary>
        /// <param name="state">   The state.</param>
        void SetMultiResumeBackground(string state)
        {
            isMultiResumeBackground = state == "true";
            NRDebugger.Info("SetMultiResumeBackground: state={0}, isMultiResumeBackground={1}", state, isMultiResumeBackground);
        }

        /// <summary> Broadcast the controller display mode. </summary>
        /// <param name="displayMode">   The display mode.</param>
        public static void BroadcastControllerDisplayMode(int displayMode)
        {
            if (!NRSessionManager.Instance.NRSessionBehaviour.SessionConfig.SupportMultiResume)
                return;

            try
            {
                nativeInstance.CallStatic("broadcastControllerDisplayMode", displayMode);
            }
            catch (Exception e)
            {
                NRDebugger.Error("BroadcastDisplayMode: {0}", e.Message);
            }
        }
        /// <summary> Broadcast if it support to dynamic switch dp without process quit. </summary>
        /// <param name="dynamicSwitchSDK">   dynamic swith sdk.</param>
        public static void BroadcastDynamicSwitchDP()
        {
            if (!NRSessionManager.Instance.NRSessionBehaviour.SessionConfig.SupportMultiResume)
                return;

            try
            {
                nativeInstance.CallStatic("broadcastDynamicSwitchDP");
            }
            catch (Exception e)
            {
                NRDebugger.Error("BroadcastDynamicSwitchDP: {0}", e.Message);
            }
        }

        public static void ForceKill()
        {
            if (!NRSessionManager.Instance.NRSessionBehaviour.SessionConfig.SupportMultiResume)
                return;

            try
            {
                nativeInstance.CallStatic("forceKill");
            }
            catch (Exception e)
            {
                NRDebugger.Error("ForceKill: {0}", e.Message);
            }
        }

        public static void MoveToBackOnNR()
        {
            try
            {
                nativeInstance.CallStatic("moveToBackOnNR");
            }
            catch (Exception e)
            {
                NRDebugger.Error("moveToBackOnNR: {0}", e.Message);
                throw;
            }
        }

        public static AndroidJavaObject GetFakeActivity()
        {
            return nativeInstance.CallStatic<AndroidJavaObject>("getFakeActivity");
        }

        public static int GetXrealGlassesDisplayId()
        {
            return nativeInstance.CallStatic<int>("getNrealGlassesDisplayId");
        }

        
        /// <summary> Prepare for switching display dynamically. </summary>
        public static void PrepareDynamicSwitchDP()
        {
            if (!NRSessionManager.Instance.NRSessionBehaviour.SessionConfig.SupportMultiResume)
                return;

            try
            {
                nativeInstance.CallStatic("prepareDynamicSwitchDP");
            }
            catch (Exception e)
            {
                NRDebugger.Error("prepareDynamicSwitchDP: {0}", e.Message);
            }
        }
        
        /// <summary> Is it ready for switching display dynamically. </summary>
        public static bool ReadyForDynamicSwitchDP()
        {
            return nativeInstance.CallStatic<bool>("readyForDynamicSwitchDP");
        }
        
        /// <summary> Is it ready for restarting session. </summary>
        public static bool ReadyForRestartSession()
        {
            return nativeInstance.CallStatic<bool>("readyForRestartSession");
        }

        public static void AddXRDisplayListener(IXRDisplayListener listener)
        {
            if (mXRDisplayProxy == null)
                mXRDisplayProxy = new XRDisplayProxy(nativeInstance);
            mXRDisplayProxy.AddListener(listener);
        }
        
        public static void RemoveXRDisplayListener(IXRDisplayListener listener)
        {
            mXRDisplayProxy?.RemoveListener(listener);
        }
    }
}
