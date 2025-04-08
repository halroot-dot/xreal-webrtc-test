using UnityEngine;
using System.Collections.Generic;
using NRKernal;

namespace GeoguessrAnswer
{
    public class LocationData
    {
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int ButtonNumber { get; set; }
    }

    public class GestureAction : MonoBehaviour
    {
        [SerializeField] private MapController mapController;
        [SerializeField] private GameObject[] locationCubes; // 3つのキューブをInspectorで設定
        private Dictionary<HandGesture, LocationData>[] locationMaps; // 複数のロケーションマップ
        private int currentCubeIndex = 0; // 現在選択中のキューブのインデックス
        public HandEnum handEnum;
        private float gestureTimer = 0f;
        private HandGesture lastGesture = HandGesture.None;
        private const float GESTURE_DURATION = 2f;
        private HandGesture currentActiveGesture = HandGesture.None;
        private float lastExecutionDebugTime = 0f;

        void Start()
        {
            InitializeLocationMaps();
            if (mapController != null)
            {
                mapController.HideMap();
            }

            // Initialize cube indices
            for (int i = 0; i < locationCubes.Length; i++)
            {
                if (locationCubes[i] != null)
                {
                    var cubeInteractive = locationCubes[i].GetComponent<CubeInteractive>();
                    if (cubeInteractive != null)
                    {
                        cubeInteractive.SetCubeIndex(i);
                    }
                }
            }

            UpdateCubeSelection();
        }

        void Update()
        {
            var handState = NRInput.Hands.GetHandState(handEnum);
            if (handState == null)
            {
                ResetGestureTimer();
                return;
            }

            var currentGesture = handState.currentGesture;

            // 通常のジェスチャー処理
            if (currentGesture == lastGesture && currentGesture != HandGesture.None)
            {
                gestureTimer += Time.deltaTime;

                if (gestureTimer >= GESTURE_DURATION)
                {
                    if (locationMaps[currentCubeIndex].TryGetValue(currentGesture, out LocationData location))
                    {
                        if (currentGesture != currentActiveGesture)
                        {
                            mapController.ShowMap(location.Latitude, location.Longitude);
                            currentActiveGesture = currentGesture;
                            Debug.Log($"[Panorama] Updated location to: {location.Name} (Cube {currentCubeIndex + 1})");
                        }
                    }
                }
            }
            else
            {
                ResetGestureTimer();
            }

            lastGesture = currentGesture;

            if (Time.time - lastExecutionDebugTime >= 2f)
            {
                Debug.Log($"[Panorama] Gesture Timer: {gestureTimer:F1}s, Current gesture: {currentGesture}, Active gesture: {currentActiveGesture}, Current cube: {currentCubeIndex + 1}");
                lastExecutionDebugTime = Time.time;
            }
        }

        public void SelectCube(int index)
        {
            if (index >= 0 && index < locationCubes.Length)
            {
                currentCubeIndex = index;
                UpdateCubeSelection();
                Debug.Log($"[Panorama] Selected cube {currentCubeIndex + 1}");
            }
        }

        private void UpdateCubeSelection()
        {
            // すべてのキューブの選択状態を更新
            for (int i = 0; i < locationCubes.Length; i++)
            {
                if (locationCubes[i] != null)
                {
                    var cubeInteractive = locationCubes[i].GetComponent<CubeInteractive>();
                    if (cubeInteractive != null)
                    {
                        cubeInteractive.SetSelected(i == currentCubeIndex);
                    }
                }
            }
            Debug.Log($"[Panorama] Switched to cube {currentCubeIndex + 1}");
        }

        private void ResetGestureTimer()
        {
            gestureTimer = 0f;
        }

        private void InitializeLocationMaps()
        {
            locationMaps = new Dictionary<HandGesture, LocationData>[3];

            // キューブ1 解答用
            locationMaps[0] = new Dictionary<HandGesture, LocationData>
            {
                {
                    HandGesture.ThumbsUp,
                    new LocationData {
                        Name = "言問橋（スカイツリー）",
                        Latitude = 35.714238,
                        Longitude = 139.8032865,
                        ButtonNumber = 1,
                    }
                },
                {
                    HandGesture.Victory,
                    new LocationData {
                        Name = "渋谷スクランブル交差点",
                        Latitude = 35.6594819,
                        Longitude = 139.6956887,
                        ButtonNumber = 2,
                    }
                },
                {
                    HandGesture.Point,
                    new LocationData {
                        Name = "明治神宮",
                        Latitude = 35.674391,
                        Longitude = 139.6941435,
                        ButtonNumber = 3,
                    }
                },
                {
                    HandGesture.Pinch,
                    new LocationData {
                        Name = "雷門",
                        Latitude = 35.7112081,
                        Longitude = 139.7944656,
                        ButtonNumber = 4,
                    }
                },
                {
                    HandGesture.Call,
                    new LocationData {
                        Name = "新東名上り_新清水→新富士区間",
                        Latitude = 35.1775308,
                        Longitude = 138.6074301,
                        ButtonNumber = 5,
                    }
                }
            };

            // キューブ2 日本の観光地
            locationMaps[1] = new Dictionary<HandGesture, LocationData>
            {
                {
                    HandGesture.ThumbsUp,
                    new LocationData {
                        Name = "DENSO",
                        Latitude = 34.994729,
                        Longitude = 137.007642,
                        ButtonNumber = 1,
                    }
                },
                {
                    HandGesture.Victory,
                    new LocationData {
                        Name = "北海道 吹上温泉",
                        Latitude = 43.429605,
                        Longitude = 142.637350,
                        ButtonNumber = 2,
                    }
                },
                {
                    HandGesture.Point,
                    new LocationData {
                        Name = "京都 毘沙門堂",
                        Latitude = 35.001406,
                        Longitude = 135.819294,
                        ButtonNumber = 3,
                    }
                },
                {
                    HandGesture.Pinch,
                    new LocationData {
                        Name = "熊本 阿蘇中岳第一火口見学路",
                        Latitude = 32.881264,
                        Longitude = 131.086263,
                        ButtonNumber = 4,
                    }
                },
                {
                    HandGesture.Call,
                    new LocationData {
                        Name = "沖縄 水納島",
                        Latitude = 26.649609,
                        Longitude = 127.818119,
                        ButtonNumber = 5,
                    }
                }
            };

            // キューブ3 世界の観光地
            locationMaps[2] = new Dictionary<HandGesture, LocationData>
            {
                {
                    HandGesture.ThumbsUp,
                    new LocationData {
                        Name = "アメリカ ワシントンD.C. ホワイトハウス",
                        Latitude = 38.897720,
                        Longitude = -77.036397,
                        ButtonNumber = 1,
                    }
                },
                {
                    HandGesture.Victory,
                    new LocationData {
                        Name = "ペルー マチュピチュ",
                        Latitude = -13.164535,
                        Longitude = -72.544566,
                        ButtonNumber = 2,
                    }
                },
                {
                    HandGesture.Point,
                    new LocationData {
                        Name = "エッフェル塔",
                        Latitude = 48.8583,
                        Longitude = 2.2945,
                        ButtonNumber = 3,
                    }
                },
                {
                    HandGesture.Pinch,
                    new LocationData {
                        Name = "アラブ首長国連邦 ドバイ",
                        Latitude = 25.195389,
                        Longitude = 55.273003,
                        ButtonNumber = 4,
                    }
                },
                {
                    HandGesture.Call,
                    new LocationData {
                        Name = "サグラダ・ファミリア",
                        Latitude = 41.4036,
                        Longitude = 2.1744,
                        ButtonNumber = 5,
                    }
                }
            };
        }
    }
}