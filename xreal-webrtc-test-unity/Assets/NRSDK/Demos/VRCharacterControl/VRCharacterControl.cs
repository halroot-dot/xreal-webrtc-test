/****************************************************************************
 * Copyright 2019 Xreal Techonology Limited. All rights reserved.
 *
 * This file is part of NRSDK.
 *
 * https://www.xreal.com/
 *
 *****************************************************************************/

using UnityEngine;

namespace NRKernal.NRExamples
{
    public class VRCharacterControl : MonoBehaviour
    {
        public float moveSpeed;
        private Transform m_CenterCamera;
        private Transform CenterCamera
        {
            get
            {
                if (m_CenterCamera == null)
                {
                    if (NRSessionManager.Instance.CenterCameraAnchor != null)
                    {
                        m_CenterCamera = NRSessionManager.Instance.CenterCameraAnchor;
                    }
                    else if (Camera.main != null)
                    {
                        m_CenterCamera = Camera.main.transform;
                    }
                }
                return m_CenterCamera;
            }
        }
        
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void Move(int dir)
        {
            Vector3 moveDir = Vector3.zero;
            switch (dir)
            {
                case 1:
                    moveDir = Vector3.left;
                    break;
                case 2:
                    moveDir = Vector3.right;
                    break;
                case 3:
                    moveDir = Vector3.forward;
                    break;
                case 4:
                    moveDir = Vector3.back;
                    break;
            }

            var worldDir = CenterCamera.TransformDirection(moveDir);
            transform.position += worldDir * moveSpeed;
        }
    }
}