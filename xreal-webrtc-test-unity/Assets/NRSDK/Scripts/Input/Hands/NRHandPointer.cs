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
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary> A hand pointer. </summary>
    public class NRHandPointer : MonoBehaviour
    {
        /// <summary> The handEnum. </summary>
        public HandEnum handEnum;
        /// <summary> The raycaster. </summary>
        public NRPointerRaycaster raycaster;

        /// <summary> The default hand enum. </summary>
        private ControllerHandEnum m_controllerHandEnum;
        /// <summary> True if is enabled, false if not. </summary>
        private bool m_PointerEnabled;

        //private Quaternion mRecenterOffset = Quaternion.identity;
        private float mVerizontalAngle = 0;
        private float mHorizontalAngle = 0;
        /// <summary> Awakes this object. </summary>
        private void Awake()
        {
            m_controllerHandEnum = handEnum == HandEnum.RightHand ? ControllerHandEnum.Right : ControllerHandEnum.Left;
            raycaster.RelatedHand = m_controllerHandEnum;
        }

        /// <summary> Executes the 'enable' action. </summary>
        private void OnEnable()
        {
            NRInput.OnControllerRecentering += OnRecentering;
            NRInput.OnControllerStatesUpdated += OnControllerStatesUpdated;
        }

        /// <summary> Executes the 'disable' action. </summary>
        private void OnDisable()
        {
            NRInput.OnControllerRecentering -= OnRecentering;
            NRInput.OnControllerStatesUpdated -= OnControllerStatesUpdated;
        }

        /// <summary> Executes the 'controller states updated' action. </summary>
        private void OnControllerStatesUpdated()
        {
            UpdateTracker();
        }

        /// <summary> Updates the tracker. </summary>
        private void UpdateTracker()
        {
            var handState = NRInput.Hands.GetHandState(handEnum);
            m_PointerEnabled = NRInput.RaycastersActive && NRInput.RaycastMode == RaycastModeEnum.Laser && NRInput.Hands.IsRunning && handState.pointerPoseValid;
            raycaster.gameObject.SetActive(m_PointerEnabled);
            if (m_PointerEnabled)
            {
                TrackPose();
            }
        }

        /// <summary> Track pose. </summary>
        private void TrackPose()
        {
            transform.position = NRInput.GetPosition(m_controllerHandEnum);
            //transform.localRotation =  NRInput.GetRotation(m_controllerHandEnum) * mRecenterOffset;
            Quaternion xRotation = Quaternion.AngleAxis(mVerizontalAngle, NRInput.GetRotation(m_controllerHandEnum) * Vector3.right);
            Quaternion yRotation = Quaternion.AngleAxis(mHorizontalAngle, NRInput.GetRotation(m_controllerHandEnum) * Vector3.up);
            Quaternion rotation = xRotation * NRInput.GetRotation(m_controllerHandEnum);
            transform.localRotation = yRotation * rotation;

            //Quaternion rotation = Quaternion.AngleAxis(mVerizontalAngle, NRInput.Hands.GetHandState((HandEnum)m_controllerHandEnum).GetJointPose(HandJointID.Palm).rotation * Vector3.right) * NRInput.GetRotation(m_controllerHandEnum);
            //transform.localRotation = Quaternion.AngleAxis(mHorizontalAngle, NRInput.Hands.GetHandState((HandEnum)m_controllerHandEnum).GetJointPose(HandJointID.Palm).rotation * Vector3.forward) * rotation;
        }
        
        /// <summary> Executes the 'recentering' action. </summary>
        private void OnRecentering(ControllerHandEnum handEnum) 
        {
            if (NRInput.CurrentInputSourceType != InputSourceEnum.Hands)
            {
                Debug.Log("[NRHand] recenter return");
                return;
            }
            if(m_controllerHandEnum == handEnum)
            {
                //var headTopPose = NRFrame.HeadPose.position + NRFrame.HeadPose.rotation * Vector3.forward * 4;
                //Quaternion topToHand = Quaternion.LookRotation((headTopPose - NRInput.GetPosition(m_controllerHandEnum)).normalized, NRInput.GetRotation(m_controllerHandEnum) * Vector3.up);
                //Quaternion offset =  topToHand * Quaternion.Inverse(NRInput.GetRotation(m_controllerHandEnum));

                //mRecenterOffset = offset;

                Plane headVerticalPlane = new Plane(NRFrame.HeadPose.right, NRFrame.HeadPose.position);
                var projectionOrigPointer = headVerticalPlane.ClosestPointOnPlane(NRInput.GetPosition(m_controllerHandEnum) + NRInput.GetRotation(m_controllerHandEnum) * Vector3.forward * 3.5f);
                Vector3 formDir = NRInput.GetRotation(m_controllerHandEnum) * Vector3.forward;
                Vector3 toDir = projectionOrigPointer - NRInput.GetPosition(m_controllerHandEnum);
                float horizontalAngle = Vector3.Angle(formDir, toDir);
                Vector3 cross = Vector3.Cross(formDir, toDir);
                if (Vector3.Dot(cross, NRFrame.HeadPose.up) >= 0)
                {
                    mHorizontalAngle = horizontalAngle;
                    //mYawOffset = Quaternion.AngleAxis(horizontalAngle, Vector3.up);
                }
                else
                {
                    mHorizontalAngle = -horizontalAngle;
                    //mYawOffset = Quaternion.AngleAxis(-horizontalAngle, Vector3.up);
                }

                Plane headHorizontalPlane = new Plane(NRFrame.HeadPose.up, NRFrame.HeadPose.position);
                projectionOrigPointer = headHorizontalPlane.ClosestPointOnPlane(NRInput.GetPosition(m_controllerHandEnum) + NRInput.GetRotation(m_controllerHandEnum) * Vector3.forward * 3.5f);
                toDir = projectionOrigPointer - NRInput.GetPosition(m_controllerHandEnum);
                float verizontalAngle = Vector3.Angle(formDir, toDir);
                cross = Vector3.Cross(formDir, toDir);
                if (Vector3.Dot(cross, NRFrame.HeadPose.right) >= 0)
                {
                    mVerizontalAngle = verizontalAngle;
                    //mPitchOffset = Quaternion.AngleAxis(verizontalAngle, Vector3.right);
                }
                else
                {
                    mVerizontalAngle = -verizontalAngle;
                    //mPitchOffset = Quaternion.AngleAxis(-verizontalAngle, Vector3.right);
                }
                //NRDebugger.Error("[NRHandPointer] headTopPose ={0} topToHand ={1} handPos={2}  handRot ={3} end ={4}", headTopPose, topToHand.eulerAngles, NRInput.GetPosition(m_controllerHandEnum), NRInput.GetRotation(m_controllerHandEnum).eulerAngles,
                //   (NRInput.GetRotation(m_controllerHandEnum) * mPitchOffset).eulerAngles);
            }
        }
    }

}