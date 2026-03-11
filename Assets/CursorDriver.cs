using UnityEngine;

using Unity.Netcode;

using Unity.Netcode.Components;

using UnityEngine.InputSystem;


public class CursorDriver : NetworkBehaviour {
   private bool active ;
   public GameObject ObjectToCreate;

   private Camera theCamera ;

   private float distanceFromCamera = 0.5f ;



   // Start is called once before the first execution of Update after the MonoBehaviour is created

   public override void OnNetworkSpawn() {
       base.OnNetworkSpawn();
       Debug.Log("CursorDriver: OnNetworkSpawn called, IsOwner=" + IsOwner);
   }

   private void TryCatchCamera() {
       if (theCamera != null) return; // Already have camera

       // Check if our parent (PlayerController) is the owner
       var playerController = GetComponentInParent<PlayerController>();
       var isPlayerOwner = playerController != null && playerController.IsOwner;

       if (isPlayerOwner) {
           // Prefer a camera on the player (parent) so each owner uses their own camera
           theCamera = GetComponentInParent<Camera>();
           if (theCamera == null) {
                Debug.LogWarning("CursorDriver: No camera found on player, falling back to main camera") ;
               theCamera = Camera.main;
           }

           if (theCamera != null) {
               Debug.Log("CursorDriver: Camera found - " + theCamera.gameObject.name);
               active = false;
           }
       }
   }


   // Update is called once per frame

   void Update () {

       // Lazy load camera on first use
       TryCatchCamera();

       // Check if our parent (PlayerController) is the owner
       var playerController = GetComponentInParent<PlayerController>();
       var isPlayerOwner = playerController != null && playerController.IsOwner;

       if (isPlayerOwner && theCamera != null) {

           if (Keyboard.current.leftAltKey.wasPressedThisFrame) {

               active = true ;

           }

           if (Keyboard.current.leftAltKey.wasReleasedThisFrame) {

               active = false ;

           }

           if (active) {

                Vector2 mousePos = Mouse.current.position.ReadValue() ;

                float deltaZ = Mouse.current.scroll.ReadValue().y / 10.0f ;

                // Accumuler les changements de distance
                distanceFromCamera += deltaZ ;

                Vector3 worldPoint = theCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, distanceFromCamera)) ;

                transform.position = worldPoint ;
           }

           // Spawn when P is pressed
           if (isPlayerOwner && Keyboard.current.pKey.wasPressedThisFrame) {
               if (ObjectToCreate != null) {
                   var obj = Instantiate(ObjectToCreate, transform.position, Quaternion.identity);
                   obj.GetComponent<NetworkObject>()?.Spawn();
               } else {
                   Debug.LogWarning("CursorDriver: ObjectToCreate is not assigned!");
               }
           }              

       }

   }

}