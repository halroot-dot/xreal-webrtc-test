/****************************************************************************
* Copyright 2019 Xreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.xreal.com/        
* 
*****************************************************************************/
#if USING_XR_MANAGEMENT && USING_XR_SDK_XREAL
#define USING_XR_SDK
#endif

using AOT;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using static NRKernal.NRDevice;
using System.Text;

#if USING_XR_SDK
using UnityEngine.XR;
#endif
using System.Text;

namespace NRKernal
{
    public delegate void GlassesEvent(NRActionType actionType, uint actionParam1, uint actionParam2, float actionParam3);
    public delegate void GlassesEvent_KeyClick(NRClickType actionType, NRKeyType keyType);
    public delegate void GlassesEvent_KeyState(NRKeyStateData keyStateData);
    public delegate void GlassesEvent_Disconnected();
    public delegate void GlassesEvent_Volume(int volume);
    public delegate void GlassesEvent_RgbCameraPlugState(NRRgbCameraPluginState state);
    public delegate void GlassesEvent_NextECLevel(int level);
    public delegate void GlassesEvent_TemperatureState(GlassesTemperatureLevel level);
    public delegate void GlassesEvent_ReportData(UInt64 glassesHandle, UInt64 glassesActionHandle);
    /// <summary> Glasses event. </summary>
    /// <param name="eventType"> The eventtype.</param>
    public delegate void GlassesEvent_WearingState(GlassesEventType eventType);
    /// <summary> Glasses disconnect event. </summary>
    /// <param name="reason"> The reason.</param>
    public delegate void GlassesEvent_Disconnect(GlassesDisconnectReason reason);
    /// <summary> Glassed temporary level changed. </summary>
    /// <param name="level"> The level.</param>
    public delegate void GlassedTempLevelChanged(GlassesTemperatureLevel level);
    /// <summary> Brightness value changed event. </summary>
    /// <param name="value"> The value.</param>
    public delegate void GlassesEvent_Brightness(int value);
    /// <summary> Session event. </summary>
    /// <param name="status"> The eventtype.</param>
    public delegate void SessionSpecialEvent(SessionSpecialEventType status);

    public class NRDeviceSubsystemDescriptor : IntegratedSubsystemDescriptor<NRDeviceSubsystem>
    {
        public const string Name = "Subsystem.HMD";
        public override string id => Name;
    }

    public class NRDeviceSubsystem : IntegratedSubsystem<NRDeviceSubsystemDescriptor>
    {
        public static event GlassesEvent OnGlassesEvent;
        public static event GlassesEvent_KeyClick OnGlassesEvent_KeyClick;
        public static event GlassesEvent_KeyState OnGlassesEvent_KeyState;
        public static event GlassesEvent_Volume OnGlassesEvent_Volume;
        public static event GlassesEvent_RgbCameraPlugState OnGlassesEvent_RgbCameraPlugState;
        public static event GlassesEvent_NextECLevel OnGlassesEvent_NextECLevel;
        public static event GlassesEvent_TemperatureState OnGlassesEvent_TemperatureState;
        public static event GlassesEvent_WearingState OnGlassesEvent_WearingState;
        public static event GlassesEvent_Disconnect OnGlassesEvent_Disconnect;
        public static event GlassesEvent_Brightness OnGlassesEvent_Brightness;
        public static event GlassesEvent_ReportData OnGlassesEvent_Hardware;

        private NativeHMD m_NativeHMD = null;
        private NativeGlassesController m_NativeGlassesController = null;
        private Exception m_InitException = null;
        private static bool m_IsGlassesPlugOut = false;
        private static bool m_ResetStateOnNextResume = false;

        public UInt64 NativeGlassesHandler => m_NativeGlassesController.GlassesControllerHandle;
        public UInt64 NativeHMDHandler => m_NativeHMD.HmdHandle;
        public NativeHMD NativeHMD => m_NativeHMD;
        public bool IsAvailable => !m_IsGlassesPlugOut && running && m_InitException == null;

