using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;

public class MapController : MonoBehaviour
{
    [SerializeField] private string APIKey = "Your API Key";
    [SerializeField] private RawImage minimap;
    [SerializeField] private Material panoramaMaterial;
    [SerializeField] private bool startVisible = false;
    [SerializeField] private Collider sphereCollider;  // Sphereのコライダー
    [SerializeField] private Rigidbody sphereRigidbody;  // Sphereのリジッドボディ

    private double lat = 35.659484;
    private double lng = 139.700438;
    private float mapZoom = 16;
    private float viewangle = 0;
    private string panoID;

    private bool isPanoramaChecking = false;
    private bool isPositionChanged = false;

    private Texture2D atlas;
    private Texture2D highpanotex_zoom1;
    private Texture2D highpanotex_zoom2;
    private Texture2D lowpanotex_zoom1;
    private Texture2D lowpanotex_zoom2;
    private Texture2D lowpanotex_zoom3;

    private const string CACHE_DIRECTORY = "PanoramaCache";
    private const long MAX_CACHE_SIZE = 50L * 1024 * 1024; // 50MB in bytes

    void Awake()
    {
        // テクスチャの初期化時にフォーマットを指定
        atlas = new Texture2D(4096, 2048, TextureFormat.RGBA32, false);
        highpanotex_zoom1 = new Texture2D(1024, 512, TextureFormat.RGBA32, false);
        highpanotex_zoom2 = new Texture2D(2048, 1024, TextureFormat.RGBA32, false);
        lowpanotex_zoom1 = new Texture2D(832, 416, TextureFormat.RGBA32, false);
        lowpanotex_zoom2 = new Texture2D(1664, 832, TextureFormat.RGBA32, false);
        lowpanotex_zoom3 = new Texture2D(3328, 1664, TextureFormat.RGBA32, false);

        // テクスチャの設定を更新
        atlas.wrapMode = TextureWrapMode.Clamp;
        atlas.filterMode = FilterMode.Bilinear;

        var textures = new[] {
            highpanotex_zoom1, highpanotex_zoom2,
            lowpanotex_zoom1, lowpanotex_zoom2, lowpanotex_zoom3
        };

        foreach (var tex in textures)
        {
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
        }

        // キャッシュディレクトリの作成とクリーニング
        string cachePath = Path.Combine(Application.persistentDataPath, CACHE_DIRECTORY);
        if (!Directory.Exists(cachePath))
        {
            Directory.CreateDirectory(cachePath);
        }
        CheckAndCleanCache();

        // 物理演算の初期設定
        if (sphereRigidbody != null)
        {
            sphereRigidbody.useGravity = false;
            sphereRigidbody.isKinematic = true;
        }

        if (sphereCollider != null)
        {
            sphereCollider.isTrigger = true;
        }
    }

    void Start()
    {
        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = Color.black;
        }

