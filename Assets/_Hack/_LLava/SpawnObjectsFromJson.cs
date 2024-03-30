using UnityEngine;
using System.Collections.Generic;
using System;
[System.Serializable]
public class ObjectDefinition
{
    public float posX;
    public float posY;
    public float posZ;
    public float scaleX;
    public float scaleY;
    public float scaleZ;

    public Vector3 Position => new Vector3(posX, posY, posZ);
    public Vector3 Scale => new Vector3(scaleX, scaleY, scaleZ);
}


[System.Serializable]
public class SerializedObjectDefinitions
{
    public List<ObjectDefinition> definitions;
}

public class SpawnObjectsFromJson : MonoBehaviour
{
    public GameObject prefab; // Assign your prefab in the Unity Inspector
    public TextAsset jsonFile; // Assign your JSON file in the Unity Inspector

    private List<ObjectDefinition> objectDefinitions = new List<ObjectDefinition>();

    [ContextMenu("Instantiate Objects")]
    public void InstantiateObjects()
    {
        if (jsonFile != null)
        {
            ParseJSON(jsonFile.text);

            foreach (var definition in objectDefinitions)
            {
                Vector3 finalPosition = definition.Position; // Now using the converted Vector3
                Vector3 finalScale = definition.Scale; // Now using the converted Vector3


                GameObject obj = Instantiate(prefab, definition.Position, Quaternion.identity);
                obj.transform.localScale = finalScale;
                obj.transform.position = finalPosition;
            }
        }
        else
        {
            Debug.LogError("No JSON file assigned!");
        }
    }

    private void ParseJSON(string jsonString)
    {
        try
        {
            Debug.Log("Attempting to parse JSON: " + jsonString);
            objectDefinitions.Clear();
            SerializedObjectDefinitions deserializedObject = JsonUtility.FromJson<SerializedObjectDefinitions>(jsonString);

            if (deserializedObject == null)
            {
                Debug.LogError("Deserialized object is null");
                return;
            }

            if (deserializedObject.definitions == null)
            {
                Debug.LogError("Deserialized object's definitions are null");
                return;
            }

            objectDefinitions.AddRange(deserializedObject.definitions);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error parsing JSON: " + ex.Message);
        }
    }

}
