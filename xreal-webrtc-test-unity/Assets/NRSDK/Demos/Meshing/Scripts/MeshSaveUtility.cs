/****************************************************************************
* Copyright 2019 Xreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.xreal.com/        
* 
*****************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace NRKernal.NRExamples
{
    public static class MeshSaveUtility
    {
        static int m_SubFolderIndex = 0;

        static string SavePath
        {
            get
            {
                string folder;
#if UNITY_EDITOR
                folder = Directory.GetCurrentDirectory();
#else
                folder = Application.persistentDataPath;
#endif
                return Path.Combine(folder, "MeshSave");
            }
        }

        static MeshSaveUtility()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(SavePath);
            if (!directoryInfo.Exists)
            {
                directoryInfo.Create();
            }
            else
            {
                while (m_SubFolderIndex < int.MaxValue)
                {
                    string subFolder = Path.Combine(SavePath, m_SubFolderIndex.ToString());
                    directoryInfo = new DirectoryInfo(subFolder);
                    if (directoryInfo.Exists)
                        m_SubFolderIndex++;
                    else
                        break;
                }
            }

        }

        public static void Save(Dictionary<ulong, Mesh> meshDictCopy)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(SavePath, m_SubFolderIndex.ToString()));
            if (!directoryInfo.Exists)
            {
                directoryInfo.Create();
            }
            foreach (var item in meshDictCopy)
            {
                FileInfo fileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, item.Key.ToString() + ".obj"));
                StreamWriter sw = new StreamWriter(fileInfo.FullName);
                string str = MeshToString(item.Value);
                sw.Write(str);
                sw.Flush();
                sw.Close();
            }

            m_SubFolderIndex++;
        }

        public static void Save(Dictionary<ulong, NRMeshInfo> meshDictCopy)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(SavePath, m_SubFolderIndex.ToString()));
            if (!directoryInfo.Exists)
            {
                directoryInfo.Create();
            }
            foreach (var item in meshDictCopy)
            {
                FileInfo fileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, $"{item.Key}_with_label.obj"));
                StreamWriter sw = new StreamWriter(fileInfo.FullName);
                string str = MeshInfoToString(item.Value);
                sw.Write(str);
                sw.Flush();
                sw.Close();
            }

            m_SubFolderIndex++;
        }

        public static void Clear()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(SavePath);
            if (directoryInfo.Exists)
            {
                foreach (var file in directoryInfo.EnumerateFiles("*.obj"))
                {
                    file.Delete();
                }
            }
        }
        public static string MeshToString(Mesh mesh)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("# \n");

            Vector3[] vertices = mesh.vertices;
            foreach (Vector3 v in vertices)
            {
                sb.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, v.z));
            }
            sb.Append("\n");

            Vector3[] normals = mesh.normals;
            foreach (Vector3 vn in normals)
            {
                sb.Append(string.Format("vn {0} {1} {2}\n", vn.x, vn.y, vn.z));
            }
            sb.Append("\n");

            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                    triangles[i] + 1, triangles[i + 1] + 1, triangles[i + 2] + 1));
            }
            sb.Append("\n");
            return sb.ToString();
        }

        public static Mesh StringToMesh(string data)
        {
            var lines = data.Split('\n');
            List<Vector3> verticeList = new List<Vector3>();
            List<Vector3> normalList = new List<Vector3>();
            List<int> triangleList = new List<int>();

            foreach (var line in lines)
            {
                var nums = line.Split(' ', '/');
                switch (nums[0])
                {
                    case "v":
                        verticeList.Add(new Vector3(float.Parse(nums[1]), float.Parse(nums[2]), float.Parse(nums[3])));
                        break;
                    case "vn":
                        normalList.Add(new Vector3(float.Parse(nums[1]), float.Parse(nums[2]), float.Parse(nums[3])));
                        break;
                    case "f":
                        triangleList.Add(int.Parse(nums[1]) - 1);
                        triangleList.Add(int.Parse(nums[4]) - 1);
                        triangleList.Add(int.Parse(nums[7]) - 1);
                        break;
                    default:
                        break;
                }
            }

            return new Mesh
            {
                vertices = verticeList.ToArray(),
                normals = normalList.ToArray(),
                triangles = triangleList.ToArray()
            };
        }

        public static string MeshInfoToString(NRMeshInfo meshInfo)
        {
            Mesh mesh = meshInfo.baseMesh;
            NRMeshingVertexSemanticLabel[] labels = meshInfo.labels;
            StringBuilder sb = new StringBuilder();
            sb.Append("# \n");
            sb.Append($"#id {meshInfo.identifier}\n");
            sb.Append($"#state {meshInfo.state}\n");

            Vector3[] vertices = mesh.vertices;
            int index = 0;
            foreach (Vector3 v in vertices)
            {
                sb.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, v.z));
                sb.Append($"#l {labels[index++]}\n");
            }
            sb.Append("\n");

            Vector3[] normals = mesh.normals;
            foreach (Vector3 vn in normals)
            {
                sb.Append(string.Format("vn {0} {1} {2}\n", vn.x, vn.y, vn.z));
            }
            sb.Append("\n");

            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                    triangles[i] + 1, triangles[i + 1] + 1, triangles[i + 2] + 1));
            }
            sb.Append("\n");
            return sb.ToString();
        }
        public static NRMeshInfo StringToMeshInfo(string data)
        {
            NRMeshInfo info = new NRMeshInfo();

            var lines = data.Split('\n');
            List<Vector3> verticeList = new List<Vector3>();
            List<Vector3> normalList = new List<Vector3>();
            List<NRMeshingVertexSemanticLabel> labelList = new List<NRMeshingVertexSemanticLabel>();
            List<int> triangleList = new List<int>();

            foreach (var line in lines)
            {
                var nums = line.Split(' ', '/');
                switch (nums[0])
                {
                    case "v":
                        verticeList.Add(new Vector3(float.Parse(nums[1]), float.Parse(nums[2]), float.Parse(nums[3])));
                        break;
                    case "vn":
                        normalList.Add(new Vector3(float.Parse(nums[1]), float.Parse(nums[2]), float.Parse(nums[3])));
                        break;
                    case "f":
                        triangleList.Add(int.Parse(nums[1]) - 1);
                        triangleList.Add(int.Parse(nums[4]) - 1);
                        triangleList.Add(int.Parse(nums[7]) - 1);
                        break;
                    case "#id":
                        info.identifier = ulong.Parse(nums[1]);
                        break;
                    case "#state":
                        info.state =  (NRMeshingBlockState)Enum.Parse(typeof(NRMeshingBlockState), nums[1]);
                        break;
                    case "#l":
                        var label = (NRMeshingVertexSemanticLabel)Enum.Parse(typeof(NRMeshingVertexSemanticLabel), nums[1]);
                        labelList.Add(label);
                        break;
                    default:
                        break;
                }
            }

            info.baseMesh= new Mesh
            {
                name = $"{info.identifier}",
                vertices = verticeList.ToArray(),
                normals = normalList.ToArray(),
                triangles = triangleList.ToArray()
            };
            info.labels = labelList.ToArray();

            return info;
            
        }
    }
}
