using System.Collections; // Required for coroutines
using UnityEngine;

public class BottomLerpCoroutine : MonoBehaviour
{
    public RectTransform targetRect; // Assign in the inspector
    public float valueA = 0f; // Starting value
    public float valueB = 100f; // Target value
    public bool checkBool = true; // The bool to check before starting the lerp

    //ssave
    private float targetValue;
    public float lerpSpeed = 0.4f; // Speed of the lerp

    [ContextMenu("Lerp to Value A")]
    public void OnButtonClick()
    {
        checkBool = !checkBool;
        
        if (!checkBool) // Check if the bool is true
        {
            StopAllCoroutines(); // Stop any existing coroutines to avoid conflicts
            StartCoroutine(LerpToValueCoroutine(valueA));
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(LerpToValueCoroutine(valueB));
        }
    }

    private IEnumerator LerpToValueCoroutine(float newValue)
    {
        targetValue = newValue;
        bool lerping = true;

        while (lerping)
        {
            float newBottom = Mathf.Lerp(
                targetRect.offsetMin.y,
                targetValue,
                Time.deltaTime * lerpSpeed
            );
            targetRect.offsetMin = new Vector2(targetRect.offsetMin.x, newBottom);

            if (Mathf.Approximately(newBottom, targetValue))
            {
                lerping = false; // Stop the coroutine loop when the target value is reached
            }
            yield return null; // Wait for the next frame before continuing the loop
        }
    }
}
