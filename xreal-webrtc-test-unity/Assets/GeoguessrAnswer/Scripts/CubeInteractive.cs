using UnityEngine;
using UnityEngine.EventSystems;

namespace GeoguessrAnswer
{
    public class CubeInteractive : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private MeshRenderer m_MeshRender;
        private Color defaultColor = Color.white;
        private Color hoverColor = Color.blue;
        private Color selectedColor = Color.green;
        private bool isSelected = false;
        private GestureAction gestureAction;
        private int cubeIndex;

        void Awake()
        {
            m_MeshRender = transform.GetComponent<MeshRenderer>();
            gestureAction = FindObjectOfType<GestureAction>();
        }

        public void SetCubeIndex(int index)
        {
            cubeIndex = index;
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            m_MeshRender.material.color = selected ? selectedColor : defaultColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (gestureAction != null)
            {
                gestureAction.SelectCube(cubeIndex);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isSelected)
            {
                m_MeshRender.material.color = hoverColor;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isSelected)
            {
                m_MeshRender.material.color = defaultColor;
            }
        }
    }
}