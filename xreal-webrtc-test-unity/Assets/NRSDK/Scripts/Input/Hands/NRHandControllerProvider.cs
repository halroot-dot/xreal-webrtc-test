/****************************************************************************
* Copyright 2019 Xreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.xreal.com/         
* 
*****************************************************************************/

using AOT;
using System;
using UnityEngine;

namespace NRKernal
{
    public class NRHandControllerProvider : ControllerProviderBase
    {
        /// <summary> The native handtracking. </summary>
#if UNITY_EDITOR
        private NREmulatorHandTracking m_NativeHandTracking;
#else
        private NativeHandTracking m_NativeHandTracking;
#endif
        /// <summary> Constructor. </summary>
        /// <param name="states"> The states.</param>
        public NRHandControllerProvider(ControllerState[] states) : base(states)
        {
#if UNITY_EDITOR
            m_NativeHandTracking = new NREmulatorHandTracking();
#else
            m_NativeHandTracking = new NativeHandTracking(NRSessionManager.Instance.NativeAPI);
#endif
        }

        public override int ControllerCount { get { return 2; } }

        /// <summary> True to need recenter. </summary>
        private bool[] m_NeedRecenter = new bool[2];
        /// <summary> Update the controller. </summary>
        public override void Update()
        {
            if (m_NativeHandTracking == null)
            {
                return;
            }
            m_NativeHandTracking.Update(GetHandState(0), GetHandState(1));

            if(states != null)
            {
                for (int i = 0; i < states.Length; i++)
                {
                    UpdateControllerState(i, GetHandState(i));
                }
            }
        }

        /// <summary> Start the controller. </summary>
        public override void Start()
        {
            base.Start();
            EnableHandTracking(true);
            NRSessionManager.Instance.NRHMDPoseTracker.OnModeChanged += OnTrackingModeChanged;
        }

        public override void Resume()
        {
            base.Resume();
            EnableHandTracking(true);
            //NRSessionManager.Instance.NRHMDPoseTracker.OnModeChanged += OnTrackingModeChanged;
        }

        public override void Pause()
        {
            base.Pause();
            EnableHandTracking(false);
            //NRSessionManager.Instance.NRHMDPoseTracker.OnModeChanged -= OnTrackingModeChanged;
        }
        /// <summary> Stop the controller. </summary>
        public override void Stop()
        {
            base.Stop();

            EnableHandTracking(false);
            NRSessionManager.Instance.NRHMDPoseTracker.OnModeChanged -= OnTrackingModeChanged;
        }

        public void SetControllerState(ControllerState[] states)
        {
            this.states = states;
        }

        private void OnTrackingModeChanged(NRHMDPoseTracker.TrackingModeChangedResult result)
        {
            NRDebugger.Info("[NRHandControllerProvider] OnTrackingModeChanged: {0}", result.success);
            EnableHandTracking(true);
        }

        private void EnableHandTracking(bool enable)
        {
#if !UNITY_EDITOR
            NRSessionManager.Instance.NativeAPI.Configuration.SetHandTrackingEnabled(enable);
#endif
        }

        private HandState GetHandState(int index)
        {
            return NRInput.Hands.GetHandState(index == 0 ? HandEnum.RightHand : HandEnum.LeftHand);
        }

        private void UpdateControllerState(int index, HandState handState)
        {
            states[index].controllerType = ControllerType.CONTROLLER_TYPE_HAND;
            states[index].availableFeature = ControllerAvailableFeature.CONTROLLER_AVAILABLE_FEATURE_ROTATION | ControllerAvailableFeature.CONTROLLER_AVAILABLE_FEATURE_POSITION;
            states[index].connectionState = ControllerConnectionState.CONTROLLER_CONNECTION_STATE_CONNECTED;
            states[index].rotation = handState.pointerPose.rotation;
            states[index].position = handState.pointerPose.position;
            states[index].gyro = Vector3.zero;
            states[index].accel = Vector3.zero;
            states[index].mag = Vector3.zero;
            states[index].touchPos = Vector3.zero;
            states[index].isTouching = handState.pointerPoseValid && handState.isPinching;
            states[index].recentered = false;
            states[index].isCharging = false;
            states[index].batteryLevel = 0;

            IControllerStateParser stateParser = ControllerStateParseUtility.GetControllerStateParser(states[index].controllerType, index);
            if (stateParser != null)
            {
                stateParser.ParserControllerState(states[index]);
            }

            if (m_NeedRecenter[index])
            {
                states[index].recentered = true;
                m_NeedRecenter[index] = false;
            }
        }

        public override void Recenter(int index)
        {
            base.Recenter(index);
            if (index < ControllerCount)
            {
                m_NeedRecenter[index] = true;
            }
            
        }
    }
}
