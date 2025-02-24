/****************************************************************************
* Copyright 2019 Xreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.xreal.com/        
* 
*****************************************************************************/

using NRKernal.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace NRKernal.NRExamples
{
    /// <summary> A local map example. </summary>
    public class LocalMapExample : MonoBehaviour
    {
        /// <summary> The nr world anchor store. </summary>
        private NRWorldAnchorStore m_NRWorldAnchorStore;
        /// <summary> The anchor panel. </summary>
        public Transform m_AnchorPanel;
        /// <summary> Target for the anchor item. </summary>
        private Transform target;

        /// <summary> Dictionary of anchor prefabs. </summary>
        private Dictionary<string, GameObject> m_AnchorPrefabDict = new Dictionary<string, GameObject>();

        private MapQualityIndicator.IndicatorSettings Settings => MapQualityIndicator.Settings;

        private bool m_IsFirstMapping = true;

        /// <summary> Starts this object. </summary>
        private void Start()
        {
            //MapQualityIndicator.AddStateChangeListener(OnAnchorStateChanged);

            var anchorItems = FindObjectsOfType<AnchorItem>();
            foreach (var item in anchorItems)
            {
                item.OnAnchorItemClick += OnAnchorItemClick;
                m_AnchorPrefabDict.Add(item.key, item.gameObject);
            }
            m_AnchorPanel.gameObject.SetActive(false);
            m_NRWorldAnchorStore = new NRWorldAnchorStore();
            m_NRWorldAnchorStore.OnNotifyMessage += LocalMapExample_NotifyMessage;
            NRSessionManager.Instance.NRHMDPoseTracker.OnModeChanged += (result) =>
            {
                if (result.success)
                {
                    StartCoroutine(EnableAnchorAsync());
                    MainThreadDispather.QueueOnMainThread(() =>
                    {
                        m_NRWorldAnchorStore.DisableAllAnchors();
                    });
                }
            };
            StartCoroutine(EnableAnchorAsync());
        }

        private void OnEnable()
        {
            StartCoroutine(EnableAnchorAsync());            
        }

        private void OnDisable()
        {
            MainThreadDispather.QueueOnMainThread(() =>
            {
                m_NRWorldAnchorStore.DisableAllAnchors();
            });
        }

        private void LocalMapExample_NotifyMessage(string msg)
        {
            Toaster.Toast(msg, 8000);
        }

        /// <summary> Updates this object. </summary>
        private void Update()
        {
            if (NRInput.GetButtonDown(ControllerButton.TRIGGER) && target != null)
            {
                AddAnchor();
            }
        }

        /// <summary> Coroutine for asynchronously enabling anchor tracking. </summary>
        IEnumerator EnableAnchorAsync()
        {
            if (NRSessionManager.Instance.NRHMDPoseTracker.TrackingMode == TrackingType.Tracking6Dof)
            {
                NRDebugger.Info("[LocalMapExample] EnableAnchorAsync");
                yield return null;
                EnableAnchor();
            }
        }

        /// <summary> Enables anchor tracking. </summary>
        private void EnableAnchor()
        {
#if !UNITY_EDITOR
            NRDebugger.Info("[LocalMapExample] EnableAnchor");
            NRSessionManager.Instance.NativeAPI.Configuration.SetTrackableAnchorEnabled(true);
#endif
        }

        /// <summary> Open or close anchor panel. </summary>
        public void SwitchAnchorPanel()
        {
            m_AnchorPanel.gameObject.SetActive(!m_AnchorPanel.gameObject.activeInHierarchy);
        }

        /// <summary> Executes the 'anchor item click' action. </summary>
        /// <param name="key">        The key.</param>
        /// <param name="anchorItem"> The anchor item.</param>
        private void OnAnchorItemClick(string key, GameObject anchorItem)
        {
            // If an anchor is under creating, do not create another new one
            if (NRWorldAnchorStore.Instance.IsCreatingNewAnchor)
            {
                return;
            }

            if (target != null)
            {
                DestroyImmediate(target.gameObject);
            }

            target = Instantiate(anchorItem).transform;
            target.parent = NRInput.AnchorsHelper.GetAnchor(ControllerAnchorEnum.RightModelAnchor);
            target.position = target.parent.transform.position + target.parent.forward;
            target.forward = target.parent.forward;
            Destroy(target.gameObject.GetComponent<BoxCollider>());

            SwitchAnchorPanel();
        }

        private async Task ShowGuideDialog(NRWorldAnchor anchor)
        {
            if(anchor.TryGetComponent<AnchorItem>(out var anchorItem))
            {
                await anchorItem.ShowGuide();
            }
        }

        /// <summary> Load NRWorldAnchorStore object. </summary>
        public void Load()
        {
            if (m_NRWorldAnchorStore == null)
            {
                return;
            }
            var list = m_NRWorldAnchorStore.GetLoadableAnchorUUID();
            foreach (var item in list)
            {
                m_NRWorldAnchorStore.LoadwithUUID(item.Key, (UInt64 handle) =>
                {
                    var go = Instantiate(m_AnchorPrefabDict[item.Value]);
#if UNITY_EDITOR
                    go.transform.position = UnityEngine.Random.insideUnitSphere + Vector3.forward * 2;
#else
                    go.transform.position = Vector3.forward * 10000;
#endif
                    NRWorldAnchor anchor = go.AddComponent<NRWorldAnchor>();
                    anchor.UserDefinedKey = item.Value;
                    anchor.UUID = item.Key;
                    anchor.BindAnchor(handle);
                    go.SetActive(true);
                    NRDebugger.Info("[NRWorldAnchorStore] LoadwithUUID: {0}, UserDefinedKey: {1} Handle: {2}", item.Key, item.Value, handle);
                });
            }
        }

        public void Erase()
        {
            if (m_NRWorldAnchorStore == null)
            {
                return;
            }

            m_NRWorldAnchorStore.EraseAllAnchors();
        }

        /// <summary> Save anchors your add. </summary>
        public void Save()
        {
            if (m_NRWorldAnchorStore == null)
            {
                return;
            }
            m_NRWorldAnchorStore.SaveAllAnchors();
        }

        /// <summary> Destroy all anchors from memory. </summary>
        public void Destroy()
        {
            if (m_NRWorldAnchorStore == null)
            {
                return;
            }
            m_NRWorldAnchorStore.Destroy();

            MapQualityIndicator.InterruptMappingGuide();
        }

        /// <summary> Add a new anchor. </summary>
        public async void AddAnchor()
        {
            if (m_NRWorldAnchorStore == null || target == null)
            {
                return;
            }

            if (m_NRWorldAnchorStore.IsCreatingNewAnchor)
            {
                return;
            }

            var anchorItem = target.GetComponent<AnchorItem>();
            if (anchorItem == null)
            {
                return;
            }
            var go = Instantiate(target.gameObject);
            go.transform.position = target.position;
            go.transform.rotation = target.rotation;
            go.SetActive(true);

            string key = anchorItem.key;
            NRWorldAnchor anchor = go.AddComponent<NRWorldAnchor>();
            anchor.UserDefinedKey = key;
            bool success = anchor.CreateAnchor();

            DestroyImmediate(target.gameObject);

            if (success)
            {
                MapQualityIndicator.SetCurrentAnchor(anchor, false);

                await ShowMapQualityIndicator(anchor);
            }
            else
            {
                DestroyImmediate(go);
            }

        }

        private async Task ShowMapQualityIndicator(NRWorldAnchor anchor)
        {
            if (m_IsFirstMapping)
            {
                m_IsFirstMapping = false;
                await ShowGuideDialog(anchor);
            }
            MapQualityIndicator.ShowMappingGuide();
        }
    }

    internal class PromptTexts
    {
        public static readonly string s_ReAddPrompt = "Mapping didn't succeed.\n\n" +
            "Please re-add the anchor, taking care to view its surroundings thoroughly.\n\n" +
            "If challenges persist, consider a richer, wider environment.";
        public static readonly string s_RemapPrompt = "Mapping didn't succeed.\n\n" +
            "Please remap the anchor, taking care to view its surroundings thoroughly.\n\n" +
            "If challenges persist, consider a richer, wider environment.";
    }
}