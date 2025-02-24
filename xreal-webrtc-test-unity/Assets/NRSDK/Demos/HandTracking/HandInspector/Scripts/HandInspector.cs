using NRKernal;
using UnityEngine;
using UnityEngine.UI;
public class HandInspector : MonoBehaviour
{
    public HandEnum handEnum;
    public Vector3 cameraLocalPos;

    public RawImage m_RawImage;
    public Text m_HandInfoText;

    public int width = 512;
    public int height = 512;
    public int depth = 24;
    public RenderTextureFormat format = RenderTextureFormat.Default;

    private Camera renderCamera;
    private RenderTexture renderTexture;

    private void OnEnable()
    {
        NRInput.Hands.OnHandStatesUpdated += OnHandTracking;
        NRInput.Hands.OnHandTrackingStopped += OnHandTrackingStopped;
    }


    private void OnDisable()
    {
        NRInput.Hands.OnHandStatesUpdated -= OnHandTracking;
        NRInput.Hands.OnHandTrackingStopped -= OnHandTrackingStopped;
    }



    void Start()
    {
        // Create the render texture
        renderTexture = RenderTexture.GetTemporary(width, height, depth, format);

        // Create a camera to render the object onto the render texture
        GameObject cameraObject = new GameObject("RenderCamera");
        renderCamera = cameraObject.AddComponent<Camera>();
        renderCamera.targetTexture = renderTexture;
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = Color.black;
        renderCamera.nearClipPlane = 0.001f;
        renderCamera.enabled = false; // Disable the camera by default

        m_RawImage.texture =  renderTexture;
        renderCamera.transform.parent = this.transform;
    }
    private void OnHandTracking()
    {
        float angle = NRInput.Hands.GetVerticalAngleBetweenWristAndCenterCamera(handEnum);
        var handState = NRInput.Hands.GetHandState(handEnum);
        m_HandInfoText.text = $"Gesture: {handState.currentGesture}\n" +
            $"IsTracked: {handState.isTracked}\n" +
            $"IsValid: {handState.pointerPoseValid}\n" +
            $"AngleFromCenterEye: {angle} {NRInput.CameraCenter.InverseTransformPoint(handState.GetJointPose(HandJointID.Wrist).position)}" ;
        if (handState.jointsPoseDict.TryGetValue(HandJointID.Wrist, out var wristPose))
        {
            renderCamera.transform.position = wristPose.position + NRFrame.HeadPose.rotation * cameraLocalPos;
            renderCamera.transform.rotation = NRFrame.HeadPose.rotation;
        }

    }

    private void OnHandTrackingStopped()
    {
        OnHandTracking();
    }

    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Space))
        {
            // Enable the camera and render the object onto the render texture
            renderCamera.Render();

        }
    }

    void OnDestroy()
    {
        // Release the render texture when the object is destroyed
        RenderTexture.ReleaseTemporary(renderTexture);
    }
}