        /// <summary> The brightness minimum. </summary>
        public const int BRIGHTNESS_MIN = 0;
        /// <summary> The brightness maximum. </summary>
        private static int m_Brightness_Max = 7;
        public static int BrightnessMax => m_Brightness_Max;

#if USING_XR_SDK
        private const string k_idDisplaySubSystem = "NRSDK Display";

        private XRDisplaySubsystem m_XRDisplaySubsystem;
        public XRDisplaySubsystem XRDisplaySubsystem
        {
            get { return m_XRDisplaySubsystem; }
        }
#endif

        public NRDeviceSubsystem(NRDeviceSubsystemDescriptor descriptor) : base(descriptor)
        {
            NRDebugger.Info("[NRDeviceSubsystem] Create");
            m_NativeGlassesController = new NativeGlassesController();
            m_NativeHMD = new NativeHMD();

#if !UNITY_EDITOR
            try
            {
                m_NativeGlassesController.Create();
                m_NativeGlassesController.RegistEventCallBack(OnGlassesActionCallback);
#if USING_XR_SDK
                var targetRenderMode = NRSessionManager.Instance.NRSessionBehaviour.SessionConfig.TargetRenderMode;
                NativeXRPlugin.SetTargetRenderMode((int)targetRenderMode);

                m_XRDisplaySubsystem = NRFrame.CreateXRSubsystem<XRDisplaySubsystemDescriptor, XRDisplaySubsystem>(k_idDisplaySubSystem);
                m_NativeHMD.Create(NativeXRPlugin.GetHMDHandle());
#else
                m_NativeHMD.Create();
#endif
            }
            catch (Exception e)
            {
                m_InitException = e;
                throw e;
            }
            NRDebugger.Info("[NRDeviceSubsystem] Created");
#if ENABLE_NATIVE_SESSION_MANAGER
            NativeSessionManager.SetHMDHandle(NativeHMDHandler);
#endif
#endif
        }

        #region LifeCycle
        public override void Start()
        {
            base.Start();

            NRDebugger.Info("[NRDeviceSubsystem] Start");
#if !UNITY_EDITOR
            m_NativeGlassesController?.Start();
            NRDevice.OnSessionSpecialEvent?.Invoke(SessionSpecialEventType.GlassesStarted);
            
            int outBrightnessMax = 0;
            var result = NativeApi.NRGlassesGetDisplayBrightnessLevelCount(NativeGlassesHandler, ref outBrightnessMax);
            NativeErrorListener.Check(result, this, "NRGlassesControlGetBrightnessLevelNumber");
            if (result == NativeResult.Success)
                m_Brightness_Max  = outBrightnessMax - 1;

            NRDebugger.Info("[NRDeviceSubsystem] MaxBrightness  = {0}", m_Brightness_Max);
            
#if USING_XR_SDK
            XRDisplaySubsystem?.Start();
            NativeXRPlugin.RegistDisplaySubSystemEventCallback(DisplaySubSystemStart);
#else
            m_NativeHMD?.Start();
#endif
            NRDevice.OnSessionSpecialEvent?.Invoke(SessionSpecialEventType.HMDStarted);
#endif
            NRDebugger.Info("[NRDeviceSubsystem] Started");
        }

        [MonoPInvokeCallback(typeof(OnDisplaySubSystemStartCallback))]
        private static void DisplaySubSystemStart(bool start)
        {
            try
            {
                NRDevice.Subsystem?.OnDisplaySubSystemStart(start);
            }
            catch (Exception ex)
            {
                NRDebugger.Error("[NRDeviceSubsystem] DisplaySubSystemStart: {0}\n{1}", ex.Message, ex.StackTrace);
            }
        }

