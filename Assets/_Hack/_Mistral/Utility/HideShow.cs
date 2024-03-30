using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideShow : MonoBehaviour
{
    public GameObject[] objectsToHide;
    bool state;

    // Update is called once per frame
   public void HideShowObjects()
    {
        state = !state;
        foreach (GameObject obj in objectsToHide)
        {
            obj.SetActive(state);//ye
        }
    }
}
