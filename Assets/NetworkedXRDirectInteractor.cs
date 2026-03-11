using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit.Interactors;

using UnityEngine.XR.Interaction.Toolkit.Interactables;


public class NetworkedXRDirectInteractor : XRDirectInteractor {


   protected new void OnTriggerEnter (Collider col) {

       base.OnTriggerEnter (col) ;

       IXRInteractable interactable ;

       interactionManager.TryGetInteractableForCollider (col, out interactable) ;

       if (interactable != null) {

           attachTransform.SetPositionAndRotation (interactable.transform.position, interactable.transform.rotation) ;

       }

   }


}