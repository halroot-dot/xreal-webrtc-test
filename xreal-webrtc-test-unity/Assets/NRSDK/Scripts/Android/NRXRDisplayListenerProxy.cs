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
    using System;
    using System.Collections.Generic;


    public interface IXRDisplayListener
    {
        void onXRDisplayAdded(int i, int width, int height);
        void onXRDisplayRemoved(int i);
    }

    public class XRDisplayProxy : AndroidJavaProxy
    {
        private List<IXRDisplayListener> m_XRDisplayListenerList;
        private AndroidJavaClass m_NativeInstance;

        public XRDisplayProxy(AndroidJavaClass nativeInstance) : base("ai.nreal.activitylife.IXRDisplayListener")
        {
            NRDebugger.Info("[XRDisplayProxy] init");
            m_NativeInstance = nativeInstance;
        }

        public void AddListener(IXRDisplayListener callback)
        {
            if (m_XRDisplayListenerList == null)
            {
                m_XRDisplayListenerList = new List<IXRDisplayListener>();
            }
            if (m_XRDisplayListenerList.Count == 0)
            {
#if !UNITY_EDITOR
                m_NativeInstance.CallStatic("setXRDisplayListener", this);
#endif
            }
            if (!m_XRDisplayListenerList.Contains(callback))
                m_XRDisplayListenerList.Add(callback);
        }

        public void RemoveListener(IXRDisplayListener callback)
        {
            if (m_XRDisplayListenerList != null && m_XRDisplayListenerList.Count > 0)
            {
                m_XRDisplayListenerList.Remove(callback);
                if (m_XRDisplayListenerList.Count == 0)
                {
#if !UNITY_EDITOR
                    //pass null parameter will cause signature resolved as ()V .
                    //m_NativeInstance.CallStatic("setXRDisplayListener", null);
                    var methodID = AndroidJNI.GetStaticMethodID(m_NativeInstance.GetRawClass(), "setXRDisplayListener", "(Lai/nreal/activitylife/IXRDisplayListener;)V");
                    jvalue[] args = new jvalue[1];
                    args[0].l = IntPtr.Zero;
                    AndroidJNI.CallStaticVoidMethod(m_NativeInstance.GetRawClass(), methodID, args);

#endif
                }
            }
        }

        void onXRDisplayAdded(int dpID, int width, int height)
        {
            if (m_XRDisplayListenerList != null)
            {
                for (int i = 0; i < m_XRDisplayListenerList.Count; i++)
                {
                    m_XRDisplayListenerList[i].onXRDisplayAdded(dpID, width, height);
                }
            }
        }

        void onXRDisplayRemoved(int dpID)
        {
            if (m_XRDisplayListenerList != null)
            {
                for (int i = 0; i < m_XRDisplayListenerList.Count; i++)
                {
                    m_XRDisplayListenerList[i].onXRDisplayRemoved(dpID);
                }
            }
        }
    }
}

