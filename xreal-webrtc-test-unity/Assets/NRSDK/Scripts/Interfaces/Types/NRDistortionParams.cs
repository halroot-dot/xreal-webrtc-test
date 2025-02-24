/****************************************************************************
* Copyright 2019 Xreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.xreal.com/                  
* 
*****************************************************************************/

using System.Runtime.InteropServices;
using System.Text;

namespace NRKernal
{
    /// <summary> Values that represent nr camera models. </summary>
    public enum NRCameraModel
    {
        /// <summary> An enum constant representing the nr camera model radial option. </summary>
        NR_CAMERA_MODEL_RADIAL = 1,
        /// <summary> An enum constant representing the nr camera model fisheye option. </summary>
        NR_CAMERA_MODEL_FISHEYE = 2,
        /// <summary> An enum constant representing the nr camera model fisheye_rttp option. </summary>
        NR_CAMERA_MODEL_FISHEYE_RTTP = 3,
    }

    /// <summary>
    /// Camera distortion parameters
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NRDistortionParams
    {
        /// <summary> The camera model. </summary>
        [MarshalAs(UnmanagedType.I4)]
        public NRCameraModel cameraModel;
        /// <summary> The first distort parameters. </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public float[] distortParams;

        /// <summary> Convert this object into a string representation. </summary>
        /// <returns> A string that represents this object. </returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"cameraModel:{cameraModel} ");
            if (distortParams != null)
            {
                for (int i = 0; i < distortParams.Length; ++i)
                {
                    sb.Append($"distortParams{i}:{distortParams[i]} ");
                }
            }
            else
            {
                sb.Append("distortParams:null");
            }
            return sb.ToString();
        }
    }
}
