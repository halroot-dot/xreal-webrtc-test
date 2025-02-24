/****************************************************************************
* Copyright 2019 Xreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.xreal.com/        
* 
*****************************************************************************/

using System;

namespace NRKernal
{
    using UnityEngine;
    using UnityEngine.UI;
    using System.Collections;
    using System.IO;
    using System.Collections.Generic;

    public class NRTrackingModeChangedTip : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_PauseRendererGroup;
        [SerializeField]
        private SpriteRenderer m_FullScreenMask;
        [SerializeField]
        private AnimationCurve m_AnimationCurve;
        private Coroutine m_FadeInCoroutine;
        private Coroutine m_FadeOutCoroutine;

        private static NativeResolution resolution = new NativeResolution(1920, 1080);

        public static NRTrackingModeChangedTip Create()
        {
            NRTrackingModeChangedTip lostTrackingTip;
            var config = NRSessionManager.Instance.NRSessionBehaviour?.SessionConfig;
            if (config == null || config.TrackingModeChangeTipPrefab == null)
            {
                lostTrackingTip = GameObject.Instantiate(Resources.Load<NRTrackingModeChangedTip>("NRTrackingModeChangedTip"));
            }
            else
            {
                lostTrackingTip = GameObject.Instantiate(config.TrackingModeChangeTipPrefab);
            }
#if !UNITY_EDITOR
            resolution = NRFrame.GetDeviceResolution(NativeDevice.LEFT_DISPLAY);
#endif

            NRDebugger.Info("[NRTrackingModeChangedTip] Created");
            return lostTrackingTip;
        }

        private void Awake()
        {
            NRDebugger.Info("[NRTrackingModeChangedTip] Awake");
            Initialize();
        }

        private void Initialize()
        {
            m_FullScreenMask.sortingOrder = 9999;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (m_FadeInCoroutine != null)
            {
                NRKernalUpdater.Instance.StopCoroutine(m_FadeInCoroutine);
            }
            if (m_FadeOutCoroutine != null)
            {
                NRKernalUpdater.Instance.StopCoroutine(m_FadeOutCoroutine);
            }

            m_FadeInCoroutine = StartCoroutine(FadeIn());

        }
        public void Hide()
        {
            if (m_FadeInCoroutine != null)
            {
                NRKernalUpdater.Instance.StopCoroutine(m_FadeInCoroutine);
            }
            if (m_FadeOutCoroutine != null)
            {
                NRKernalUpdater.Instance.StopCoroutine(m_FadeOutCoroutine);
            }
            m_FadeOutCoroutine = StartCoroutine(FadeOut());
        }

        void LateUpdate()
        {
            //避免Mesh被视椎体裁减
            var centerAnchor = NRSessionManager.Instance.CenterCameraAnchor;
            if (centerAnchor != null)
            {
                m_PauseRendererGroup.transform.position = centerAnchor.position + centerAnchor.forward * 3;
                m_PauseRendererGroup.transform.rotation = centerAnchor.rotation;
            }
        }
        
        private IEnumerator FadeIn()
        {
            m_FullScreenMask.sharedMaterial.color = Color.black;
            
            yield return 0;
        }

        private IEnumerator FadeOut()
        {
            m_FullScreenMask.sharedMaterial.color = Color.black;
            yield return null;
            yield return null;
            yield return null;

            var FadeOutDuring = 1f;
            var TimeElapse = 0f;
            while (true)
            {
                float percent = TimeElapse / FadeOutDuring;
                percent = m_AnimationCurve.Evaluate(percent);
                percent = Mathf.Clamp(1.0f - percent, 0, 1);
                m_FullScreenMask.sharedMaterial.color =  new Color(0f, 0f, 0f, percent);
                yield return null;

                TimeElapse += Time.deltaTime;
                if (TimeElapse >= FadeOutDuring)
                {
                    break;
                }
            }
            gameObject.SetActive(false);
        }
    }
}
