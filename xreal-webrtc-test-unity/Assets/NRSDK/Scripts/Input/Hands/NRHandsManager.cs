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
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary> A manager of hand states. </summary>
    public class NRHandsManager
    {
        private readonly Dictionary<HandEnum, NRHand> m_HandsDict;
        private readonly HandState[] m_HandStates; // index 0 represents right and index 1 represents left
        private readonly OneEuroFilter[] m_OneEuroFilters;
        private bool m_Inited;

        private Vector3 m_CameraPosition = Vector3.zero;
        private Quaternion m_CameraQuaternion = Quaternion.identity;
        private float m_Weight = 0.1f;

        public Action OnHandTrackingStarted;
        public Action OnHandStatesUpdated;
        public Action OnHandTrackingStopped;

        private Vector3 pointerPosition;    //手势位置
        private Quaternion handRotation;    //手势rotation
        private Vector3 pointerDirection;   //手势朝向
        /// <summary>
        /// Returns true if the hand tracking is now running normally
        /// </summary>
        public bool IsRunning
        {
            get
            {
                if (NRInput.GetAvailableControllersCount() == 0)
                    return false;
                return NRInput.CurrentInputSourceType == InputSourceEnum.Hands;
            }
        }

        public NRHandsManager()
        {
            m_HandsDict = new Dictionary<HandEnum, NRHand>();
            m_HandStates = new HandState[2] { new HandState(HandEnum.RightHand), new HandState(HandEnum.LeftHand) };
            m_OneEuroFilters = new OneEuroFilter[2] { new OneEuroFilter(), new OneEuroFilter() };
        }

        /// <summary>
        /// Regist the left or right NRHand. There would be at most one NRHand for each hand enum
        /// </summary>
        /// <param name="hand"></param>
        internal void RegistHand(NRHand hand)
        {
            if (hand == null || hand.HandEnum == HandEnum.None)
                return;
            var handEnum = hand.HandEnum;
            if (m_HandsDict.ContainsKey(handEnum))
            {
                m_HandsDict[handEnum] = hand;
            }
            else
            {
                m_HandsDict.Add(handEnum, hand);
            }
        }

        /// <summary>
        /// UnRegist the left or right NRHand
        /// </summary>
        /// <param name="hand"></param>
        internal void UnRegistHand(NRHand hand)
        {
            if (hand == null)
                return;
            m_HandsDict.Remove(hand.HandEnum);
        }

        /// <summary>
        /// Init hand tracking with a certain service
        /// </summary>
        internal void Init()
        {
            if (m_Inited)
                return;

            NRInput.OnControllerStatesUpdated += UpdateHandTracking;
            m_Inited = true;
            NRDebugger.Info("[HandsManager] Hand Tracking Inited");
        }
        /// <summary>
        /// UnInit hand tracking with a certain service
        /// </summary>
        internal void UnInit()
        {
            if (!m_Inited)
                return;

            NRInput.OnControllerStatesUpdated -= UpdateHandTracking;
            m_Inited = false;
            NRDebugger.Info("[HandsManager] Hand Tracking UnInit");
        }

        /// <summary>
        /// Returns true if start hand tracking success
        /// </summary>
        /// <returns></returns>
        internal bool StartHandTracking(bool switchProvider = true)
        {
            if (!m_Inited)
            {
                Init();
            }
            else if (IsRunning)
            {
                NRDebugger.Info("[HandsManager] Hand Tracking is already running");
                return true;
            }

            NRDebugger.Info("[HandsManager] Hand Tracking Start: Success");
            if (switchProvider)
            {
                NRInput.SwitchControllerProvider(typeof(NRHandControllerProvider));
                OnHandTrackingStarted?.Invoke();
            }
            
            return true;
        }

        /// <summary>
        /// Returns true if stop hand tracking success
        /// </summary>
        /// <returns></returns>
        internal bool StopHandTracking()
        {
            if (!m_Inited)
            {
                NRDebugger.Info("[HandsManager] Hand Tracking Stop: Success");
                return true;
            }

            if (!IsRunning)
            {
                NRDebugger.Info("[HandsManager] Hand Tracking Stop: Success");
                return true;
            }

            NRDebugger.Info("[HandsManager] Hand Tracking Stop: Success");
            NRInput.SwitchControllerProvider(NRInput.controllerProviderType);
            ResetHandStates();
            OnHandTrackingStopped?.Invoke();
            UnInit();
            return true;
        }

        /// <summary>
        /// Get the current hand state of the left or right hand
        /// </summary>
        /// <param name="handEnum"></param>
        /// <returns></returns>
        public HandState GetHandState(HandEnum handEnum)
        {
            switch (handEnum)
            {
                case HandEnum.RightHand:
                    return m_HandStates[0];
                case HandEnum.LeftHand:
                    return m_HandStates[1];
                default:
                    break;
            }
            return null;
        }

        /// <summary>
        /// Get the left or right NRHand if it has been registered.
        /// </summary>
        /// <param name="handEnum"></param>
        /// <returns></returns>
        public NRHand GetHand(HandEnum handEnum)
        {
            NRHand hand;
            if (m_HandsDict != null && m_HandsDict.TryGetValue(handEnum, out hand))
            {
                return hand;
            }
            return null;
        }

        /// <summary>
        /// Returns true if user is now performing the systemGesture
        /// </summary>
        /// <returns></returns>
        public bool IsPerformingSystemGesture()
        {
            return IsPerformingSystemGesture(HandEnum.LeftHand) || IsPerformingSystemGesture(HandEnum.RightHand);
        }

        /// <summary>
        /// Returns true if user is now performing the systemGesture
        /// </summary>
        /// <param name="handEnum"></param>
        /// <returns></returns>
        public bool IsPerformingSystemGesture(HandEnum handEnum)
        {
            return IsPerformingSystemGesture(GetHandState(handEnum));
        }

        private void ResetHandStates()
        {
            for (int i = 0; i < m_HandStates.Length; i++)
            {
                m_HandStates[i].Reset();
            }
        }

        private void UpdateHandTracking()
        {
            //if (!IsRunning)
            //    return;
            if (!m_Inited)
                return;

            UpdateHandPointer();
            OnHandStatesUpdated?.Invoke();
        }

        private void UpdateHandPointer()
        {
            for (int i = 0; i < m_HandStates.Length; i++)
            {
                var handState = m_HandStates[i];
                if (handState == null)
                    continue;

                CalculatePointerPose(handState);
            }
        }

        private Vector3 GetNeckPosition()
        {
            if (m_CameraPosition == Vector3.zero)
            {
                m_CameraPosition = NRInput.CameraCenter.position;
            }
            else
            {
                m_CameraPosition = Vector3.Lerp(m_CameraPosition, NRInput.CameraCenter.position, m_Weight);
            }

            Vector3 neckOffset = new Vector3(0, -0.18f, 0);
            Vector3 neckPosition = m_CameraPosition +
                Vector3.Lerp(m_CameraQuaternion * neckOffset, neckOffset, 0.5f);
            return neckPosition;
        }

        private Vector3 GetShoulderPosition(bool isRight)
        {
            if (m_CameraQuaternion == Quaternion.identity)
            {
                m_CameraQuaternion = NRInput.CameraCenter.rotation;
            }
            else
            {
                m_CameraQuaternion = Quaternion.Slerp(m_CameraQuaternion, NRInput.CameraCenter.rotation, m_Weight);
            }

            Vector3 shoulderOffset = new Vector3(isRight ? 0.1f : -0.1f, 0, 0);
            Vector3 shoulderPosition = GetNeckPosition() +
                Quaternion.Euler(0, m_CameraQuaternion.eulerAngles.y, 0) * shoulderOffset;
            return shoulderPosition;
        }

        private Vector3 GetHandRayDirection(HandState handState, Vector3 pointerPosition, Vector3 handPosition)
        {
            Vector3 shoulderPosition = GetShoulderPosition(handState.handEnum == HandEnum.RightHand);

            Plane plane = new Plane(Vector3.up, shoulderPosition);
            float distance = plane.GetDistanceToPoint(pointerPosition);
            Vector3 reflectedWristPosition;

            float ref_scale = 1.35f;
            float up_range = 0.55f;
            float down_range = 0.25f;

            if (distance > 0)
            {
                if (Mathf.Abs(distance) >= up_range)
                {
                    reflectedWristPosition = pointerPosition - ref_scale * (distance * Vector3.up);
                }
                else
                {
                    float ratio = Mathf.Abs(distance) / up_range;
                    reflectedWristPosition = pointerPosition - ref_scale * ratio * (distance * Vector3.up);
                }
            }
            else
            {
                if (Mathf.Abs(distance) >= down_range)
                {
                    reflectedWristPosition = pointerPosition - ref_scale * (distance * Vector3.up);
                }
                else
                {
                    float ratio = Mathf.Abs(distance) / down_range;
                    reflectedWristPosition = pointerPosition - ref_scale * ratio * (distance * Vector3.up);
                }
            }

            Vector3 rayOrigin = Vector3.Lerp(reflectedWristPosition, shoulderPosition, 0.6f);
            Vector3 indexPosition = handState.GetJointPose(HandJointID.IndexProximal).position;

            Vector3 rootDir = (indexPosition - rayOrigin).normalized;
            Vector3 subDir = (handPosition - handState.GetJointPose(HandJointID.Wrist).position).normalized;

            return Vector3.Slerp(rootDir, subDir, 0.35f);
        }

        private void CalculatePointerPose(HandState handState)
        {
            if (handState.isTracked)
            {
                var wristPose = handState.GetJointPose(HandJointID.Wrist);
                var cameraTransform = NRInput.CameraCenter;
                handState.pointerPoseValid = Vector3.Angle(cameraTransform.forward, wristPose.forward) < 110f;

                if (handState.pointerPoseValid)
                {
                    Vector3 middleToRing = (handState.GetJointPose(HandJointID.MiddleProximal).position
                                          - handState.GetJointPose(HandJointID.RingProximal).position).normalized;  // ring2middle
                    Vector3 middleToWrist = (handState.GetJointPose(HandJointID.MiddleProximal).position
                                           - handState.GetJointPose(HandJointID.Wrist).position).normalized;  // wrist2middle
                    Vector3 middleToCenter = Vector3.Cross(middleToWrist, middleToRing).normalized;  // middle2center

                    var handPosition = handState.GetJointPose(HandJointID.MiddleProximal).position
                                       + middleToWrist * 0.02f
                                       + middleToCenter * (handState.handEnum == HandEnum.RightHand ? 0.06f : -0.06f);  // without middleToRing
                    pointerPosition = handState.GetJointPose(HandJointID.MiddleProximal).position
                                        + middleToWrist * 0.02f
                                        + middleToRing * 0.01f
                                        + middleToCenter * (handState.handEnum == HandEnum.RightHand ? 0.06f : -0.06f);

                    pointerDirection = GetHandRayDirection(handState, pointerPosition, handPosition);
                    handRotation = Quaternion.LookRotation(pointerDirection);
                    float deltaTime = Time.deltaTime;
                    float angle = Quaternion.Angle(handRotation, handState.pointerPose.rotation);
                    if(angle > 0)
                        deltaTime *= Mathf.Pow(angle, 2);
                    if (handState.currentGesture == HandGesture.Pinch)
                    {
                        if (handState.preGesture != HandGesture.Pinch)
                        {
                            handState.pinchingTimer = 0;
                            handState.lockRotation = true;
                            handState.pinchNonMovable = true;
                            m_OneEuroFilters[(int)handState.handEnum].SetEuroParam(0f, 0f,0,false);
                        }
                        
                        if (handState.lockRotation)
                        {
                            if (angle > 6.5f)//|| handState.pinchingTimer >= 0.8f)
                            {
                                handState.lockRotation = false;
                                handState.pinchNonMovable = false;
                                m_OneEuroFilters[(int)handState.handEnum].SetEuroParam(1.0f, 1.0f, 5f);
                            }
                            else if(handState.pinchNonMovable && angle > 3.0f)
                            {
                                m_OneEuroFilters[(int)handState.handEnum].SetEuroParam(0.1f, 0.4f, 2f);
                                handState.pinchNonMovable = false;
                            }
                            else if(angle < 0.5f && !handState.pinchNonMovable)
                            {
                                handState.pinchNonMovable = true;
                                m_OneEuroFilters[(int)handState.handEnum].SetEuroParam(0, 0, 0,false);
                            }
                        }
                        handState.pinchingTimer += Time.deltaTime;
                    }
                    else if(handState.preGesture == HandGesture.Pinch)
                    {
                        if (angle > 5f)
                        {
                            m_OneEuroFilters[(int)handState.handEnum].SetEuroParam(1.0f, 1.0f, 5f,false);
                        }
                        else
                        {
                            m_OneEuroFilters[(int)handState.handEnum].SetEuroParam(0, 0, 0, false);
                        }
                        handState.lockRotation = false;
                        handState.nonMovableTimer = 0;
                    }
                    else
                    {
                        if (handState.lockRotation)
                        {
                            if(angle > 4.0f)
                            {
                                handState.lockRotation = false;
                                handState.nonMovableTimer = 0;
                            }
                            else if (angle > 1.5f)
                            {
                                m_OneEuroFilters[(int)handState.handEnum].SetEuroParam(0.2f, 0.5f, 2.0f);
                            }
                            else
                            {
                                m_OneEuroFilters[(int)handState.handEnum].SetEuroParam(0.1f, 0.2f, 1.0f,false);
                            }
                        }
                        else if (Quaternion.Angle(handState.nonMovablePose.rotation, handRotation) < 2.0f)
                        {
                            handState.nonMovableTimer += Time.deltaTime;
                            if (handState.nonMovableTimer >= 0.25f)
                            {
                                m_OneEuroFilters[(int)handState.handEnum].SetEuroParam(0.15f, 0.5f, 1.0f);
                                handState.lockRotation = true;
                            }
                        }
                        else
                        {
                            handState.nonMovablePose.position = pointerPosition;
                            handState.nonMovablePose.rotation = handState.pointerPose.rotation;
                            handState.nonMovableTimer = 0;
                           m_OneEuroFilters[(int)handState.handEnum].SetEuroParam(0.2f, 1.0f, 5f);
                        }
                    }
                    m_OneEuroFilters[(int)handState.handEnum].SyncParam(deltaTime);
                    Quaternion pointerRotation = Quaternion.LookRotation(m_OneEuroFilters[(int)handState.handEnum].Step(Time.realtimeSinceStartup, pointerDirection));
                    handState.pointerPose = new Pose(pointerPosition, pointerRotation);
                }
                else if(handState.preGesture == HandGesture.Pinch)
                {
                    Vector3 middleToRing = (handState.GetJointPose(HandJointID.MiddleProximal).position
                                          - handState.GetJointPose(HandJointID.RingProximal).position).normalized;  // ring2middle
                    Vector3 middleToWrist = (handState.GetJointPose(HandJointID.MiddleProximal).position
                                           - handState.GetJointPose(HandJointID.Wrist).position).normalized;  // wrist2middle
                    Vector3 middleToCenter = Vector3.Cross(middleToWrist, middleToRing).normalized;  // middle2center

                    var handPosition = handState.GetJointPose(HandJointID.MiddleProximal).position
                                       + middleToWrist * 0.02f
                                       + middleToCenter * (handState.handEnum == HandEnum.RightHand ? 0.06f : -0.06f);  // without middleToRing
                    pointerPosition = handState.GetJointPose(HandJointID.MiddleProximal).position
                                        + middleToWrist * 0.02f
                                        + middleToRing * 0.01f
                                        + middleToCenter * (handState.handEnum == HandEnum.RightHand ? 0.06f : -0.06f);

                    pointerDirection = GetHandRayDirection(handState, pointerPosition, handPosition);
                    handRotation = Quaternion.LookRotation(pointerDirection);

                    //float angle = Quaternion.Angle(handRotation, handState.pointerPose.rotation);
                    m_OneEuroFilters[(int)handState.handEnum].SetEuroParam(1.0f, 1.0f, 5f,false);
                    Quaternion pointerRotation = Quaternion.LookRotation(m_OneEuroFilters[(int)handState.handEnum].Step(Time.realtimeSinceStartup, pointerDirection));
                    handState.pointerPose = new Pose(pointerPosition, pointerRotation);
                }
            }
            else
            {
                if (handState.pointerPoseValid && handState.preGesture == HandGesture.Pinch)
                {
                    Quaternion pointerRotation = Quaternion.LookRotation(m_OneEuroFilters[(int)handState.handEnum].Step(Time.realtimeSinceStartup, pointerDirection));
                    m_OneEuroFilters[(int)handState.handEnum].SetEuroParam(1.0f, 1.0f, 5f,true);
                    handState.pointerPose = new Pose(pointerPosition, pointerRotation);
                }
                handState.pointerPoseValid = false;
            }
        }

        private bool IsPerformingSystemGesture(HandState handState)
        {
            if (!IsRunning)
            {
                return false;
            }
            return handState.currentGesture == HandGesture.System;
        }

        public class OneEuroFilter
        {
            public float Beta = 10f;
            public float MinCutoff = 1.0f;
            public float DCutOff = 1.0f;
            (float t, Vector3 x, Vector3 dx) _prev;

            private float BetaTarget = 10;
            private float MinCutoffTarget = 1.0f;
            private float DCutOffTarget = 1.0f;
            public void SetEuroParam(float miniCutoff,float cutOff,float beta,bool lerp = true)
            {
                this.BetaTarget = beta;
                this.MinCutoffTarget = miniCutoff;
                this.DCutOffTarget = cutOff;
                if (!lerp)
                {
                    this.Beta = beta;
                    this.MinCutoff = miniCutoff;
                    this.DCutOff = cutOff;
                }
            }

            public void SyncParam(float deltaTime)
            {
                Beta = Mathf.Lerp(Beta, BetaTarget, deltaTime);
                MinCutoff = Mathf.Lerp(MinCutoff, MinCutoffTarget, deltaTime);
                DCutOff = Mathf.Lerp(DCutOff, DCutOffTarget, deltaTime);
            }

            public Vector3 Step(float t, Vector3 x)
            {
                var t_e = t - _prev.t;

                if (t_e < 1e-5f)
                    return _prev.x;

                var dx = (x - _prev.x) / t_e;
                var dx_res = Vector3.Lerp(_prev.dx, dx, Alpha(t_e, DCutOff));

                var cutoff = MinCutoff + Beta * dx_res.magnitude;
                var x_res = Vector3.Lerp(_prev.x, x, Alpha(t_e, cutoff));

                _prev = (t, x_res, dx_res);

                return x_res;
            }

            static float Alpha(float t_e, float cutoff)
            {
                var r = 2 * Mathf.PI * cutoff * t_e;
                return r / (r + 1);
            }
        }
    }
}
