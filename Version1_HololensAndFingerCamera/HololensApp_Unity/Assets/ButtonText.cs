using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

	public void OnPointerEnter(PointerEventData eventData)
    {
        GetComponentInChildren<Text>().color = Color.white;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GetComponentInChildren<Text>().color = Color.black;
    }

    public void OnBecameVisible()
    {
        GetComponentInChildren<Text>().color = Color.black;
    }

    public void OnBecameInvisible()
    {
        GetComponentInChildren<Text>().color = Color.black;
    }
}
