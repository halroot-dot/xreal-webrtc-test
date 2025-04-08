[System.Serializable]
public class WebRTCConfiguration
{
    public string ServerUrl = "192.168.x.x";
    public int ServerPort = 3001;
    private float recordDurationSeconds = 30.0f;
    private int videoWidth = 1280;
    private int videoHeight = 720;
    private int frameRate = 30;

    private string[] iceServers = new[]
    {
        "stun:stun.l.google.com:19302"
    };

    public float RecordDurationSeconds => recordDurationSeconds;
    public int VideoWidth => videoWidth;
    public int VideoHeight => videoHeight;
    public int FrameRate => frameRate;
    public string[] IceServers => iceServers;

    public string GetFullServerUrl()
    {
        return $"ws://{ServerUrl}:{ServerPort}/";
    }
}