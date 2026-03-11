using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkTransform
{
    public Vector3 cameraPositionOffset = new Vector3 (0, 1.6f, 0) ;

    public Quaternion cameraOrientationOffset = new Quaternion () ;

    protected Transform cameraTransform ;

    protected Camera theCamera ;

    

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        CatchCamera();
    }

    public void CatchCamera () {
        Debug.Log("PlayerController: CatchCamera called, IsOwner=" + IsOwner + ", IsLocalPlayer=" + IsLocalPlayer) ;
        if (!IsOwner) {
            Debug.Log("PlayerController: Not the owner, skipping camera setup");
            return;
        }

        Debug.Log("PlayerController: IsOwner, attaching camera") ;

        // attach the camera to the navigation rig
        
        //theCamera = (Camera)GameObject.FindFirstObjectByType (typeof(Camera)) ;
        // --> instead, get the child camera 
        theCamera = GetComponentInChildren<Camera>(true);
        if (theCamera == null) { 
            Debug.LogError("Camera not found in Player prefab!"); 
            return; 
        }

        Debug.Log("Camera found: " + theCamera.gameObject.name + ", currently active: " + theCamera.gameObject.activeSelf);

        // Activate the camera GameObject and all its parents
        theCamera.gameObject.SetActive(true);
        theCamera.enabled = true; // only enable the camera for the local player

        Debug.Log("Camera enabled. Active now: " + theCamera.gameObject.activeSelf + ", Camera.enabled: " + theCamera.enabled);

        cameraTransform = theCamera.transform ;

        cameraTransform.SetParent (transform) ;

        cameraTransform.localPosition = cameraPositionOffset ;

        cameraTransform.localRotation = cameraOrientationOffset ;

        Debug.Log("Camera setup complete");
    }

       private void Update()
    {
        if (!IsSpawned || !HasAuthority)
            return;

        float x = (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f);
        float z = (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f);
        
        x *= Time.deltaTime * 150.0f;
        z *= Time.deltaTime * 3.0f;

        transform.Rotate(0, x, 0);
        transform.Translate(0, 0, z);
    }


    

    
}
