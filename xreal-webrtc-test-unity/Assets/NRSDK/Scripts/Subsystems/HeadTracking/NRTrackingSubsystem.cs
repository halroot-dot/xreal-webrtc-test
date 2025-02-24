/****************************************************************************
* Copyright 2019 Xreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.xreal.com/        
* 
*****************************************************************************/

using System;

namespace NRKernal
{
    public class NRTrackingSubsystemDescriptor : IntegratedSubsystemDescriptor<NRTrackingSubsystem>
    {
        public const string Name = "Subsystem.Tracking";
        public override string id => Name;
    }

    public class NR6DOFTrackingSubsystemDescriptor : IntegratedSubsystemDescriptor<NRTrackingSubsystem>, ITrackingSubsystemDescriptor
    {
        public static readonly string Name = "nr_perception_head_tracking_6dof";
        public override string id => Name;
    }

    public class NR3DOFTrackingSubsystemDescriptor : IntegratedSubsystemDescriptor<NRTrackingSubsystem>, ITrackingSubsystemDescriptor
    {
        public static readonly string Name = "nr_perception_head_tracking_3dof";
        public override string id => Name;
    }

    public class NR0DOFTrackingSubsystemDescriptor : IntegratedSubsystemDescriptor<NRTrackingSubsystem>, ITrackingSubsystemDescriptor
    {
        public static readonly string Name = "nr_perception_head_tracking_0dof";
        public override string id => Name;
    }

    public class NR0DOFStabTrackingSubsystemDescriptor : IntegratedSubsystemDescriptor<NRTrackingSubsystem>, ITrackingSubsystemDescriptor
    {
        public static readonly string Name = "nr_perception_head_tracking_eis";
        public override string id => Name;
    }

    public class NRTrackingSubsystem : IntegratedSubsystem<NRTrackingSubsystemDescriptor>
    {
        private ITrackingDataProvider m_Provider;

        public NRTrackingSubsystem(NRTrackingSubsystemDescriptor descriptor) : base(descriptor)
        {
#if UNITY_EDITOR
            m_Provider = new NREmulatorTrackingDataProvider();
#else
            m_Provider = new NRTrackingDataProvider();
#endif
        }

        public override void Start()
        {
            base.Start();
            m_Provider.Start();
        }

        public override void Pause()
        {
            base.Pause();
            m_Provider.Pause();
        }

        public override void Resume()
        {
            base.Resume();
            m_Provider.Resume();
        }

        public override void Destroy()
        {
            base.Destroy();
            m_Provider.Destroy();
        }

        public void RegistInputSubSystemEventCallback(Action<bool> callback)
        {
            m_Provider.RegistInputSubSystemEventCallback(callback);
        }

        public bool GetFramePresentHeadPose(ref UnityEngine.Pose pose, ref LostTrackingReason lostReason, ref ulong timestamp)
        {
            return m_Provider.GetFramePresentHeadPose(ref pose, ref lostReason, ref timestamp);
        }

        public bool GetFramePresentTimeByCount(uint count, ref ulong timeStamp)
        {
            return m_Provider.GetFramePresentTimeByCount(count, ref timeStamp);
        }

        public bool GetHeadPose(ref UnityEngine.Pose pose, ulong timestamp)
        {
            return m_Provider.GetHeadPose(ref pose, timestamp);
        }

        public ulong GetHMDTimeNanos()
        {
            return m_Provider.GetHMDTimeNanos();
        }

        public bool InitTrackingType(IntegratedSubsystemDescriptor descriptor)
        {
            return m_Provider.InitTrackingType(descriptor);
        }

        public bool SwitchTrackingType(IntegratedSubsystemDescriptor descriptor)
        {
            return m_Provider.SwitchTrackingType(descriptor);
        }

        public void Recenter()
        {
            m_Provider.Recenter();
        }
    }
}
