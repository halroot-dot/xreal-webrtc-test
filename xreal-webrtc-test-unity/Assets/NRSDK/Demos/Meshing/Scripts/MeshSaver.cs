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
using System.Threading;
using UnityEngine;

namespace NRKernal.NRExamples
{
    /// <summary> A mesh info processor to save unity mesh to obj file. </summary>
    public class MeshSaver : MonoBehaviour, IMeshInfoProcessor
    {

        Dictionary<ulong, Mesh> m_MeshDict = new Dictionary<ulong, Mesh>();
        Thread m_SaveThread;

        public void Save()
        {
            if (m_SaveThread == null)
            {
                m_SaveThread = new Thread(SaveMeshThread);
                m_SaveThread.Start();
            }
        }

        void IMeshInfoProcessor.UpdateMeshInfo(ulong identifier, NRMeshInfo meshInfo)
        {
            NRMeshingBlockState meshingBlockState = meshInfo.state;
            Mesh mesh = meshInfo.baseMesh;

            NRDebugger.Debug("[MeshSaver] meshingBlockState: {0} identifier: {1}", meshingBlockState, identifier);
            lock (m_MeshDict)
            {
                m_MeshDict[identifier] = mesh;
            }
        }

        void SaveMeshThread()
        {
            Dictionary<ulong, Mesh> meshDictCopy;
            lock (m_MeshDict)
            {
                meshDictCopy = new Dictionary<ulong, Mesh>(m_MeshDict);
            }

            MeshSaveUtility.Save(meshDictCopy);
            m_SaveThread = null;
        }

        void IMeshInfoProcessor.ClearMeshInfo()
        {
            NRDebugger.Debug("[MeshSaver] ClearMeshInfo.");
            MeshSaveUtility.Clear();
        }


    }
}
