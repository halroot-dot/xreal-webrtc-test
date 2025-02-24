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
    using System.Collections;
    using UnityEngine;

    public class NRTrackingModeChangedListener : IDisposable
    {
        public delegate void OnTrackStateChangedDel(bool trackChanging);
        public event OnTrackStateChangedDel OnTrackStateChanged;
        private NRTrackingModeChangedTip m_LostTrackingTip;
        private Coroutine m_EnableRenderCamera;
        private Coroutine m_DisableRenderCamera;
        private const float MinTimeLastLimited = 0.5f;
        private const float MaxTimeLastLimited = 6f;

        public NRTrackingModeChangedListener()
        {
            NRHMDPoseTracker.OnChangeTrackingMode += OnChangeTrackingMode;
        }

        private void OnChangeTrackingMode(TrackingType origin, TrackingType target)
        {
            NRDebugger.Info("[NRTrackingModeChangedListener] OnChangeTrackingMode: {0} => {1}", origin, target);
            ShowTips();
        }

        private void ShowTips()
        {
            if (m_EnableRenderCamera != null)
            {
                NRKernalUpdater.Instance.StopCoroutine(m_EnableRenderCamera);
                m_EnableRenderCamera = null;
            }
            if (m_DisableRenderCamera != null)
            {
                NRKernalUpdater.Instance.StopCoroutine(m_DisableRenderCamera);
                m_DisableRenderCamera = null;
            }
            m_EnableRenderCamera = NRKernalUpdater.Instance.StartCoroutine(EnableTrackingInitializingRenderCamera());
        }

        public IEnumerator EnableTrackingInitializingRenderCamera()
        {
            if (m_LostTrackingTip == null)
            {
                m_LostTrackingTip = NRTrackingModeChangedTip.Create();
            }
            m_LostTrackingTip.Show();
            var reason = NRFrame.LostTrackingReason;

            float begin_time = Time.realtimeSinceStartup;
            var endofFrame = new WaitForEndOfFrame();
            yield return endofFrame;
            yield return endofFrame;
            yield return endofFrame;
            NRDebugger.Info("[NRTrackingModeChangedListener] Enter tracking initialize mode...");
            OnTrackStateChanged?.Invoke(true);

            NRHMDPoseTracker postTracker = NRSessionManager.Instance.NRHMDPoseTracker;
            while ((NRFrame.LostTrackingReason != LostTrackingReason.NONE || postTracker.IsTrackModeChanging || (Time.realtimeSinceStartup - begin_time) < MinTimeLastLimited)
                && (Time.realtimeSinceStartup - begin_time) < MaxTimeLastLimited)
            {
                NRDebugger.Info("[NRTrackingModeChangedListener] Wait for tracking: modeChanging={0}, lostTrackReason={1}",
                    postTracker.IsTrackModeChanging, NRFrame.LostTrackingReason);
                yield return endofFrame;
            }

            if (m_DisableRenderCamera == null)
            {
                m_DisableRenderCamera = NRKernalUpdater.Instance.StartCoroutine(DisableTrackingInitializingRenderCamera());
            }
            m_EnableRenderCamera = null;
        }

        public IEnumerator DisableTrackingInitializingRenderCamera()
        {
            if (m_LostTrackingTip != null)
            {
                m_LostTrackingTip.Hide();
            }
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            OnTrackStateChanged?.Invoke(false);
            NRDebugger.Info("[NRTrackingModeChangedListener] Exit tracking initialize mode...");
            m_DisableRenderCamera = null;
        }

        public void Dispose()
        {
            NRHMDPoseTracker.OnChangeTrackingMode -= OnChangeTrackingMode;

            if (m_EnableRenderCamera != null)
            {
                NRKernalUpdater.Instance.StopCoroutine(m_EnableRenderCamera);
                m_EnableRenderCamera = null;
            }
            if (m_DisableRenderCamera != null)
            {
                NRKernalUpdater.Instance.StopCoroutine(m_DisableRenderCamera);
                m_DisableRenderCamera = null;
            }

            if (m_LostTrackingTip != null)
            {
                GameObject.Destroy(m_LostTrackingTip.gameObject);
                m_LostTrackingTip = null;
            }
        }
    }
}
