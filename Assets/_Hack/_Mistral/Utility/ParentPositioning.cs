using UnityEngine;

public class ParentPositioning : MonoBehaviour
{
    public Transform ParentToPosition;
    public Transform Controller;

    // Update is called once per frame
    public void PositionParent()
    {
        ParentToPosition.position = Controller.position;

        // Assuming Controller.rotation gives you the rotation of the controller
        Quaternion controllerRotation = Controller.rotation;

        // Extract the Y-axis rotation from the controller's rotation
        float yRotation = controllerRotation.eulerAngles.y;

        // Create a new Quaternion with only the Y-axis rotation
        ParentToPosition.rotation = Quaternion.Euler(0, yRotation, 0);

    }
}
