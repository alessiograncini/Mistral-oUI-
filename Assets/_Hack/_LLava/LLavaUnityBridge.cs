using System.Collections;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;

public class LLavaUnityBridge : MonoBehaviour
{
    private string pythonServerURL = "https://ba9c1b2c7283.ngrok.app/upload-image/"; //"http://192.168.1.181:5000/nextTick"; // Replace with your Python server URL
    private string webRequestRefreshURL = "https://3764cb5e7025.ngrok.app/render-default/";
    //private string bunServerURL = "http://localhost:8000/getResponse"; // Replace with your Bun server URL
    private string responseID = ""; // This will store the ID returned from the Python server

    public Texture2D ImageTest; // The image you want to sends
    public string Response;

    public WebViewManager WebViewManager;
    private string feedback = "";

    public void UploadConnector()
    {
        StartCoroutine(UploadCoroutine(ImageTest));
    }

    public void GetResponseConnector()
    {
        if (string.IsNullOrEmpty(responseID))
        {
            Debug.LogError("Response ID is not set. Make sure you've uploaded the image first.");
            return;
        }
        StartCoroutine(GetResponseCoroutine(responseID));
    }

    public string UpdateResponseConnector()
    {
        return Response;
    }

    public string UpdateFeedback()
    {
        return feedback;
    }

    [ContextMenu("Upload")]
    public void Upload()
    {
        StartCoroutine(UploadCoroutine(ImageTest));
    }

    public void UploadRecursive(Texture2D image)
    {
        StartCoroutine(UploadCoroutine(image));
    }

    private string caption;

    public IEnumerator UploadCoroutine(Texture2D image)
    {
        WWWForm form = new WWWForm();
        byte[] imageData = image.EncodeToPNG();
        form.AddBinaryData("file", imageData, "image.png");

        using (UnityWebRequest www = UnityWebRequest.Post(pythonServerURL, form))
        {
            // Add custom header here
            //www.SetRequestHeader("ngrok-skip-browser-warning", "true");
            //www.SetRequestHeader("User-Agent", "CustomUserAgent/1.0");
            www.SetRequestHeader("ngrok-skip-browser-warning", "true");
              www.SetRequestHeader("Accept", "application/json");
        

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error: " + www.error);
            }
            else
            {
                Debug.Log("Image uploaded successfully!");
                var jsonResponse = JSON.Parse(www.downloadHandler.text);
                responseID = jsonResponse["url"]; // Capture the ID returned by the server
                Debug.Log("Received ID: " + responseID);
                caption = jsonResponse["caption"];
                // see in editor
                Response = caption;
                // Optionally, start getting response automatically
                yield return new WaitForSeconds(40);
                WebViewManager.UpdateLink(webRequestRefreshURL);
                //GetResponseConnector();
            }
        }
    }

    private IEnumerator GetResponseCoroutine(string id)
    {
        //string requestURL = $"{bunServerURL}/{id}";
        string requestURL = $"{id}";

        while (true)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(requestURL))
            {
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Error getting response: {www.error}");
                    break;
                }
                else
                {
                           yield return new WaitForSeconds(10);
                            WebViewManager.UpdateLink(requestURL);
                    // var jsonResponse = JSON.Parse(www.downloadHandler.text);
                    // string type = jsonResponse["type"];

                    // switch (type)
                    // {
                    //     case "done":
                    //         feedback = "Loaded new content";
                    //         Response = jsonResponse["data"]; // Update the response data
                    //         //WebViewManager.UpdateLink(Response); // Update the WebView with the new content
                    //         //WebViewManager.UpdateLink(requestURL);
                    //         break;
                    //     case "processing":
                    //         feedback = "Processing...";
                    //         break;
                    //     case "cancelled":
                    //         feedback = "Cancelled by the user.";
                    //         break;
                    //     default:
                    //         feedback = "Unknown response type.";
                    //         yield return new WaitForSeconds(10);
                    //         WebViewManager.UpdateLink(requestURL);
                    //         break;
                    //}

                    Debug.Log("Feedback: " + feedback);

                    // if (type != "processing") // If not processing, we can stop polling
                    // {
                    //     break;
                    // }
                }
            }
            yield return new WaitForSeconds(1); // Poll every second
        }
    }
}