        [MonoPInvokeCallback(typeof(NRGlassesActionCallback))]
        private static void OnGlassesActionCallback(UInt64 glasses_handle, UInt64 glasses_action_handle, UInt64 user_data)
        {
            NRActionType action_type = NRActionType.ACTION_TYPE_UNKNOWN;
            uint action_param1 = 0;
            uint action_param2 = 0;
            float action_param3 = 0;
            NativeApi.NRGlassesActionGetType(glasses_handle, glasses_action_handle, ref action_type);
            NativeApi.NRGlassesActionGetParam(glasses_handle, glasses_action_handle, ref action_param1);
            NativeApi.NRGlassesActionGetParam2(glasses_handle, glasses_action_handle, ref action_param2);
            NativeApi.NRGlassesActionGetParam3(glasses_handle, glasses_action_handle, ref action_param3);
            NRDebugger.Info($"OnGlassesActionCallback: action_type={action_type}, action_param={action_param1}, action_param2={action_param2}, action_param3={action_param3}");
            MainThreadDispather.QueueOnMainThread(() =>
            {
                OnGlassesEvent?.Invoke(action_type, action_param1, action_param2, action_param3);
            });

            if (action_type == NRActionType.ACTION_TYPE_CLICK || action_type == NRActionType.ACTION_TYPE_DOUBLE_CLICK || action_type == NRActionType.ACTION_TYPE_LONG_PRESS)
            {
                MainThreadDispather.QueueOnMainThread(() =>
                {
                    NRClickType clickType = (NRClickType)action_type;
                    NRKeyType keyType = (NRKeyType)action_param1;
                    NRDebugger.Info($"OnGlassesActionCallback: clickType={clickType}, keyType={keyType}");
                    OnGlassesEvent_KeyClick?.Invoke(clickType, keyType);
                });
            }
            else if (action_type == NRActionType.ACTION_TYPE_KEY_STATE)
            {
                MainThreadDispather.QueueOnMainThread(() =>
                {
                    ulong hmd_time_nanos = 0;
                    NativeApi.NRGlassesActionGetHMDTimeNanosOnDevice(glasses_handle, glasses_action_handle, ref hmd_time_nanos);
                    NRKeyStateData keyStateData = new NRKeyStateData();
                    keyStateData.key_type = (NRKeyType)action_param1;
                    keyStateData.key_state = (NRKeyState)action_param2;
                    keyStateData.hmd_time_nanos_device = hmd_time_nanos;
                    NRDebugger.Info($"OnGlassesActionCallback: keyStateData={JsonUtility.ToJson(keyStateData)}");
                    OnGlassesEvent_KeyState?.Invoke(keyStateData);
                });
            }
            else if (action_type == NRActionType.ACTION_TYPE_PROXIMITY_WEARING_STATE)
            {
                MainThreadDispather.QueueOnMainThread(() =>
                {
                    GlassesEventType wearing_status = (GlassesEventType)action_param1;
                    if(wearing_status == GlassesEventType.PutOn || wearing_status == GlassesEventType.PutOff)
                    {
                        OnGlassesEvent_WearingState?.Invoke(wearing_status);
                        NRSessionManager.OnGlassesStateChanged?.Invoke(wearing_status);
                    }
                });
            }
            else if (action_type == NRActionType.ACTION_TYPE_INCREASE_BRIGHTNESS || action_type == NRActionType.ACTION_TYPE_DECREASE_BRIGHTNESS)
            {
                MainThreadDispather.QueueOnMainThread(() =>
                {
                    int brightness = (int)action_param1;
                    OnGlassesEvent_Brightness?.Invoke(brightness);
                });
            }
            else if (action_type == NRActionType.ACTION_TYPE_INCREASE_VOLUME || action_type == NRActionType.ACTION_TYPE_DECREASE_VOLUME)
            {
                MainThreadDispather.QueueOnMainThread(() =>
                {
                    int volume = (int)action_param1;
                    OnGlassesEvent_Volume?.Invoke(volume);
                });
            }
            else if(action_type == NRActionType.ACTION_TYPE_NEXT_EC_LEVEL)
            {
                MainThreadDispather.QueueOnMainThread(() =>
                {
                    int ecLevel = (int)action_param1;
                    NRDebugger.Info($"OnGlassesActionCallback: switch EC to {ecLevel}");
                    OnGlassesEvent_NextECLevel?.Invoke(ecLevel);
                });
            }
            else if (action_type == NRActionType.ACTION_TYPE_DISCONNECT || action_type == NRActionType.ACTION_TYPE_FORCE_QUIT)
            {
                NRDebugger.Info($"OnGlassesDisconnectEvent: m_IsGlassesPlugOut={m_IsGlassesPlugOut}, reason={action_type}");
                if (m_IsGlassesPlugOut)
                {
                    return;
                }
                if (action_type != NRActionType.ACTION_TYPE_FORCE_QUIT)
                {
                    m_IsGlassesPlugOut = true;
                }

                if(action_type == NRActionType.ACTION_TYPE_DISCONNECT)
                    OnGlassesEvent_Disconnect?.Invoke(GlassesDisconnectReason.GLASSES_DEVICE_DISCONNECT);
                else if(action_type == NRActionType.ACTION_TYPE_FORCE_QUIT)
                    OnGlassesEvent_Disconnect?.Invoke(GlassesDisconnectReason.NOTIFY_TO_QUIT_APP);
            }
            else if (action_type == NRActionType.ACTION_TYPE_RGB_CAMERA_PLUGIN_STATE)
            {
                MainThreadDispather.QueueOnMainThread(() =>
                {
                    NRRgbCameraPluginState rgbCameraPlugState = (NRRgbCameraPluginState)action_param1;
                    OnGlassesEvent_RgbCameraPlugState?.Invoke(rgbCameraPlugState);
                });
            }
            else if (action_type == NRActionType.ACTION_TYPE_TEMPERATURE_STATE)
            {
                MainThreadDispather.QueueOnMainThread(() =>
                {
                    GlassesTemperatureLevel level = (GlassesTemperatureLevel)action_param1;
                    OnGlassesEvent_TemperatureState?.Invoke(level);
                });
            }
            else if (action_type == NRActionType.ACTION_TYPE_EVENT)
            {
                MainThreadDispather.QueueOnMainThread(() =>
                {
                    OnGlassesEvent_Hardware?.Invoke(glasses_handle, glasses_action_handle);
                });
            }

            NativeApi.NRGlassesActionDestroy(glasses_handle, glasses_action_handle);
        }

