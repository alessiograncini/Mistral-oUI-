using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UrlLoader : MonoBehaviour
{
    public string imageUrl = "https://www.google.com/maps"; // Replace with your image URL
    public Image targetImage; // Assign your UI Image component here in the Inspector

    void Start()
    {
        StartCoroutine(DownloadImage(imageUrl));
    }

    IEnumerator DownloadImage(string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to download image: " + request.error);
        }
        else
        {
            // Get the downloaded image
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            // Convert the Texture2D into a Sprite
            Sprite downloadedSprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );
            // Set the downloaded sprite to the Image component
            targetImage.sprite = downloadedSprite;
        }
    }
}
