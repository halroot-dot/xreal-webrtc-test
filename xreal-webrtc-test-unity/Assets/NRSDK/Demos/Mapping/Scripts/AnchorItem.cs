/****************************************************************************
* Copyright 2019 Xreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.xreal.com/        
* 
*****************************************************************************/

namespace NRKernal.Persistence
{
    using NRKernal.NRExamples;
    using System;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
    /// <summary> An anchor item. </summary>
    public class AnchorItem : MonoBehaviour, IPointerClickHandler
    {
        /// <summary> The key. </summary>
        public string key;
        /// <summary> The on anchor item click. </summary>
        public Action<string, GameObject> OnAnchorItemClick;
        /// <summary> The anchor panel. </summary>
        [SerializeField]
        private GameObject canvas;
        [SerializeField]
        private Text anchorUUID;
        [SerializeField]
        private Text anchorState;
        [SerializeField]
        private Button remapBtn;
        [SerializeField]
        private Image processingIcon;
        private NRWorldAnchor m_NRWorldAnchor;
        private Material m_Material;

        void Start()
        {
            if (TryGetComponent(out m_NRWorldAnchor))
            {
                if (canvas != null)
                    canvas.SetActive(true);
                if (anchorUUID != null)
                    anchorUUID.text = m_NRWorldAnchor.UUID;
                m_Material = GetComponentInChildren<Renderer>()?.material;
                if (m_Material != null)
                {
                    m_NRWorldAnchor.OnTrackingChanged += (NRWorldAnchor worldAnchor, TrackingState state) =>
                    {
                        switch (state)
                        {
                            case TrackingState.Tracking:
                                m_Material.color = Color.green;
                                break;
                            case TrackingState.Paused:
                                m_Material.color = Color.white;
                                break;
                            case TrackingState.Stopped:
                                m_Material.color = Color.red;
                                break;
                        }
                    };
                }
            }
        }

        private void Update()
        {
            if (m_NRWorldAnchor == null)
            {
                return;
            }
            updateRemapButtonState();

        }

        public void Save()
        {
            if (m_NRWorldAnchor != null)
            {
                ShowProcessingIcon(true);
                MapQualityIndicator.PauseEstimateQuality();
                m_NRWorldAnchor.SaveAnchor(OnSaveSuccess, OnSaveFailure);
            }
        }

        public void Erase()
        {
            if (m_NRWorldAnchor != null)
                m_NRWorldAnchor.EraseAnchor();
        }

        public void Destory()
        {
            if (m_NRWorldAnchor != null)
            {
                m_NRWorldAnchor.DestroyAnchor();
                if (m_NRWorldAnchor == MapQualityIndicator.CurrentAnchor)
                {
                    MapQualityIndicator.InterruptMappingGuide();
                }
            }
        }

        public void EnableRemapButton(bool enable)
        {
            remapBtn.gameObject.SetActive(enable);
        }

        public void Remap()
        {
            if (m_NRWorldAnchor != null)
            {
                if (m_NRWorldAnchor.Remap())
                {
                    MapQualityIndicator.SetCurrentAnchor(m_NRWorldAnchor);
                    MapQualityIndicator.ShowMappingGuide();
                }
            }
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            OnAnchorItemClick?.Invoke(key, gameObject);
        }


        private void updateRemapButtonState()
        {
            if (MapQualityIndicator.CurrentAnchor != null)
            {
                EnableRemapButton(false);
            }
            else
            {
                EnableRemapButton(true);
            }
        }

        private void OnSaveSuccess()
        {
            MapQualityIndicator.FinishMappingGuide();
            ShowProcessingIcon(false);
        }
        private void OnSaveFailure()
        {
            NRDebugger.Debug($"[LocalMapExample] Save anchor Failed  DestroyAnchor handle:{m_NRWorldAnchor.UUID}");
            if (MapQualityIndicator.IsRemapping)
            {
                MapQualityIndicator.InterruptMappingGuide();
                Toaster.Toast(PromptTexts.s_RemapPrompt, 12000);
            }
            else
            {
                m_NRWorldAnchor.DestroyAnchor();
                MapQualityIndicator.InterruptMappingGuide();
                Toaster.Toast(PromptTexts.s_ReAddPrompt, 12000);
            }
            ShowProcessingIcon(false);
        }


        [SerializeField]
        private ConfirmDialog m_GuideDialog;
        internal async Task ShowGuide()
        {
            m_GuideDialog.Show();
            await m_GuideDialog.WaitUntilClosed();
        }

        private void ShowProcessingIcon(bool show)
        {
            Debug.Log($"ShowProcessingIcon {show}");
            processingIcon.gameObject.SetActive(show);
        }
    }
}
