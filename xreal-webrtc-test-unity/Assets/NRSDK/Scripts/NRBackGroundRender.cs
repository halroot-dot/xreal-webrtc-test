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
    using UnityEngine;

    [RequireComponent(typeof(Camera))]
    public class NRBackGroundRender : MonoBehaviour
    {
        /// <summary> A material used to render the AR background image. </summary>
        [Tooltip("A material used to render the AR background image.")]
        [SerializeField] Material m_Material;

        private Camera m_Camera;
        private MeshRenderer m_Renderer;
        private MeshFilter m_MeshFilter;
        private Mesh m_PlaneMesh;
        private Vector3[] m_Corners;
        private Vector3 m_MeshScale;

        private int[] Triangles = new int[6] {
            0,1,2,0,2,3
        };

        private Vector2[] UV = new Vector2[4] {
            new Vector2(0,0),
            new Vector2(0,1),
            new Vector2(1,1),
            new Vector2(1,0),
        };

        private void OnEnable()
        {
            m_Camera = GetComponent<Camera>();
            EnableARBackgroundRendering();
        }

        private void OnDisable()
        {
            DisableARBackgroundRendering();
        }

        private void UpdateBackGroundMesh()
        {
            if (m_Corners == null)
            {
                m_Corners = new Vector3[4];
            }

            m_Camera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), 1, Camera.MonoOrStereoscopicEye.Mono, m_Corners);
            m_MeshScale = m_Corners[2] - m_Corners[0];
            m_MeshScale.z = 1;
            for (int i = 0; i < m_Corners.Length; i++)
            {
                m_Corners[i] = m_Camera.transform.TransformPoint(m_Corners[i] * (m_Camera.farClipPlane - 100));
            }

            Vector3 center = (m_Corners[0] + m_Corners[2]) * 0.5f;
            DrawBackGroundMesh(new Pose(center, m_Camera.transform.rotation));
        }

        public void EnableARBackgroundRendering(bool updatemesh = true)
        {
            if (updatemesh)
            {
                UpdateBackGroundMesh();
            }
            m_Renderer.gameObject.SetActive(true);
        }

        public void DisableARBackgroundRendering()
        {
            if (m_Renderer != null)
            {
                m_Renderer.gameObject.SetActive(false);
            }
        }

        public void SetMaterial(Material mat)
        {
            m_Material = mat;

            if (m_Renderer != null)
            {
                m_Renderer.material = m_Material;
            }
        }

        public void SetMesh(Mesh mesh)
        {
            m_PlaneMesh = mesh;
            if (m_MeshFilter != null)
            {
                m_MeshFilter.mesh = m_PlaneMesh;
            }
        }

        /// <summary> Draw from center. </summary>
        /// <param name="centerPose"> The center pose.</param>
        private void DrawBackGroundMesh(Pose centerPose)
        {
            if (m_PlaneMesh == null)
            {
                m_PlaneMesh = new Mesh();
                Vector3[] vertices3D = new Vector3[4];
                vertices3D[0] = new Vector3(-0.5f, -0.5f);
                vertices3D[1] = new Vector3(-0.5f, 0.5f);
                vertices3D[2] = new Vector3(0.5f, 0.5f);
                vertices3D[3] = new Vector3(0.5f, -0.5f);
                m_PlaneMesh.vertices = vertices3D;
                m_PlaneMesh.triangles = Triangles;
                m_PlaneMesh.uv = UV;
            }

            if (m_Renderer == null)
            {
                var go = new GameObject("background");
                go.transform.SetParent(transform);
                m_Renderer = go.AddComponent<MeshRenderer>();
                m_MeshFilter = go.AddComponent<MeshFilter>();
            }

            m_Renderer.transform.position = centerPose.position;
            m_Renderer.transform.rotation = centerPose.rotation;

            float distance = Vector3.Distance(m_Renderer.transform.position, m_Camera.transform.position);
            m_Renderer.transform.localScale = distance * m_MeshScale;

            m_MeshFilter.mesh = m_PlaneMesh;
            m_Renderer.material = m_Material;
        }
    }
}
