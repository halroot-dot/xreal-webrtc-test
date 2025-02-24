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

    public partial class NativeAPI
    {
        private static UInt64 m_ApiHandler;
        public static UInt64 ApiHandler
        {
            get { return m_ApiHandler; }
        }

#if UNITY_ANDROID
        /// <summary> Create NRAPI on android platform. </summary>
        public static void Create(IntPtr unityActivity)
        {
            if (m_ApiHandler != 0)
                return;

            UInt64 apiHandler = 0;
            NativeResult result = NativeApi.NRAPICreate(unityActivity, ref apiHandler);
            NativeErrorListener.Check(result, m_ApiHandler, "NRAPICreate", true);
            m_ApiHandler = apiHandler;

        }
#else
        /// <summary> Create NRAPI on none-android platform. </summary>
        internal static void Create()
        {
            if (m_ApiHandler != 0)
                return;

            UInt64 apiHandler = 0;
            NativeApi.NRAPICreate(ref apiHandler);
            m_ApiHandler = apiHandler;
        }
#endif

        /// <summary>
        /// Set sdk license data
        /// </summary>
        /// <param name="license_data"> license data. </param>
        /// <returns></returns>
        internal static bool InitSetLicenseData(byte[] license_data)
        {
            if (m_ApiHandler != 0 && license_data != null && license_data.Length > 0)
            {
                Int32 data_len = license_data.Length;
                NativeResult result = NativeApi.NRAPIInitSetLicenseData(m_ApiHandler, license_data, data_len);
                NativeErrorListener.Check(result, m_ApiHandler, "NRAPIInitSetLicenseData", true);
                return result == NativeResult.Success;
            }
            return false;
        }

        /// <summary> Get the version information of SDK. </summary>
        /// <returns> The version. </returns>
        internal static string GetVersion()
        {
            NRVersion version = new NRVersion();
            NativeApi.NRGetVersion(m_ApiHandler, ref version);
            return version.ToString();
        }

        /// <summary> Start NRAPI. </summary>
        public static void Start()
        {
            if (m_ApiHandler != 0)
            {
                NativeResult result = NativeApi.NRAPIStart(m_ApiHandler);
                NativeErrorListener.Check(result, m_ApiHandler, "NRAPIStart", true);
            }
        }

        /// <summary> Stop NRAPI. </summary>
        public static void Stop()
        {
            if (m_ApiHandler != 0)
            {
                NativeResult result = NativeApi.NRAPIStop(m_ApiHandler);
                NativeErrorListener.Check(result, m_ApiHandler, "NRAPIStop", false);
            }
        }

        /// <summary> Destroy NRAPI. </summary>
        public static void Destroy()
        {
            if (m_ApiHandler != 0)
            {
                NativeResult result = NativeApi.NRAPIDestroy(m_ApiHandler);
                NativeErrorListener.Check(result, m_ApiHandler, "NRAPIDestroy", false);
                m_ApiHandler = 0;
            }
        }

        private partial struct NativeApi
        {
#if UNITY_ANDROID
            /// <summary> Create API object. </summary>
            [DllImport(NativeConstants.NRNativeLibrary)]
            internal static extern NativeResult NRAPICreate(IntPtr android_activity, ref UInt64 out_api_handle);
#else
            [DllImport(NativeConstants.NRNativeLibrary)]
            internal static extern NativeResult NRAPICreate(ref UInt64 out_api_handle);
#endif

            /// <summary>
            /// Set sdk license data.
            /// </summary>
            /// <param name="api_handle"> sdk handle. </param>
            /// <param name="license_data"> license data. </param>
            /// <param name="data_len"> license data length. </param>
            /// <returns></returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            internal static extern NativeResult NRAPIInitSetLicenseData(UInt64 api_handle, byte[] license_data, Int32 data_len);

            /// <summary> Get the version information of SDK. </summary>
            /// <param name="out_version"> [in,out] The version information as NRVersion.</param>
            /// <returns> The result of operation. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            internal static extern NativeResult NRGetVersion(UInt64 api_handle, ref NRVersion out_version);

            /// <summary> Start API object. </summary>
            /// <param name="api_handle"> The Handle of API object.</param>
            /// <returns> The result of operation. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            internal static extern NativeResult NRAPIStart(UInt64 api_handle);

            /// <summary> Stop API ojbect. </summary>
            /// <param name="api_handle"> The Handle of API object.</param>
            /// <returns> The result of operation. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            internal static extern NativeResult NRAPIStop(UInt64 api_handle);

            /// <summary>Release memory used by the API object. </summary>
            /// <param name="api_handle"> The Handle of API object.</param>
            /// <returns> The result of operation. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            internal static extern NativeResult NRAPIDestroy(UInt64 api_handle);
        };
    }
}
