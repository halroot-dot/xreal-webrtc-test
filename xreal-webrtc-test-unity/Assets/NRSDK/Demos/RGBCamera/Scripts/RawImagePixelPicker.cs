/****************************************************************************
* Copyright 2019 Xreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.xreal.com/        
* 
*****************************************************************************/

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NRKernal.NRExamples
{

    public class RawImagePixelPicker : MonoBehaviour, IPointerDownHandler
    {
        #region settings
        [SerializeField]
        private RawImage m_RawImage;
        [SerializeField]
        private bool m_FlipY;
        [SerializeField]
        private Image m_Pixel;
        [SerializeField]
        bool m_RChannelOnly = true;
        #endregion

        #region events
        public event Action<Vector2> OnRawImageClick;
        #endregion
        public void OnPointerDown(PointerEventData eventData)
        {
            if (m_RawImage == null)
            {
                Debug.LogError("RawImage reference is not set.");
                return;
            }

            // 获取点击的屏幕坐标
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_RawImage.rectTransform,
                eventData.pointerPressRaycast.screenPosition,
                eventData.pressEventCamera,
                out localPoint
            );

            // 计算点击位置相对于 RawImage 的位置
            Vector2 uv;
            RectTransform rt = m_RawImage.rectTransform;
            Vector2 rtSize = rt.rect.size;
            uv = new Vector2(
                (localPoint.x + rtSize.x * 0.5f) / rtSize.x,
                (localPoint.y + rtSize.y * 0.5f) / rtSize.y
            );

            if (m_FlipY)
            {
                uv.y = 1 - uv.y;
            }
            // 检查 UV 是否在有效范围内
            if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
            {
                Debug.LogWarning($"Click is out of bounds. {uv}");
                return;
            }
            else
            {
                Debug.LogWarning($"Click is {uv}");
            }

            // 获取 RawImage 纹理
            Texture2D texture = m_RawImage.texture as Texture2D;
            if (texture == null)
            {
                Debug.LogError("Texture is not a Texture2D.");
                return;
            }

            // 计算纹理坐标
            Vector2 textureCoord = new Vector2(uv.x * texture.width, uv.y * texture.height);

            OnRawImageClick?.Invoke(textureCoord);

            // 获取点击位置的颜色
            Color pixelColor = texture.GetPixel((int)textureCoord.x, (int)textureCoord.y);
            if (m_RChannelOnly)
            {
                pixelColor.g = pixelColor.b = 0;
            }

            // 打印颜色信息
            Debug.Log($"Clicked at UV: {uv}, Texture Coordinate: {textureCoord}, Color: {pixelColor}");
            m_Pixel.color = pixelColor;


        }
    }
}