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
    using UnityEngine;
    using System.Runtime.InteropServices;
    using System.Diagnostics;

    /// <summary> Values that represent native color spaces. </summary>
    internal enum NativeRenderFlags
    {
        /// <summary>
        /// None
        /// </summary>
        NONE = 0,

        /// <summary>
        /// The linear color space
        /// </summary>
        LINEAR = 1,
    }

    /// <summary> Values that represent frame buffer mode. </summary>
    internal enum NativeFrameBufferMode
    {
        /// <summary> An enum constant representing the unknown option. </summary>
        UnKnown = 0,
        /// <summary> An enum constant representing the two d mono option. </summary>
        Mono = 1,
        /// <summary> An enum constant representing the three d stereo option. </summary>
        Stereo = 2,
    }

    /// <summary>
    /// HMD Eye offset Native API .
    /// </summary>
    internal class NativeRenderring
    {
        private UInt64 m_RenderingHandle = 0;
        public UInt64 RenderingHandle
        {
            get
            {
                return m_RenderingHandle;
            }
        }

        public NativeRenderring()
        {
        }

        ~NativeRenderring()
        {
        }

        public bool Create(UInt64 renderHandle = 0)
        {
            NRDebugger.Info("[NativeRender] Create: renderHandle={0}", renderHandle);
            if (renderHandle == 0)
            {
                var result = NativeApi.NRRenderingCreate(ref renderHandle);
                NativeErrorListener.Check(result, this, "Create", true);

                NativeRenderFlags colorspace = QualitySettings.activeColorSpace == ColorSpace.Linear
                    ? NativeRenderFlags.LINEAR
                    : NativeRenderFlags.NONE;
                NativeApi.NRRenderingInitSetFlags(renderHandle, (UInt64)colorspace);
            }

            m_RenderingHandle = renderHandle;
#if ENABLE_NATIVE_SESSION_MANAGER && !UNITY_EDITOR
            NativeSessionManager.SetRenderingHandle(m_RenderingHandle);
#endif
            return m_RenderingHandle != 0;
        }

        public bool Start()
        {
            if (m_RenderingHandle == 0)
            {
                return false;
            }

            var result = NativeApi.NRRenderingStart(m_RenderingHandle);
            NativeErrorListener.Check(result, this, "Start", true);
            return result == NativeResult.Success;
        }

        public bool Pause()
        {
            if (m_RenderingHandle == 0)
            {
                return false;
            }

            var result = NativeApi.NRRenderingPause(m_RenderingHandle);
            NativeErrorListener.Check(result, this, "Pause", true);
            return result == NativeResult.Success;
        }

        public bool Resume()
        {
            if (m_RenderingHandle == 0)
            {
                return false;
            }

            var result = NativeApi.NRRenderingResume(m_RenderingHandle);
            NativeErrorListener.Check(result, this, "Resume", true);
            return result == NativeResult.Success;
        }

        public void GetFramePresentTime(ref UInt64 present_time)
        {
            if (m_RenderingHandle == 0)
            {
                return;
            }

            NativeApi.NRRenderingGetFramePresentTime(m_RenderingHandle, ref present_time);
        }

        public void SetPersistentProtect(bool persistentProtectMode)
        {
            NativeApi.NRRenderingSetPersistentProtect(m_RenderingHandle, persistentProtectMode ? (uint)1 : (uint)0);
        }

        public NativeFrameBufferMode NRRenderingGetFrameBufferMode()
        {
            NativeFrameBufferMode frameBufferMode = NativeFrameBufferMode.UnKnown;
            var result = NativeApi.NRRenderingGetFrameBufferMode(m_RenderingHandle, ref frameBufferMode);
            NativeErrorListener.Check(result, this, "Resume", true);
            return frameBufferMode;
        }
        
        public bool Stop()
        {
            if (m_RenderingHandle == 0)
            {
                return false;
            }

            NativeResult result = NativeApi.NRRenderingStop(m_RenderingHandle);
            NativeErrorListener.Check(result, this, "Stop", true);
            return result == NativeResult.Success;

        }

        public bool Destroy()
        {
            if (m_RenderingHandle == 0)
            {
                return false;
            }

            NativeResult result = NativeApi.NRRenderingDestroy(m_RenderingHandle);
            NativeErrorListener.Check(result, this, "Destroy", true);
            m_RenderingHandle = 0;
            return result == NativeResult.Success;
        }

        private partial struct NativeApi
        {
            #region NRRender
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRenderingCreate(ref UInt64 out_rendering_handle);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRenderingDestroy(UInt64 rendering_handle);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRenderingStart(UInt64 rendering_handle);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRenderingStop(UInt64 rendering_handle);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRenderingPause(UInt64 rendering_handle);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRenderingResume(UInt64 rendering_handle);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRenderingGetFramePresentTime(UInt64 rendering_handle, ref UInt64 frame_present_time);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRenderingInitSetFlags(UInt64 rendering_handle, UInt64 rendering_flags);
            
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRenderingSetPersistentProtect(UInt64 rendering_handle, UInt32 state);
            
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRenderingGetFrameBufferMode(UInt64 rendering_handle, ref NativeFrameBufferMode out_frame_buffer_mode);
            #endregion
        };
    }
}