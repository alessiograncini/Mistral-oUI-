using UnityEngine;

public class EnableDisableUI : MonoBehaviour
{
    public MagicLeap.Examples.PlaceFromCamera placeFromCamera;
    public void ToggleUI(){
        placeFromCamera.enabled = !placeFromCamera.enabled;
    }
}
