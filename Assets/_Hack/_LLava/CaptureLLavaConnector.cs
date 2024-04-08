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
        CameraCaptureExample.RunMistraCapture();
    }

    public IEnumerator MistralRecursive()
    {
        //CameraCaptureExample.RunMistraCapture();
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

    /*
    /// <summary>
    /// Start the capture process
    /// </summary>

    public void StartCapture()
    {
        StartCoroutine(CameraCaptureCoroutine());
    }

    /// <summary>
    ///
    /// </summary>
    public void LLMRequestSimple()
    {
        LLavaUnityBridge.UploadConnector();
    }

    /// <summary>
    ///
    /// </summary>
    public void LLMGetResponseSimple()
    {
        LLavaUnityBridge.GetResponseConnector();
    }

    public IEnumerator CameraCaptureCoroutine()
    {
        yield return new WaitForSeconds(3);
        CameraCaptureExample.CaptureImageSimple();
    }
    */
}
