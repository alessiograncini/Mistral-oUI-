using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(HandInputReference))]
public class SimplePinch : MonoBehaviour
{
    public UnityEvent OnPinchRightSelect = new UnityEvent();
    public UnityEvent OnPinchLeftSelect = new UnityEvent();
    public UnityEvent OnPinchRightRelease = new UnityEvent();
    public UnityEvent OnPinchLeftRelease = new UnityEvent();
    private BoolCondition pinchSelectRight = new BoolCondition();
    private BoolCondition pinchSelectLeft = new BoolCondition();
    public float pinchThreshold = 0.03f;
   
    // Update is called once per frame
    void Update()
    {

        // HANDLE RIGHT 
        if (Vector3.Distance(GetComponent<HandInputReference>().RightHandIndexTip.transform.position, GetComponent<HandInputReference>().RightHandThumbTip.transform.position) < pinchThreshold)
        {
            pinchSelectRight.Value = true;
        }
        else
        {
            pinchSelectRight.Value = false;
        }
        if (pinchSelectRight.ChangedTrue)
        {
            OnPinchRightSelect.Invoke();
        }
        if (pinchSelectRight.ChangedFalse)
        {
            OnPinchRightRelease.Invoke();
        }

        // HANDLE LEFT 
        if (Vector3.Distance(GetComponent<HandInputReference>().LeftHandIndexTip.transform.position, GetComponent<HandInputReference>().LeftHandThumbTip.transform.position) < pinchThreshold)
        {
            pinchSelectLeft.Value = true;
        }
        else
        {
            pinchSelectLeft.Value = false;
        }
        if (pinchSelectLeft.ChangedTrue)
        {
            OnPinchLeftSelect.Invoke();
        }
        if (pinchSelectLeft.ChangedFalse)
        {
            OnPinchLeftRelease.Invoke();
        }
       
    }

   
}