        public void ResetStateOnNextResume()
        {
            m_ResetStateOnNextResume = true;
        }

        // Start of displaySubsystem is issued earlier while resuming.
        // Stop of displaySubsystem is issued earlier while pausing.
        void OnDisplaySubSystemStart(bool start)
        {
            NRDebugger.Info("[NRDeviceSubsystem] OnDisplaySubSystemStart: start={0}, running={1}", start, running);

            // Start of displaySubsystem while system is not running, meens a resuming event.
            if (start && !running)
            {
                // we do resuming of glassedControl here for XR.
                m_NativeGlassesController?.Resume();
                NRDevice.OnSessionSpecialEvent?.Invoke(SessionSpecialEventType.GlassesResumed);
            }
        }

        public override void Pause()
        {
            base.Pause();

#if !UNITY_EDITOR
#if USING_XR_SDK
            if (XRDisplaySubsystem != null && XRDisplaySubsystem.running)
            {
                NRDebugger.Warning("[NRDeviceSubsystem] Pause but XRDisplaySubsystem is running");
                // It it not necessary to issue Stop here, as it has been issued in native layer by unity engine.
                // XRDisplaySubsystem?.Stop();
            }
#else
            m_NativeHMD?.Pause();
#endif
            NRDevice.OnSessionSpecialEvent?.Invoke(SessionSpecialEventType.GlassesPrePause);
            m_NativeGlassesController?.Pause();
#endif
        }
        public override void Resume()
        {
            base.Resume();

            if (m_ResetStateOnNextResume)
            {
                m_ResetStateOnNextResume = false;
                m_IsGlassesPlugOut = false;
            }

#if !UNITY_EDITOR
#if USING_XR_SDK
            if (XRDisplaySubsystem != null && XRDisplaySubsystem.running)
            {
                NRDebugger.Warning("[NRDeviceSubsystem] Resume but XRDisplaySubsystem is not running");
                // It it not necessary to issue Start here, as it has been issued in native layer by unity engine.
                // XRDisplaySubsystem?.Start();
            }
#else
            m_NativeGlassesController?.Resume();
            NRDevice.OnSessionSpecialEvent?.Invoke(SessionSpecialEventType.GlassesResumed);

            m_NativeHMD?.Resume();
#endif
#endif
        }