        // 初期表示の制御
        gameObject.SetActive(startVisible);
    }

    // 表示制御用メソッドを追加
    public void ShowMap(double newLat, double newLng)
    {
        gameObject.SetActive(true);

        // 物理演算の影響を防ぐ
        if (sphereRigidbody != null)
        {
            sphereRigidbody.linearVelocity = Vector3.zero;
            sphereRigidbody.angularVelocity = Vector3.zero;
        }

        StartCoroutine(CheckPanorama(newLat, newLng));
    }

    public void HideMap()
    {
        gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            minimap.rectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            float xpos = localPoint.x * 512f / minimap.rectTransform.rect.width;
            float ypos = localPoint.y * 512f / minimap.rectTransform.rect.height;

            MinimapClicked(xpos, ypos);
        }
    }

    private void PrintTextLog(string message)
    {
        Debug.Log($"[Panorama] {message}");
    }

    private IEnumerator CheckHighResolutionAvailability()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(
            $"https://geo0.ggpht.com/cbk?cb_client=maps_sv.tactile&authuser=1&hl=ja&gl=jp&panoid={panoID}&output=tile&x=7&y=3&zoom=3&nbt&fover=0"))
        {
            yield return request.SendWebRequest();
            bool isAvailable = request.responseCode != 400;
            yield return isAvailable;
        }
    }

    public void MinimapClicked(float xpos, float ypos)
    {
        if (isPanoramaChecking) return;

        //TODO: Add calculation logic for new lat/lng based on click position
        double newLat = lat + (ypos / 256.0);
        double newLng = lng + (xpos / 256.0);

        StartCoroutine(CheckPanorama(newLat, newLng));
    }

    private IEnumerator LoadMinimap()
    {
        string mapUrl = $"https://maps.googleapis.com/maps/api/staticmap?" +
            $"center={lat},{lng}" +
            $"&zoom={mapZoom}" +
            $"&size=512x512" +
            $"&markers=color:red%7C{lat},{lng}" +
            $"&key={APIKey}";

        using (UnityWebRequest request = UnityWebRequest.Get(mapUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(request.downloadHandler.data);
                minimap.texture = texture;
            }
        }

    }

    private IEnumerator CheckPanorama(double tmplat, double tmplng)
    {
        if (isPanoramaChecking)
            yield break;

        isPanoramaChecking = true;
        string url = $"https://maps.googleapis.com/maps/api/streetview/metadata?size=512x512&location={tmplat},{tmplng}&heading={viewangle}&pitch=0&fov=110&key={APIKey}&source=outdoor";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var jsonDict = Json.Deserialize(request.downloadHandler.text) as Dictionary<string, object>;
                if (((string)jsonDict["status"]).Equals("OK"))
                {
                    lat = tmplat;
                    lng = tmplng;
                    panoID = (string)jsonDict["pano_id"];

                    StartCoroutine(LoadMinimap());
                    StartCoroutine(LoadPanorama()); // パノラマ読み込みを開始
                    PrintTextLog($"Loading lat={lat} lng={lng}\nDate:{(string)jsonDict["date"]}");
                    isPositionChanged = true;
                }
                else
                {
                    PrintTextLog("Panorama Not Found");
                }
            }
            else
            {
                PrintTextLog($"Panorama check error: {request.error}");
            }
        }

        isPanoramaChecking = false;
    }

    private IEnumerator LoadPanorama()
    {
        while (true)
        {
            isPositionChanged = false;

            bool isHRimageExists = false;
            var checkHighRes = CheckHighResolutionAvailability();
            while (checkHighRes.MoveNext())
            {
                if (checkHighRes.Current is bool)
                {
                    isHRimageExists = (bool)checkHighRes.Current;
                }
                yield return checkHighRes.Current;
            }

            if (isHRimageExists)
            {
                yield return LoadHighResolutionPanorama();
            }
            else
            {
                yield return LoadLowResolutionPanorama();
            }

            if (!isPositionChanged)
            {
                break;
            }
        }

        PrintTextLog("Loading Complete");
    }

    private IEnumerator LoadHighResolutionPanorama()
    {
        // Zoom level 1
        yield return LoadPanoramaTiles(1, 2, 1, highpanotex_zoom1);
        if (isPositionChanged) yield break;

        // Zoom level 2
        yield return LoadPanoramaTiles(2, 4, 2, highpanotex_zoom2);
    }

    private IEnumerator LoadLowResolutionPanorama()
    {
        // Zoom level 1-3
        yield return LoadPanoramaTiles(1, 2, 1, lowpanotex_zoom1, true);
        if (isPositionChanged) yield break;

        yield return LoadPanoramaTiles(2, 4, 2, lowpanotex_zoom2, true);
        if (isPositionChanged) yield break;

        yield return LoadPanoramaTiles(3, 7, 4, lowpanotex_zoom3, true);
    }

    private string GetCacheFilePath(int zoom, int x, int y, string panoId)
    {
        string fileName = $"{panoId}_z{zoom}_x{x}_y{y}.jpg";
        return Path.Combine(Application.persistentDataPath, CACHE_DIRECTORY, fileName);
    }

    private bool TryLoadFromCache(string filePath, out Texture2D texture)
    {
        texture = null;
        if (File.Exists(filePath))
        {
            try
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                texture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
                if (texture.LoadImage(fileData))
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Cache load error: {e.Message}");
            }
        }
        return false;
    }

    private void SaveToCache(string filePath, byte[] imageData)
    {
        try
        {
            File.WriteAllBytes(filePath, imageData);
            CheckAndCleanCache(); // キャッシュサイズをチェック
        }
        catch (Exception e)
        {
            Debug.LogError($"Cache save error: {e.Message}");
        }
    }

    private void CheckAndCleanCache()
    {
        string cachePath = Path.Combine(Application.persistentDataPath, CACHE_DIRECTORY);
        var directory = new DirectoryInfo(cachePath);

        // キャッシュサイズの計算
        long totalSize = 0;
        var files = directory.GetFiles("*.jpg")
                            .OrderBy(f => f.LastWriteTime)
                            .ToList();

        foreach (var file in files)
        {
            totalSize += file.Length;
        }

        // キャッシュサイズが制限を超えている場合、古いファイルから削除
        if (totalSize > MAX_CACHE_SIZE)
        {
            PrintTextLog($"Cache size ({totalSize / 1024 / 1024}MB) exceeds limit. Cleaning...");

            foreach (var file in files)
            {
                try
                {
                    totalSize -= file.Length;
                    file.Delete();
                    PrintTextLog($"Deleted cache file: {file.Name}");

                    if (totalSize <= MAX_CACHE_SIZE)
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error deleting cache file: {e.Message}");
                }
            }
        }
    }

    private IEnumerator LoadPanoramaTiles(int zoom, int xTiles, int yTiles, Texture2D targetTexture, bool isLowRes = false)
    {
        // アトラスをクリア
        Color[] clearPixels = new Color[atlas.width * atlas.height];
        for (int i = 0; i < clearPixels.Length; i++) clearPixels[i] = Color.black;
        atlas.SetPixels(clearPixels);
        atlas.Apply();

        // タイルを配置
        for (int x = 0; x < xTiles; x++)
        {
            for (int y = 0; y < yTiles; y++)
            {
                string cacheFilePath = GetCacheFilePath(zoom, x, y, panoID);
                Texture2D tileTexture;

                if (TryLoadFromCache(cacheFilePath, out tileTexture))
                {
                    // キャッシュから読み込み成功
                    atlas.SetPixels(x * 512, (yTiles - 1 - y) * 512, 512, 512, tileTexture.GetPixels());
                    PrintTextLog($"Loaded from cache - Position: x={x * 512}, y={(yTiles - 1 - y) * 512}, zoom={zoom}");
                    Destroy(tileTexture);
                }
                else
                {
                    // APIから取得
                    string url = $"https://maps.google.com/cbk?output=tile&panoid={panoID}&zoom={zoom}&x={x}&y={y}&heading=0";
                    using (UnityWebRequest request = UnityWebRequest.Get(url))
                    {
                        yield return request.SendWebRequest();

                        if (request.result == UnityWebRequest.Result.Success)
                        {
                            // キャッシュに保存
                            SaveToCache(cacheFilePath, request.downloadHandler.data);

                            Texture2D tempTex = new Texture2D(512, 512, TextureFormat.RGBA32, false);
                            tempTex.LoadImage(request.downloadHandler.data);
                            atlas.SetPixels(x * 512, (yTiles - 1 - y) * 512, 512, 512, tempTex.GetPixels());
                            PrintTextLog($"Downloaded and cached - Position: x={x * 512}, y={(yTiles - 1 - y) * 512}, zoom={zoom}");
                            Destroy(tempTex);
                        }
                    }
                }
            }
        }

        atlas.Apply();

        // テクスチャのリサイズと切り出し
        if (isLowRes)
        {
            // 低解像度の場合は指定サイズで切り出し
            int srcHeight = atlas.height;
            int dstHeight = targetTexture.height;
            int yOffset = srcHeight - dstHeight;

            targetTexture.SetPixels(0, 0, targetTexture.width, targetTexture.height,
                atlas.GetPixels(0, yOffset, targetTexture.width, targetTexture.height));
            targetTexture.Apply(false);
        }
        else
        {
            // 高解像度の場合は元のサイズを維持してコピー
            if (zoom == 1)
            {
                targetTexture.SetPixels(0, 0, 1024, 512, atlas.GetPixels(0, 0, 1024, 512));
                targetTexture.Apply(false);
            }
            else if (zoom == 2)
            {
                targetTexture.SetPixels(0, 0, 2048, 1024, atlas.GetPixels(0, 0, 2048, 1024));
                targetTexture.Apply(false);
            }
        }

        if (panoramaMaterial != null)
        {
            panoramaMaterial.mainTexture = targetTexture;
            panoramaMaterial.mainTextureScale = new Vector2(-1, 1);
            panoramaMaterial.mainTextureOffset = new Vector2(1, 0);
            panoramaMaterial.SetTexture("_MainTex", targetTexture);

            // デバッグ情報を出力
            PrintTextLog($"Texture applied - Size: {targetTexture.width}x{targetTexture.height}, Zoom: {zoom}");
            PrintTextLog($"Atlas size: {atlas.width}x{atlas.height}");
        }

    }



    void OnDestroy()
    {
        // テクスチャのクリーンアップ
        var textures = new[] { atlas, highpanotex_zoom1, highpanotex_zoom2,
                              lowpanotex_zoom1, lowpanotex_zoom2, lowpanotex_zoom3 };
        foreach (var tex in textures)
        {
            if (tex != null) Destroy(tex);
        }
    }
}


