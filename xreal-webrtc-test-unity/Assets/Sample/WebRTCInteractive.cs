using UnityEngine;
using UnityEngine.EventSystems;

public class WebRTCInteractive : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject webRTCStreamerObject;

    private MeshRenderer m_MeshRender;
    private WebRTCStreamer streamer;
    private Color defaultColor = Color.white;
    private Color hoverColor = new Color(1f, 0.8f, 0.8f); // 薄い赤
    private Color activeColor = Color.red;
    private bool isStreaming = false;

    void Awake()
    {
        m_MeshRender = GetComponent<MeshRenderer>();
        if (webRTCStreamerObject != null)
        {
            streamer = webRTCStreamerObject.GetComponent<WebRTCStreamer>();
            webRTCStreamerObject.SetActive(false);
        }
    }

    void Start()
    {
        m_MeshRender.material.color = defaultColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (webRTCStreamerObject == null) return;

        isStreaming = !isStreaming;
        if (isStreaming)
        {
            webRTCStreamerObject.SetActive(true);
            streamer.StartWebRTC();
        }
        else
        {
            // 先にクリーンアップを呼び出してから無効化
            if (streamer != null)
            {
                streamer.OnDestroy();

            }
            webRTCStreamerObject.SetActive(false);
        }
        m_MeshRender.material.color = isStreaming ? activeColor : defaultColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isStreaming)
        {
            m_MeshRender.material.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isStreaming)
        {
            m_MeshRender.material.color = defaultColor;
        }
    }
}
