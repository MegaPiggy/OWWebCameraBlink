using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WebCameraBlink
{
    public class UIButtonHoverEffects : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField]
        private bool hasOutline;
        private GameObject selectedText;
        private GameObject unselectedText;
        private GameObject selectedOutline;
        private GameObject unselectedOutline;

        public void OnEnable()
        {
            selectedText = transform.Find("Selected Text").gameObject;
            unselectedText = transform.Find("Unselected Text").gameObject;
            if (hasOutline)
            {
                selectedOutline = transform.Find("Outline Selected").gameObject;
                unselectedOutline = transform.Find("Outline Unselected").gameObject;
            }
            ToggleEffects(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            ToggleEffects(true);
            EventSystem.current.SetSelectedGameObject(null);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // PlayHoverSound();
            ToggleEffects(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ToggleEffects(false);
        }

        public void ToggleEffects(bool isSelected)
        {
            selectedText.SetActive(isSelected);
            unselectedText.SetActive(!isSelected);
            if (hasOutline)
            {
                selectedOutline.SetActive(isSelected);
                unselectedOutline.SetActive(!isSelected);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.parent.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent.GetComponent<RectTransform>());
        }
    }

}