        public override void Destroy()
        {
            base.Destroy();

            NRDebugger.Info("[NRDeviceSubsystem] Destroy");
#if !UNITY_EDITOR
            NRDevice.OnSessionSpecialEvent?.Invoke(SessionSpecialEventType.GlassesPreStop);
            m_NativeGlassesController?.Stop();
            m_NativeGlassesController?.Destroy();
            
#if USING_XR_SDK
            XRDisplaySubsystem?.Destroy();
            m_XRDisplaySubsystem = null;
#else
            m_NativeHMD.Stop();
            m_NativeHMD.Destroy();
#endif
            m_IsGlassesPlugOut = false;
            NRDebugger.Info("[NRDeviceSubsystem] Destroyed");
#endif
        }
        #endregion

        #region HMD
        public NRDeviceType GetDeviceType()
        {
            if (!IsAvailable)
            {
                throw new NRGlassesNotAvailbleError("Device is not available.");
            }

#if !UNITY_EDITOR
            return m_NativeHMD.GetDeviceType();
#else
            return NRDeviceType.None;
#endif
        }

        public NRDeviceCategory GetDeviceCategory()
        {
            if (!IsAvailable)
            {
                throw new NRGlassesNotAvailbleError("Device is not available.");
            }

#if !UNITY_EDITOR
            return m_NativeHMD.GetDeviceCategory();
#else
            return NRDeviceCategory.REALITY;
#endif
        }

        public bool IsFeatureSupported(NRSupportedFeature feature)
        {
            if (!IsAvailable)
            {
                throw new NRGlassesNotAvailbleError("Device is not available.");
            }

#if !UNITY_EDITOR
            return m_NativeHMD.IsFeatureSupported(feature);
#else
            return true;
#endif
        }

        /// <summary> Gets the resolution of device. </summary>
        /// <param name="eye"> device index.</param>
        /// <returns> The device resolution. </returns>
        public NativeResolution GetDeviceResolution(NativeDevice device)
        {
            if (!IsAvailable)
            {
                throw new NRGlassesNotAvailbleError("Device is not available.");
            }
#if !UNITY_EDITOR
            return m_NativeHMD.GetDeviceResolution(device);
#else
            return new NativeResolution(1920, 1080);
#endif
        }

        /// <summary> Gets device fov. </summary>
        /// <param name="eye">         The display index.</param>
        /// <param name="fov"> [in,out] The out device fov.</param>
        /// <returns> A NativeResult. </returns>
        public void GetEyeFov(NativeDevice device, ref NativeFov4f fov)
        {
            if (!IsAvailable)
            {
                throw new NRGlassesNotAvailbleError("Device is not available.");
            }
#if !UNITY_EDITOR
            fov = m_NativeHMD.GetEyeFov(device);
#else
            fov = new NativeFov4f(0, 0, 1, 1);
#endif
        }

        /// <summary> Get the intrinsic matrix of device. </summary>
        /// <returns> The device intrinsic matrix. </returns>
        public NRDistortionParams GetDeviceDistortion(NativeDevice device)
        {
            if (!IsAvailable)
            {
                throw new NRGlassesNotAvailbleError("Device is not available.");
            }
            NRDistortionParams result = new NRDistortionParams();
#if !UNITY_EDITOR
            m_NativeHMD.GetCameraDistortion(device, ref result);
#endif
            return result;
        }

