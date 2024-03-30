using UnityEngine;

public class CubeController : MonoBehaviour
{
    private SimplePinch simplePinch;
    private HandInputReference handInputRef;

    public float lerpVelocity = 1.0f;
    private bool isLerping = false;
    private GameObject targetFinger;
    private Vector3 targetPosition;

    void Start()
    {
        simplePinch = FindObjectOfType<SimplePinch>();
        handInputRef = FindObjectOfType<HandInputReference>();

        if (simplePinch != null)
        {
            simplePinch.OnPinchRightSelect.AddListener(() => StartLerping(handInputRef.RightHandCenter));
            simplePinch.OnPinchLeftSelect.AddListener(() => StartLerping(handInputRef.LeftHandCenter));
            simplePinch.OnPinchRightRelease.AddListener(() => StopLerping(handInputRef.RightHandCenter));
            simplePinch.OnPinchLeftRelease.AddListener(() => StopLerping(handInputRef.LeftHandCenter));
        }
    }

    void Update()
    {
        if (isLerping && targetFinger != null)
        {

            targetPosition = targetFinger.transform.position;
            transform.position = Vector3.Lerp(transform.position, targetPosition, lerpVelocity * Time.deltaTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetFinger.transform.rotation, lerpVelocity * Time.deltaTime);
        }
    }

    private void StartLerping(GameObject finger)
    {
        if (Vector3.Distance(transform.position, finger.transform.position) < 0.1f)
        {
            targetFinger = finger;
            isLerping = true;
        }

    }

    private void StopLerping(GameObject finger)
    {
        if (targetFinger == finger)
        {
            isLerping = false;
            targetFinger = null;
        }
    }
}
