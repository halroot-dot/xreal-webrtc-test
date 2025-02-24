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

    /// <summary> Session Native API. </summary>
    internal partial class NativeCamera : ICameraDataProvider
    {
        /// <summary> Handle of the native camera. </summary>
        private UInt64 m_NativeCameraHandle;
        private bool _IsErrorState = false;

        /// <summary> Creates a new bool. </summary>
        /// <returns> True if it succeeds, false if it fails. </returns>
        public bool Create()
        {
            _IsErrorState = false;
            var result = NativeApi.NRRgbCameraCreate(ref m_NativeCameraHandle);
            NativeErrorListener.Check(result, this, "Create", true);
            return result == NativeResult.Success;
        }

        /// <summary> Gets raw data. </summary>
        /// <param name="imageHandle"> Handle of the image.</param>
        /// <param name="camera">      The camera.</param>
        /// <param name="ptr">         [in,out] The pointer.</param>
        /// <param name="size">        [in,out] The size.</param>
        /// <returns> True if it succeeds, false if it fails. </returns>
        public bool GetRawData(UInt64 imageHandle, NativeDevice camera, ref IntPtr ptr, ref int size)
        {
            if (_IsErrorState)
            {
                return false;
            }
            uint data_size = 0;
            var result = NativeApi.NRRgbCameraImageGetRawData(m_NativeCameraHandle, imageHandle, ref ptr, ref data_size);
            size = (int)data_size;
            NativeErrorListener.Check(result, this, "GetRawData");
            return result == NativeResult.Success;
        }

        /// <summary> Gets a resolution. </summary>
        /// <param name="imageHandle"> Handle of the image.</param>
        /// <param name="camera">      The camera.</param>
        /// <returns> The resolution. </returns>
        public NativeResolution GetResolution(UInt64 imageHandle, NativeDevice camera)
        {
            NativeResolution resolution = new NativeResolution(0, 0);
            var result = NativeApi.NRRgbCameraImageGetResolution(m_NativeCameraHandle, imageHandle, ref resolution);
            NativeErrorListener.Check(result, this, "GetResolution");
            return resolution;
        }

        /// <summary> Gets hmd time nanos. </summary>
        /// <param name="imageHandle"> Handle of the image.</param>
        /// <param name="camera">      The camera.</param>
        /// <returns> The hmd time nanos. </returns>
        public UInt64 GetHMDTimeNanos(UInt64 imageHandle, NativeDevice camera)
        {
            UInt64 time = 0;
            NativeApi.NRRgbCameraImageGetHMDTimeNanos(m_NativeCameraHandle, imageHandle, ref time);
            return time;
        }

        /// <summary> Get exposure time. </summary>
        /// <param name="imageHandle"> Handle of the image. </param>
        /// <param name="camera">      The camera. </param>
        /// <returns> Exposure time of the image. </returns>
        public UInt32 GetExposureTime(UInt64 imageHandle, NativeDevice camera)
        {
            UInt32 exposureTime = 0;
            return exposureTime;
        }

        /// <summary> Get Gain. </summary>
        /// <param name="imageHandle"> Handle of the image. </param>
        /// <param name="camera">      The camera. </param>
        /// <returns> Gain of the image. </returns>
        public UInt32 GetGain(UInt64 imageHandle, NativeDevice camera)
        {
            UInt32 gain = 0;
            return gain;
        }

        /// <summary> Callback, called when the set capture. </summary>
        /// <param name="callback"> The callback.</param>
        /// <param name="userdata"> (Optional) The userdata.</param>
        /// <returns> True if it succeeds, false if it fails. </returns>
        public bool SetCaptureCallback(CameraImageCallback callback, UInt64 userdata = 0)
        {
            if (_IsErrorState)
            {
                return false;
            }
            var result = NativeApi.NRRgbCameraSetCaptureCallback(m_NativeCameraHandle, callback, userdata);
            NativeErrorListener.Check(result, this, "SetCaptureCallback");
            return result == NativeResult.Success;
        }

        /// <summary> Starts a capture. </summary>
        /// <returns> True if it succeeds, false if it fails. </returns>
        public bool StartCapture()
        {
            if (_IsErrorState)
            {
                NativeErrorListener.Check(NativeResult.RGBCameraDeviceNotFind, this, "StartCapture", true);
                return false;
            }
            var result = NativeApi.NRRgbCameraStart(m_NativeCameraHandle);
            _IsErrorState = (result != NativeResult.Success);
            NativeErrorListener.Check(result, this, "StartCapture", true);
            return result == NativeResult.Success;
        }

        /// <summary> Stops a capture. </summary>
        /// <returns> True if it succeeds, false if it fails. </returns>
        public bool StopCapture()
        {
            if (_IsErrorState)
            {
                return false;
            }
            var result = NativeApi.NRRgbCameraStop(m_NativeCameraHandle);
            NativeErrorListener.Check(result, this, "StopCapture", true);
            return result == NativeResult.Success;
        }

        /// <summary> Pause a capture. </summary>
        /// <returns> True if it succeeds, false if it fails. </returns>
        public bool PauseCapture()
        {
            NRDebugger.Info("[NativeCamera] PauseCapture skipped.");
            return true;
        }

        /// <summary> Resume a capture. </summary>
        /// <returns> True if it succeeds, false if it fails. </returns>
        public bool ResumeCapture()
        {
            NRDebugger.Info("[NativeCamera] ResumeCapture skipped.");
            return true;
        }

        /// <summary> Destroys the image described by imageHandle. </summary>
        /// <param name="imageHandle"> Handle of the image.</param>
        /// <returns> True if it succeeds, false if it fails. </returns>
        public bool DestroyImage(UInt64 imageHandle)
        {
            if (_IsErrorState)
            {
                return false;
            }
            var result = NativeApi.NRRgbCameraImageDestroy(m_NativeCameraHandle, imageHandle);
            NativeErrorListener.Check(result, this, "DestroyImage");
            return result == NativeResult.Success;
        }

        /// <summary> Releases this object. </summary>
        /// <returns> True if it succeeds, false if it fails. </returns>
        public bool Release()
        {
            _IsErrorState = false;
            var result = NativeApi.NRRgbCameraDestroy(m_NativeCameraHandle);
            NativeErrorListener.Check(result, this, "Release");
            m_NativeCameraHandle = 0;
            return result == NativeResult.Success;
        }
        public NativeVector2f ProjectPoint(NativeVector3f worldPoint)
        {
            var result = NativeApi.NRRgbCameraProjectPoint(m_NativeCameraHandle, ref worldPoint, out NativeVector2f out_ImagePoint);
            NativeErrorListener.Check(result, this, "ProjectPoint");
            return out_ImagePoint;
        }

        public NativeVector3f UnProjectPoint(NativeVector2f imagePoint)
        {
            var result = NativeApi.NRRgbCameraUnProjectPoint(m_NativeCameraHandle, ref imagePoint, out NativeVector3f out_WorldPoint);
            NativeErrorListener.Check(result, this, "ProjectPoint");
            return out_WorldPoint;
        }
        /// <summary> A native api. </summary>
        private struct NativeApi
        {
            /// <summary> Nrrgb camera image get raw data. </summary>
            /// <param name="rgb_camera_handle">       Handle of the RGB camera.</param>
            /// <param name="rgb_camera_image_handle"> Handle of the RGB camera image.</param>
            /// <param name="out_image_raw_data">      [in,out] Information describing the out image raw.</param>
            /// <param name="out_image_raw_data_size"> [in,out] Size of the out image raw data.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRgbCameraImageGetRawData(UInt64 rgb_camera_handle,
                UInt64 rgb_camera_image_handle, ref IntPtr out_image_raw_data, ref UInt32 out_image_raw_data_size);

            /// <summary> Nrrgb camera image get resolution. </summary>
            /// <param name="rgb_camera_handle">       Handle of the RGB camera.</param>
            /// <param name="rgb_camera_image_handle"> Handle of the RGB camera image.</param>
            /// <param name="out_image_resolution">    [in,out] The out image resolution.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRgbCameraImageGetResolution(UInt64 rgb_camera_handle,
                UInt64 rgb_camera_image_handle, ref NativeResolution out_image_resolution);

            /// <summary> Nrrgb camera image get hmd time nanos. </summary>
            /// <param name="rgb_camera_handle">        Handle of the RGB camera.</param>
            /// <param name="rgb_camera_image_handle">  Handle of the RGB camera image.</param>
            /// <param name="out_image_hmd_time_nanos"> [in,out] The out image hmd time nanos.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRgbCameraImageGetHMDTimeNanos(
                UInt64 rgb_camera_handle, UInt64 rgb_camera_image_handle,
                ref UInt64 out_image_hmd_time_nanos);

            /// <summary> Nrrgb camera create. </summary>
            /// <param name="out_rgb_camera_handle"> [in,out] Handle of the out RGB camera.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRgbCameraCreate(ref UInt64 out_rgb_camera_handle);

            /// <summary> Nrrgb camera destroy. </summary>
            /// <param name="rgb_camera_handle"> Handle of the RGB camera.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRgbCameraDestroy(UInt64 rgb_camera_handle);

            /// <summary> Callback, called when the nrrgb camera set capture. </summary>
            /// <param name="rgb_camera_handle"> Handle of the RGB camera.</param>
            /// <param name="image_callback">    The image callback.</param>
            /// <param name="userdata">          The userdata.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary, CallingConvention = CallingConvention.Cdecl)]
            public static extern NativeResult NRRgbCameraSetCaptureCallback(
                UInt64 rgb_camera_handle, CameraImageCallback image_callback, UInt64 userdata);

            /// <summary> Nrrgb camera start capture. </summary>
            /// <param name="rgb_camera_handle"> Handle of the RGB camera.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRgbCameraStart(UInt64 rgb_camera_handle);

            /// <summary> Nrrgb camera stop capture. </summary>
            /// <param name="rgb_camera_handle"> Handle of the RGB camera.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRgbCameraStop(UInt64 rgb_camera_handle);

            /// <summary> Nrrgb camera image destroy. </summary>
            /// <param name="rgb_camera_handle">       Handle of the RGB camera.</param>
            /// <param name="rgb_camera_image_handle"> Handle of the RGB camera image.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRgbCameraImageDestroy(UInt64 rgb_camera_handle,
                UInt64 rgb_camera_image_handle);

            /// <summary>
            /// Projects a 3d world point in the RGB camera space to a 2d pixel in the image space.
            /// </summary>
            /// <param name="rgb_camera_handle">The handle of RGB camera object.</param>
            /// <param name="world_point">The 3d world point in the RGB camera space.</param>
            /// <param name="out_image_point">The 2d pixel in the image space.</param>
            /// <returns></returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRgbCameraProjectPoint(UInt64 rgb_camera_handle,
                ref NativeVector3f world_point,
                out NativeVector2f out_image_point);

            /// <summary>
            /// Unprojects a 2d pixel in the image space to a 3d world point in homogenous coordinate.
            /// </summary>
            /// <param name="rgb_camera_handle">The handle of RGB camera object.</param>
            /// <param name="image_point">The 2d pixel in the image space.</param>
            /// <param name="out_world_point">The 3d world point in the RGB camera space.</param>
            /// <returns></returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRRgbCameraUnProjectPoint(UInt64 rgb_camera_handle,
                ref NativeVector2f image_point,
                out NativeVector3f out_world_point);
        };
    }
}
