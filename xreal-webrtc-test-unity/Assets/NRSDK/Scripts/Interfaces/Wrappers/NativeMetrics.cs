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
    using System.Runtime.InteropServices;

    /// Supported features for the meshing system.
    internal enum NRMetricsFeature
    {
        NR_METRICS_FEATURE_NULL = 0,
        /// Report tearing frame count in one second. Default is disabled, be careful to enable this feature as it needs extra cpu or gpu cost.
        NR_METRICS_FEATURE_EXTENDED_TEARING_COUNT = 1,
        /// Render back color on screen. Default is disabled, be careful to enable this feature as it needs extra cpu or gpu cost.
        NR_METRICS_FEATURE_EXTENDED_RENDER_BACK_COLOR = 2,
    };
    
    /// <summary>
    /// Metrics's Native API.
    /// </summary>
    internal class NativeMetrics
    {
        private UInt64 m_MetricsHandle = 0;

        /// <summary> Create this object. </summary>
        /// <returns> True if it succeeds, false if it fails. </returns>
        public bool Create(UInt64 metricsHandle = 0)
        {
            NRDebugger.Info("[NativeMetrics] Create: metricsHandle={0}", metricsHandle);
            if (metricsHandle == 0)
            {
                NativeResult result = NativeApi.NRMetricsCreate(ref metricsHandle);
                NativeErrorListener.Check(result, this, "Create");
            }

            m_MetricsHandle = metricsHandle;
            return m_MetricsHandle != 0;
        }

        /// <summary> Start this object. </summary>
        /// <returns> True if it succeeds, false if it fails. </returns>
        public bool Start()
        {
            var result = NativeApi.NRMetricsStart(m_MetricsHandle);
            NativeErrorListener.Check(result, this, "Start");
            return result == NativeResult.Success;
        }

        /// <summary> Get the number of times the current frame has been displayed on the HMD screen. </summary>
        /// <returns> The number of times the current frame has been displayed on the HMD screen.. </returns>
        public uint GetCurrFramePresentCount()
        {
            if (m_MetricsHandle == 0)
            {
                return 0u;
            }
            uint frameCount = 1;
            var result = NativeApi.NRMetricsGetCurrFramePresentCount(m_MetricsHandle, ref frameCount);
            NativeErrorListener.Check(result, this, "GetCurrFramePresentCount");
            return frameCount;
        }

        /// <summary> Get the number of extended frame count. This indicates the number of extended frame count used when predict frame present time. </summary>
        /// <returns> The number of extended frame count. </returns>
        public uint GetExtendedFrameCount()
        {
            if (m_MetricsHandle == 0)
            {
                return 0u;
            }
            uint extraFrameCount = 0;
            var result = NativeApi.NRMetricsGetExtendedFrameCount(m_MetricsHandle, ref extraFrameCount);
            NativeErrorListener.Check(result, this, "GetExtendedFrameCount");
            return extraFrameCount;
        }

        /// <summary> Get the number of screen tears in one second. This reports when ATW takes too long and experience a screen tear. </summary>
        /// <returns> The number of screen tears in one second. </returns>
        public uint GetTearedFrameCount()
        {
            if (m_MetricsHandle == 0)
            {
                return 0u;
            }
            uint frameCount = 0;
            var result = NativeApi.NRMetricsGetTearedFrameCount(m_MetricsHandle, ref frameCount);
            NativeErrorListener.Check(result, this, "GetTearedFrameCount");
            return frameCount;
        }

        /// <summary> Get the number of early finished frames in one second. This indicates the number of frames delivered before they were needed. </summary>
        /// <returns> The number of early frames in one second. </returns>
        public uint GetEarlyFrameCount()
        {
            if (m_MetricsHandle == 0)
            {
                return 0u;
            }
            uint frameCount = 0;
            var result = NativeApi.NRMetricsGetEarlyFrameCount(m_MetricsHandle, ref frameCount);
            NativeErrorListener.Check(result, this, "GetEarlyFrameCount");
            return frameCount;
        }


        /// <summary> Get the number of dropped frames in one second. This indicates the number of times a frame wasn’t delivered on time, and the previous frame was used in ATW instead. </summary>
        /// <returns> The number of early frames in one second. </returns>
        public uint GetDroppedFrameCount()
        {
            if (m_MetricsHandle == 0)
            {
                return 0u;
            }
            uint frameCount = 0;
            var result = NativeApi.NRMetricsGetDroppedFrameCount(m_MetricsHandle, ref frameCount);
            NativeErrorListener.Check(result, this, "GetDroppedFrameCount");
            return frameCount;
        }


        /// <summary> Get the app frame latency(in nanosecond). This is the absolute time between when an app queries the pose before rendering and the time the frame is displayed on the HMD screen. </summary>
        /// <returns> The app frame latency. </returns>
        public ulong GetAppFrameLatency()
        {
            if (m_MetricsHandle == 0)
            {
                return 0uL;
            }
            ulong latency = 0;
            var result = NativeApi.NRMetricsGetAppFrameLatency(m_MetricsHandle, ref latency);
            NativeErrorListener.Check(result, this, "GetAppFrameLatency");
            return latency;
        }


        /// <summary> Get the frame count of presenting on screen per second. </summary>
        /// <returns> The fps of presenting on screen. </returns>
        public uint GetPresentFps()
        {
            if (m_MetricsHandle == 0)
            {
                return 0;
            }
            uint presentFPS = 0;
            var result = NativeApi.NRMetricsGetPresentFps(m_MetricsHandle, ref presentFPS);
            NativeErrorListener.Check(result, this, "GetPresentFps");
            return presentFPS;
        }


        /// <summary> Stop this object. </summary>
        /// <returns> True if it succeeds, false if it fails. </returns>
        public bool Stop()
        {
            if (m_MetricsHandle == 0)
            {
                return false;
            }

            NativeResult result = NativeApi.NRMetricsStop(m_MetricsHandle);
            NativeErrorListener.Check(result, this, "Stop");
            return result == NativeResult.Success;
        }


        /// <summary> Destroy this object. </summary>
        /// <returns> True if it succeeds, false if it fails. </returns>
        public bool Destroy()
        {
            if (m_MetricsHandle == 0)
            {
                return false;
            }

            NativeResult result = NativeApi.NRMetricsDestroy(m_MetricsHandle);
            NativeErrorListener.Check(result, this, "Destroy");
            m_MetricsHandle = 0;
            return result == NativeResult.Success;
        }

        /// <summary> Pauses this object. </summary>
        /// <returns> True if it succeeds, false if it fails. </returns>
        public bool Pause()
        {
            if (m_MetricsHandle == 0)
            {
                return false;
            }

            var result = NativeApi.NRMetricsPause(m_MetricsHandle);
            NativeErrorListener.Check(result, this, "Pause");
            return result == NativeResult.Success;
        }

        /// <summary> Resumes this object. </summary>
        /// <returns> True if it succeeds, false if it fails. </returns>
        public bool Resume()
        {
            if (m_MetricsHandle == 0)
            {
                return false;
            }

            var result = NativeApi.NRMetricsResume(m_MetricsHandle);
            NativeErrorListener.Check(result, this, "Resume");
            return result == NativeResult.Success;
        }

        public bool EnableFeature(NRMetricsFeature feature, bool enable)
        {
            if (m_MetricsHandle == 0)
            {
                return false;
            }

            var result = NativeApi.NRMetricsSetFeatureEnable(m_MetricsHandle, feature, enable ? 1 : 0);
            NativeErrorListener.Check(result, this, "EnableFeature");
            return result == NativeResult.Success;
            
        }

        private partial struct NativeApi
        {
            /// <summary> Create the Metrics object. </summary>
            /// <param name="out_metrics_handle"> [in,out] Handle of the metrics.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRMetricsCreate(ref UInt64 out_metrics_handle);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRMetricsStart(UInt64 metrics_handle);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRMetricsGetCurrFramePresentCount(UInt64 metrics_handle, ref uint frame_present_count);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRMetricsGetExtendedFrameCount(UInt64 metrics_handle, ref uint extended_frame_count);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRMetricsGetTearedFrameCount(UInt64 metrics_handle, ref uint teared_frame_count);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRMetricsGetEarlyFrameCount(UInt64 metrics_handle, ref uint early_frame_count);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRMetricsGetDroppedFrameCount(UInt64 metrics_handle, ref uint dropped_frame_count);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRMetricsGetPresentFps(UInt64 metrics_handle, ref uint present_fps);

            // [DllImport(NativeConstants.NRNativeLibrary)]
            // public static extern NativeResult NRMetricsGetFrameCompositeTime(UInt64 metrics_handle, ref ulong frame_composite_time);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRMetricsGetAppFrameLatency(UInt64 metrics_handle, ref ulong app_frame_latency);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRMetricsStop(UInt64 metrics_handle);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRMetricsDestroy(UInt64 metrics_handle);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRMetricsPause(UInt64 metrics_handle);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRMetricsResume(UInt64 metrics_handle);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRMetricsSetFeatureEnable( UInt64 metrics_handle, NRMetricsFeature feature, int state);

        }
    }
}
