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


    /// <summary>
    /// The callback method type which will be called when plug off xreal glasses. </summary>
    /// <param name="glasses_control_handle"> glasses_control_handle The handle of GlassesControl.</param>
    /// <param name="user_data">              The custom user data.</param>
    public delegate void NRGlassesControlPlugOffCallback(UInt64 glasses_control_handle, UInt64 user_data);


    public delegate void NRGlassesActionCallback(UInt64 glasses_handle, UInt64 glasses_action_handle, UInt64 user_data);

    /// <summary> A controller for handling native glasses. </summary>
    public partial class NativeGlassesController
    {
        /// <summary> Handle of the glasses controller. </summary>
        private UInt64 m_GlassesControllerHandle = 0;
        /// <summary> Gets the handle of the glasses controller. </summary>
        /// <value> The glasses controller handle. </value>
        public UInt64 GlassesControllerHandle
        {
            get
            {
                return m_GlassesControllerHandle;
            }
        }

        /// <summary> Creates this object. </summary>
        public void Create()
        {
            NativeResult result = NativeApi.NRGlassesCreate(ref m_GlassesControllerHandle);
            NativeErrorListener.Check(result, this, "Create", true);
        }

        /// <summary> Back, called when the regist glasses plug out. </summary>
        /// <param name="callback"> The callback.</param>
        /// <param name="userdata"> The userdata.</param>
        public void RegisGlassesPlugOutCallBack(NRGlassesControlPlugOffCallback callback, ulong userdata)
        {
            NativeResult result = NativeApi.NRGlassesControlSetGlassesDisconnectedCallback(m_GlassesControllerHandle, callback, userdata);
            NativeErrorListener.Check(result, this, "RegisGlassesPlugOutCallBack");
        }

        public void RegistEventCallBack(NRGlassesActionCallback callback)
        {
            NativeResult result = NativeApi.NRGlassesInitSetActionCallback(m_GlassesControllerHandle, callback, 0);
            NativeErrorListener.Check(result, this, "NRGlassesInitSetActionCallback");
        }

        /// <summary> Starts this object. </summary>
        public void Start()
        {
            NativeResult result = NativeApi.NRGlassesStart(m_GlassesControllerHandle);
            NativeErrorListener.Check(result, this, "Start", true);
        }

        /// <summary> Pauses this object. </summary>
        public void Pause()
        {
            NativeResult result = NativeApi.NRGlassesPause(m_GlassesControllerHandle);
            NativeErrorListener.Check(result, this, "Pause", true);
        }

        /// <summary> Resumes this object. </summary>
        public void Resume()
        {
            NativeResult result = NativeApi.NRGlassesResume(m_GlassesControllerHandle);
            NativeErrorListener.Check(result, this, "Resume", true);
        }

        /// <summary> Stops this object. </summary>
        public void Stop()
        {
            NativeResult result = NativeApi.NRGlassesStop(m_GlassesControllerHandle);
            NativeErrorListener.Check(result, this, "Stop");
        }

        /// <summary> Destroys this object. </summary>
        public void Destroy()
        {
            NativeResult result = NativeApi.NRGlassesDestroy(m_GlassesControllerHandle);
            NativeErrorListener.Check(result, this, "Destroy");
            m_GlassesControllerHandle = 0;
        }

        private partial struct NativeApi
        {
            /// <summary> Create the GlassesControl object. </summary>
            /// <param name="out_glasses_control_handle"> [in,out] The handle of GlassesControl.</param>
            /// <returns> The result of operation. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesCreate(ref UInt64 out_glasses_control_handle);

            /// <summary> Start the GlassesControl system. </summary>
            /// <param name="glasses_control_handle"> The handle of GlassesControl.</param>
            /// <returns> The result of operation. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesStart(UInt64 glasses_control_handle);

            /// <summary> Pause the GlassesControl system. </summary>
            /// <param name="glasses_control_handle"> The handle of GlassesControl.</param>
            /// <returns> The result of operation. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesPause(UInt64 glasses_control_handle);

            /// <summary> Resume the GlassesControl system. </summary>
            /// <param name="glasses_control_handle"> The handle of GlassesControl.</param>
            /// <returns> The result of operation. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesResume(UInt64 glasses_control_handle);

            /// <summary> Stop the GlassesControl system. </summary>
            /// <param name="glasses_control_handle"> The handle of GlassesControl.</param>
            /// <returns> The result of operation. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesStop(UInt64 glasses_control_handle);

            /// <summary> Release memory used by the GlassesControl. </summary>
            /// <param name="glasses_control_handle"> The handle of GlassesControl.</param>
            /// <returns> The result of operation. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesDestroy(UInt64 glasses_control_handle);


            /// <summary> Set the callback method when plug off the glasses. </summary>
            /// <param name="glasses_control_handle"> The handle of GlassesControl.</param>
            /// <param name="data_callback">          The callback method.</param>
            /// <param name="user_data">              The data which will be returned when callback is
            ///                                       triggered.</param>
            /// <returns> The result of operation. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesControlSetGlassesDisconnectedCallback(
                    UInt64 glasses_control_handle, NRGlassesControlPlugOffCallback data_callback, UInt64 user_data);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesInitSetActionCallback(UInt64 glasses_control_handle, NRGlassesActionCallback action_callback, UInt64 user_data);

        }
    }
}
