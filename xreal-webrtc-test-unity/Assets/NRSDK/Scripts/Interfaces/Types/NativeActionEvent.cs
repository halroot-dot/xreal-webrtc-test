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

    public enum NRKeyType
    {
        NR_KEY_TYPE_UNKNOWN = 0,
        NR_KEY_TYPE_SELECT = 1,
        NR_KEY_TYPE_INCREASE = 2,
        NR_KEY_TYPE_DECREASE = 3,
        NR_KEY_TYPE_MENU = 4,
        NR_KEY_TYPE_ALL = 1000,
    }

    public enum NRActionType
    {
        ACTION_TYPE_UNKNOWN = 0,
        ACTION_TYPE_CLICK = 1,
        ACTION_TYPE_DOUBLE_CLICK = 2,
        ACTION_TYPE_LONG_PRESS = 3,
        ACTION_TYPE_OPEN_SCREEN = 4,
        ACTION_TYPE_CLOSE_SCREEN = 5,
        ACTION_TYPE_INCREASE_BRIGHTNESS = 6,
        ACTION_TYPE_DECREASE_BRIGHTNESS = 7,
        ACTION_TYPE_INCREASE_VOLUME = 8,
        ACTION_TYPE_DECREASE_VOLUME = 9,
        ACTION_TYPE_SWITCH_TO_MONO = 10,
        ACTION_TYPE_SWITCH_TO_STEORO = 11,
        ACTION_TYPE_NEXT_EC_LEVEL = 12,
        ACTION_TYPE_SWITCH_TO_DP_VOICE = 13,
        ACTION_TYPE_SWITCH_TO_UVC_VOICE = 14,
        ACTION_TYPE_RESERVED0 = 15,
        ACTION_TYPE_RESERVED1 = 16,
        ACTION_TYPE_RESERVED2 = 17,
        ACTION_TYPE_RESERVED3 = 18,
        ACTION_TYPE_RESERVED4 = 19,
        ACTION_TYPE_SWITCH_SLEEP_TIME_LEVEL = 30,
        ACTION_TYPE_SWITCH_DISPLAY_COLOR_CALIBRATION = 31,
        ACTION_TYPE_STARTUP_STATE = 32,
        ACTION_TYPE_TRIGGER_SWITCH_SPACE_MODE = 33,
        ACTION_TYPE_TRIGGER_RECENTER = 34,
        ACTION_TYPE_TRIGGER_OSD_MAIN_MENU = 35,
        ACTION_TYPE_TRIGGER_TAKE_PHOTO = 36,
        ACTION_TYPE_TRIGGER_TAKE_VIDEO = 37,
        ACTION_TYPE_RESERVED5 = 1000,
        ACTION_TYPE_SCREEN_STATUS_NOTIFY = 1010,
        ACTION_TYPE_DISCONNECT = 2000,
        ACTION_TYPE_FORCE_QUIT = 2001,
        ACTION_TYPE_EVENT = 2002,
        ACTION_TYPE_SYSTEM_DISPLAY_CHANGE = 2003,
        ACTION_TYPE_AUDIO_ALGORITHM_CHANGE = 2020,
        ACTION_TYPE_DISPLAY_STATE = 2021,
        ACTION_TYPE_DP_WORKING_STATE = 2022,
        ACTION_TYPE_KEY_STATE = 2023,
        ACTION_TYPE_PROXIMITY_WEARING_STATE = 2024,
        ACTION_TYPE_RGB_CAMERA_PLUGIN_STATE = 2025,
        ACTION_TYPE_TEMPERATURE_DATA = 2026,
        ACTION_TYPE_POWER_SAVE_STATE = 2027,
        ACTION_TYPE_TEMPERATURE_STATE = 2028,
    }

    public enum NRClickType
    {
        CLICK = NRActionType.ACTION_TYPE_CLICK,
        DOUBLE_CLICK = NRActionType.ACTION_TYPE_DOUBLE_CLICK,
        LONG_PRESS = NRActionType.ACTION_TYPE_LONG_PRESS,
    }
    public enum NRKeyState
    {
        NR_KEY_STATE_UNKNOWN = 0,
        NR_KEY_STATE_BUTTON_DOWN = 1,
        NR_KEY_STATE_BUTTON_UP = 2,
    }

    public enum NRRgbCameraPluginState
    {
        NR_RGB_CAMERA_PLUGIN_STATE_UNKNOWN = 0,
        NR_RGB_CAMERA_PLUGIN_STATE_PLUGIN = 1,
        NR_RGB_CAMERA_PLUGIN_STATE_PLUGOUT = 2,
    }

    public struct NRKeyStateData
    {
        public NRKeyType key_type;
        public NRKeyState key_state;
        public ulong hmd_time_nanos_device;
    }

}