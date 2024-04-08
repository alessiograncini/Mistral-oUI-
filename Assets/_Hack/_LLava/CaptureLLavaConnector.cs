using System.Collections;
using MagicLeap.Examples;
using TMPro;
using UnityEngine;

public class CaptureLLavaConnector : MonoBehaviour
{
    public CameraCaptureExample CameraCaptureExample;
    public CameraCaptureVisualizer CameraCaptureVisualizer;
    public LLavaUnityBridge LLavaUnityBridge;

    public TextMeshProUGUI ResponseText;
    public TextMeshProUGUI FeedbackText;

    public UnityEngine.UI.Image ImageFeedbackUI;

    void Start()
    {
        StartCoroutine(MistralRecursive());
    }

    private bool recordStarted;

    public IEnumerator MistralRecursive()
    {
        // TODO:
        // Get video frame here
        // display it ImageFeedbackUI.sprite.texture
        // CameraCaptureExample.DisplayLastCapturedFrameOnUI(ImageFeedbackUI);
        yield return new WaitForSeconds(4);
        LLavaUnityBridge.UploadRecursive(ImageFeedbackUI.sprite.texture);
        StartCoroutine(MistralRecursive());
    }

    void Update()
    {
        // always show response
        ResponseText.text = LLavaUnityBridge.UpdateResponseConnector();
        FeedbackText.text = LLavaUnityBridge.UpdateFeedback();
    }
}
