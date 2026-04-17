using Unity.Cinemachine; // Note the new namespace in Unity 6
using UnityEngine;

public class PlayerRotationSync : MonoBehaviour {
    public CinemachineCamera vCam; 

    void Start() {
        // Initialization if needed
    }

    void LateUpdate() {
        if (vCam == null) return;

        // 1. Sync the player's Y rotation with the Cinemachine Camera's Pan axis
        float cameraYaw = vCam.State.RawOrientation.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0, cameraYaw, 0);

        // 2. FORCE the vCam to stay centered within the player capsule
        // This prevents the camera from "poking" out of the capsule if there's any 
        // procedural bobbing or offset on the vCam transform.
        Vector3 localPos = vCam.transform.localPosition;
        if (localPos.z > 0) localPos.z = 0f; // Pull back to capsule center
        localPos.x = 0f; // Center horizontally
        vCam.transform.localPosition = localPos;
    }
}