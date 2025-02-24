using NRKernal;
using UnityEngine;

public static class NRHandsManagerExtension
{
    public static float GetVerticalAngleBetweenWristAndCenterCamera(this NRHandsManager handManager, HandEnum handEnum)
    {
        var cameraTransform = NRInput.CameraCenter;
        var handState = NRInput.Hands.GetHandState(handEnum);
        var wristPose = handState.GetJointPose(HandJointID.Wrist); //world pose
        Vector3 pos = cameraTransform.InverseTransformPoint(wristPose.position);
        pos.x = 0;
        float angle = Vector3.Angle(pos, Vector3.forward);
        return angle;
    }
}
