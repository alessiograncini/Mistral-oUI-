using System.Collections;
using UnityEngine;
using Vuplex.WebView;

public class WebViewManager : MonoBehaviour
{
    public CanvasWebViewPrefab canvasWebViewPrefab;



    public void UpdateLink(string newUrl)
    {
        StartCoroutine(UpdateUrlWhenReady(newUrl));
    }

    IEnumerator UpdateUrlWhenReady(string newUrl)
    {
        // Wait until the CanvasWebViewPrefab is initialized
        yield return canvasWebViewPrefab.WaitUntilInitialized();

        // Load a new URL
        canvasWebViewPrefab.WebView.LoadUrl(newUrl);
    }
}