        /// <summary> Get the intrinsic matrix of device. </summary>
        /// <returns> The device intrinsic matrix. </returns>
        public NativeMat3f GetDeviceIntrinsicMatrix(NativeDevice device)
        {
            if (!IsAvailable)
            {
                throw new NRGlassesNotAvailbleError("Device is not available.");
            }
            NativeMat3f result = new NativeMat3f();
#if !UNITY_EDITOR
             m_NativeHMD.GetCameraIntrinsicMatrix(device, ref result);
#endif
            return result;
        }

        /// <summary> Get the project matrix of camera in unity. </summary>
        /// <param name="result"> [out] True to result.</param>
        /// <param name="znear">  The znear.</param>
        /// <param name="zfar">   The zfar.</param>
        /// <returns> project matrix of camera. </returns>
        public EyeProjectMatrixData GetEyeProjectMatrix(out bool result, float znear, float zfar)
        {
            if (!IsAvailable)
            {
                throw new NRGlassesNotAvailbleError("Device is not available.");
            }
            result = false;
            EyeProjectMatrixData m_EyeProjectMatrix = new EyeProjectMatrixData();
#if !UNITY_EDITOR
            result = m_NativeHMD.GetProjectionMatrix(ref m_EyeProjectMatrix, znear, zfar);
#endif
            return m_EyeProjectMatrix;
        }

        /// <summary> Get the offset position between device and head. </summary>
        /// <value> The device pose from head. </value>
        public Pose GetDevicePoseFromHead(NativeDevice device)
        {
            if (!IsAvailable)
            {
                throw new NRGlassesNotAvailbleError("Device is not available.");
            }
#if !UNITY_EDITOR
            return m_NativeHMD.GetDevicePoseFromHead(device);
#else
            return Pose.identity;
#endif
        }
        #endregion

        #region brightness KeyEvent on XrealLight.


        /// <summary> Gets the brightness. </summary>
        /// <returns> The brightness. </returns>
        public int GetBrightness()
        {
            if (!IsAvailable)
            {
                return -1;
            }

#if !UNITY_EDITOR
            int brightness = -1;
            var result = NativeApi.NRGlassesGetDisplayBrightnessLevel(NativeGlassesHandler, ref brightness);
            return result == NativeResult.Success ? brightness : -1;
#else
            return 0;
#endif
        }

        /// <summary> Sets the brightness. </summary>
        /// <param name="brightness">        The brightness.</param>
        public void SetBrightness(int brightness)
        {
            if (!IsAvailable)
            {
                return;
            }

            AsyncTaskExecuter.Instance.RunAction(() =>
            {
#if !UNITY_EDITOR
                NativeApi.NRGlassesSetDisplayBrightnessLevel(NativeGlassesHandler, brightness);
#endif
            });
        }
        #endregion

        public bool SupportElectrochromic()
        {
            var deviceType = GetDeviceType();
            return !(deviceType == NRDeviceType.XrealLight
                || deviceType == NRDeviceType.XrealAir
                || deviceType == NRDeviceType.XrealAIR2);
        }

        public bool SupportColorTemperature()
        {
            var deviceType = GetDeviceType();
            return deviceType == NRDeviceType.Xreal_One
                || deviceType == NRDeviceType.Xreal_One_ProL
                || deviceType == NRDeviceType.Xreal_One_ProM;
        }

        public string GetGlassesSystemVersion()
        {
            if (!IsAvailable)
            {
                throw new NRGlassesNotAvailbleError("Device is not available.");
            }
            byte[] version_data = new byte[128];
            int out_version_size = 0;
            NativeResult result = NativeApi.NRGlassesGetGlassesSystemVersion(NativeGlassesHandler, version_data, version_data.Length, ref out_version_size);
            NativeErrorListener.Check(result, NativeGlassesHandler, "NRGlassesGetGlassesSystemVersion");
            return Encoding.UTF8.GetString(version_data, 0, out_version_size);
        }

