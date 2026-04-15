using Unity.Cinemachine; // Note the new namespace in Unity 6
using UnityEngine;

public class PlayerRotationSync : MonoBehaviour {
    public CinemachineCamera vCam; 

    void Update() {
        // Sync the player's Y rotation with the Cinemachine Camera's Pan axis
        float cameraYaw = vCam.State.RawOrientation.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0, cameraYaw, 0);
    }
}