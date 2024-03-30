using UnityEngine;
using System.Collections;

public class BentoBoxPopulator : MonoBehaviour
{
    public RectTransform moduleArea; // Assign in inspector
    public GameObject modulePrefab; // Assign in inspector
    public int maxModules = 10; // Max number of modules to instantiate
    public float spacing; // Spacing between modules
    public Vector2 minSize; // Minimum size for the module instances
    public Vector2 maxSize; // Maximum size for the module instances
    public float resizeDuration = 1.0f; // Duration for the resize animation

    private void Start()
    {
        PopulateArea();
    }

   [ContextMenu("Populate Area")]
    private void PopulateArea()
    {
        // Clear previous children
        foreach (Transform child in moduleArea)
        {
            Destroy(child.gameObject);
        }

        // Calculate starting positions
        float startX = -moduleArea.rect.width / 2 + minSize.x / 2;
        float startY = moduleArea.rect.height / 2 - minSize.y / 2;
        
        for (int i = 0; i < maxModules; i++)
        {
            // Instantiate a new module
            GameObject newModule = Instantiate(modulePrefab, moduleArea);
            RectTransform rectTransform = newModule.GetComponent<RectTransform>();

            // Set initial size of the module to minSize
            rectTransform.sizeDelta = minSize;

            // Calculate random size for the new module
            Vector2 targetSize = new Vector2(
                Random.Range(minSize.x, maxSize.x),
                Random.Range(minSize.y, maxSize.y)
            );

            // Calculate position for the new module
            int row = i / Mathf.FloorToInt(moduleArea.rect.width / (maxSize.x + spacing));
            int column = i % Mathf.FloorToInt(moduleArea.rect.width / (maxSize.x + spacing));
            float posX = startX + column * (maxSize.x + spacing);
            float posY = startY - row * (maxSize.y + spacing);

            // Set the position of the module
            rectTransform.anchoredPosition = new Vector2(posX, posY);

            // Start resize animation
            StartCoroutine(ResizeModule(rectTransform, targetSize));

            // Break if the next module would exceed the area height
            if ((row + 1) * (maxSize.y + spacing) > moduleArea.rect.height)
                break;
        }
    }

    private IEnumerator ResizeModule(RectTransform moduleTransform, Vector2 targetSize)
    {
        float time = 0;
        Vector2 originalSize = moduleTransform.sizeDelta;

        while (time < resizeDuration)
        {
            // Linear interpolation from the original size to the target size over the duration
            moduleTransform.sizeDelta = Vector2.Lerp(originalSize, targetSize, time / resizeDuration);
            time += Time.deltaTime;
            yield return null;
        }

        // Ensure the final size is exactly the target size
        moduleTransform.sizeDelta = targetSize;
    }
}