        private struct NativeApi
        {
            /// <summary> Nr glasses control get brightness level count. </summary>
            /// <param name="glasses_control_handle"> Handle of the glasses control.</param>
            /// <param name="out_brightness_level_count"> return brightness level count.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesGetDisplayBrightnessLevelCount(UInt64 glasses_control_handle, ref int out_brightness_level_count);

            /// </summary> Nr glasses control get brightness. </summary>
            /// <param name="glasses_control_handle"> Handle of the glasses control. </param>
            /// <param name="out_brightness_level"> return brightness level. </param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesGetDisplayBrightnessLevel(UInt64 glasses_control_handle, ref int out_brightness_level);

            /// <summary> Nr glasses control set brightness. </summary>
            /// <param name="glasses_control_handle"> Handle of the glasses control.</param>
            /// <param name="brightness_level">   The brightness level.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesSetDisplayBrightnessLevel(UInt64 glasses_control_handle, int brightness_level);

            /// <summary> Get key type of key event. </summary>
            /// <param name="glasses_control_handle"> Handle of the glasses control.</param>
            /// <param name="key_event_handle">       Handle of key event.</param>
            /// <param name="out_key_event_type">     Key type retrieved.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary, CallingConvention = CallingConvention.Cdecl)]
            public static extern NativeResult NRGlassesControlKeyEventGetType(UInt64 glasses_control_handle, UInt64 key_event_handle, ref int out_key_event_type);

            /// <summary> Get key function of key event. </summary>
            /// <param name="glasses_control_handle"> Handle of the glasses control.</param>
            /// <param name="key_event_handle">       Handle of key event.</param>
            /// <param name="out_key_event_type">     Key funtion retrieved.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary, CallingConvention = CallingConvention.Cdecl)]
            public static extern NativeResult NRGlassesControlKeyEventGetFunction(UInt64 glasses_control_handle, UInt64 key_event_handle, ref int out_key_event_function);

            /// <summary> Get key parameter of key event. </summary>
            /// <param name="glasses_control_handle"> Handle of the glasses control.</param>
            /// <param name="key_event_handle">       Handle of key event.</param>
            /// <param name="out_key_event_type">     Key parameter retrieved.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary, CallingConvention = CallingConvention.Cdecl)]
            public static extern NativeResult NRGlassesControlKeyEventGetParam(UInt64 glasses_control_handle, UInt64 key_event_handle, ref int out_key_event_param);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesActionDestroy(UInt64 glasses_handle, UInt64 glasses_action_handle);

            /// @brief Get type of action.
            /// @param glasses_handle The handle of glasses object.
            /// @param glasses_action_handle The handle of glasses action object.
            /// @param[out] out_action_type The type of an action.
            /// @return The result of operation.
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesActionGetType(UInt64 glasses_handle, UInt64 glasses_action_handle, ref NRActionType out_action_type);
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesActionGetParam(UInt64 glasses_handle, UInt64 glasses_action_handle, ref uint out_action_param);
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesActionGetParam2(UInt64 glasses_handle, UInt64 glasses_action_handle, ref uint out_action_param2);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesActionGetParam3(UInt64 glasses_handle, UInt64 glasses_action_handle, ref float out_action_param3);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesActionGetHMDTimeNanos(UInt64 glasses_handle, UInt64 glasses_action_handle, ref ulong out_action_hmd_time_nanos);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesActionGetHMDTimeNanosOnDevice(UInt64 glasses_handle, UInt64 glasses_action_handle, ref ulong out_action_hmd_time_nanos_device);

            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesGetGlassesSystemVersion(UInt64 glasses_handle, byte[] version_data, Int32 version_size, ref Int32 out_version_size);
        }
    }
}