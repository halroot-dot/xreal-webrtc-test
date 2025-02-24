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
    /// <summary> Holds information about Xreal SDK version info. </summary>
    public class NRVersionInfo
    {
        private static readonly string sUnityPackageVersion = "20250102111800";

        /// <summary> Gets the version. </summary>
        /// <returns> The version. </returns>
        public static string GetVersion()
        {
#if UNITY_EDITOR
            return "2.4.0";
#else
            return NativeAPI.GetVersion();
#endif
        }

        public static string GetNRSDKPackageVersion()
        {
            return sUnityPackageVersion;
        }
    }
}
