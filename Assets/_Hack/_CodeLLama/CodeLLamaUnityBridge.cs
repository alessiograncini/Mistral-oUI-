
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class CodeLLamaUnityBridge : MonoBehaviour
{
    public string apiUrl = "http://192.168.1.181:3001/api/code";
    public string  userInput;
    public string path = "Assets/_Documentation";
    public string responseTextUI;


    [ContextMenu("Send Request")]
    public void SendRequestLama()
    {
        StartCoroutine(GetCodeResponse(  userInput, path));
    }

  public IEnumerator GetCodeResponse(string userInput, string documentPath)
{
    Debug.Log("Sending: " + userInput);
    Debug.Log("Using: " + documentPath);
    string documentContent = ReadDocument(documentPath);
    string engineeredPrompt = "please suggest " + userInput + " looking at " + documentContent;
    Debug.Log("EP: " + engineeredPrompt);

    var request = new UnityWebRequest(apiUrl, "POST");
    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes("{\"message\": \"" + engineeredPrompt + "\"}");
    request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
    request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
    request.SetRequestHeader("Content-Type", "application/json");

    yield return request.SendWebRequest();

    if (request.error != null)
    {
        Debug.Log("Error: " + request.error);
        responseTextUI = "Error: " + request.error;  // Display error in the UI
    }
    else
    {
        Debug.Log("Received: " + request.downloadHandler.text);
        string response = request.downloadHandler.text;

        // Process the response to remove the original query
        int indexOfQueryEnd = response.IndexOf(userInput);
        if (indexOfQueryEnd >= 0)
        {
            responseTextUI = response.Substring(indexOfQueryEnd + userInput.Length);
        }
        else
        {
            responseTextUI = response; // In case the query is not found in the response
        }
    }
}

    private string ReadDocument(string path)
    {
        if(File.Exists(path))
        {
            return File.ReadAllText(path);
        }
        return "";
    }
}

