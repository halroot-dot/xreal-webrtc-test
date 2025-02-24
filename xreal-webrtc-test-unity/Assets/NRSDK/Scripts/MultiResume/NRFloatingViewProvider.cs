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

    public interface IFloatingViewProxy
    {
        AndroidJavaObject CreateFloatingView();
        void Show();
        void Hide();
        void DestroyFloatingView();
    }

    public class NRDefaultFloatingViewProxy : IFloatingViewProxy
    {
        private AndroidJavaObject mJavaProxyObject;

        public NRDefaultFloatingViewProxy()
        {
            var clsUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = clsUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            mJavaProxyObject = new AndroidJavaObject("ai.nreal.activitylife.NRDefaultFloatingViewProxy", activity);
        }

        public AndroidJavaObject CreateFloatingView()
        {
            return mJavaProxyObject.Call<AndroidJavaObject>("CreateFloatingView");
        }

        public void Hide()
        {
            mJavaProxyObject.Call("Hide");
        }

        public void Show()
        {
            mJavaProxyObject.Call("Show");
        }

        public void DestroyFloatingView()
        {
            mJavaProxyObject.Call("DestroyFloatingView");
        }
    }

    public class NRFloatingViewProvider : SingleTon<NRFloatingViewProvider>
    {
        protected AndroidJavaObject mJavaFloatingViewManager;
        protected AndroidJavaObject JavaFloatingViewManager
        {
            get
            {
                if (mJavaFloatingViewManager == null)
                {
                    var cls = new AndroidJavaClass("ai.nreal.activitylife.FloatingManager");
                    mJavaFloatingViewManager = cls.CallStatic<AndroidJavaObject>("getInstance");
                }
                return mJavaFloatingViewManager;
            }
        }


        protected class NRFloatingViewProxyWrapper : AndroidJavaProxy
        {
            private IFloatingViewProxy mProxy;
            public IFloatingViewProxy FloatingViewProxy => mProxy;

            public NRFloatingViewProxyWrapper(IFloatingViewProxy proxy) : base("ai.nreal.activitylife.IFloatingViewProxy")
            {
                mProxy = proxy;
            }

            public AndroidJavaObject CreateFloatingView()
            {
                if (mProxy == null)
                    return null;
                return mProxy.CreateFloatingView();
            }
            public void Show()
            {
                if (mProxy != null)
                    mProxy.Show();
            }
            public void Hide()
            {
                if (mProxy != null)
                    mProxy.Hide();
            }

            public void DestroyFloatingView()
            {
                if (mProxy != null)
                    mProxy.DestroyFloatingView();
            }
        }

        private NRFloatingViewProxyWrapper mProxyWrapper = null;

        public void RegisterFloatViewProxy(IFloatingViewProxy proxy)
        {
            Debug.Log("[NRFloatingViewProvider] RegisterFloatViewProxy: " + proxy.GetType());
            mProxyWrapper = new NRFloatingViewProxyWrapper(proxy);
            JavaFloatingViewManager.Call("setFloatingViewProxy", mProxyWrapper);
        }

        public IFloatingViewProxy GetCurrentFloatViewProxy()
        {
            if (mProxyWrapper != null)
                return mProxyWrapper.FloatingViewProxy;
            return null;
        }
    }
}
