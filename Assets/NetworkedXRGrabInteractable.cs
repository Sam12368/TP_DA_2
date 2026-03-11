using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.Netcode;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(NetworkObject))]
public class NetworkedXRGrabInteractable : XRGrabInteractable
{
    [Header("Visuals")]
    [SerializeField] private Color catchableColor = Color.cyan;
    [SerializeField] private Color caughtColor = Color.yellow;

    private Color initialColor;
    protected Rigidbody rb;
    protected Renderer colorRenderer;
    protected bool caught = false;

    NetworkObject netObjectComponent;
    NetworkedGrabRpcHandler rpcHandler;

    protected override void Awake()
    {
        base.Awake();
        colorRenderer = GetComponentInChildren<Renderer>();
        if (colorRenderer != null) initialColor = colorRenderer.material.color;
        rb = GetComponent<Rigidbody>();
        netObjectComponent = GetComponent<NetworkObject>();

        // ensure an RPC handler exists on this object
        rpcHandler = GetComponent<NetworkedGrabRpcHandler>();
        if (rpcHandler == null)
            rpcHandler = gameObject.AddComponent<NetworkedGrabRpcHandler>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        hoverEntered.AddListener(OnHoverEnteredLocal);
        hoverExited.AddListener(OnHoverExitedLocal);
        selectEntered.AddListener(OnSelectEnteredLocal);
        selectExited.AddListener(OnSelectExitedLocal);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        hoverEntered.RemoveListener(OnHoverEnteredLocal);
        hoverExited.RemoveListener(OnHoverExitedLocal);
        selectEntered.RemoveListener(OnSelectEnteredLocal);
        selectExited.RemoveListener(OnSelectExitedLocal);
    }

    // XR event handlers (Renamed to avoid hiding base virtual methods)
    private void OnHoverEnteredLocal(HoverEnterEventArgs args) => LocalShowCatchable();
    private void OnHoverExitedLocal(HoverExitEventArgs args) => LocalHideCatchable();
    private void OnSelectEnteredLocal(SelectEnterEventArgs args) => LocalCatch();
    private void OnSelectExitedLocal(SelectExitEventArgs args) => LocalRelease();

    // Local-called methods (invoked from XR events)
    public virtual void LocalCatch()
    {
        if (caught) return;

        if (netObjectComponent != null && !netObjectComponent.IsOwner)
        {
            // Demander la propriété de l'objet au serveur via notre handler RPC
            rpcHandler?.RequestOwnership();
            StartCoroutine(WaitForOwnershipAndCatch());
            return;
        }

        CatchLocal();
        rpcHandler?.NotifyShowCaught();
    }

    IEnumerator WaitForOwnershipAndCatch()
    {
        float timeout = 5f;
        float start = Time.time;
        
        // Attendre que le serveur nous accorde la propriété
        while (netObjectComponent != null && !netObjectComponent.IsOwner && Time.time - start < timeout)
            yield return null;

        if (netObjectComponent != null && netObjectComponent.IsOwner)
        {
            CatchLocal();
            rpcHandler?.NotifyShowCaught();
        }
        else
        {
            Debug.LogWarning("Failed to get ownership before timeout");
        }
    }

    // local physics/state change
    protected virtual void CatchLocal()
    {
        if (rb != null) rb.isKinematic = true;
        caught = true;
        ApplyShowCaughtVisual();
    }

    // Local release
    public virtual void LocalRelease()
    {
        if (!caught) return;

        ReleaseLocal();
        rpcHandler?.NotifyShowReleased();
    }

    protected virtual void ReleaseLocal()
    {
        if (rb != null) rb.isKinematic = false;
        caught = false;
        ApplyShowReleasedVisual();
    }

    // Local hover visuals
    public void LocalShowCatchable()
    {
        ApplyShowCatchableVisual();
        rpcHandler?.NotifyShowCatchable();
    }

    public void LocalHideCatchable()
    {
        ApplyHideCatchableVisual();
        rpcHandler?.NotifyHideCatchable();
    }

    // Visual appliers (local)
    public void ApplyShowCaughtVisual()
    {
        if (colorRenderer != null) colorRenderer.material.color = caughtColor;
    }

    public void ApplyShowReleasedVisual()
    {
        if (colorRenderer != null) colorRenderer.material.color = catchableColor;
    }

    public void ApplyShowCatchableVisual()
    {
        if (colorRenderer != null) colorRenderer.material.color = catchableColor;
    }

    public void ApplyHideCatchableVisual()
    {
        if (colorRenderer != null) colorRenderer.material.color = initialColor;
    }
}

// Network RPC handler
public class NetworkedGrabRpcHandler : NetworkBehaviour
{
    // Public wrappers
    public void NotifyShowCaught() => RequestShowCaughtRpc();
    public void NotifyShowReleased() => RequestShowReleasedRpc();
    public void NotifyShowCatchable() => RequestShowCatchableRpc();
    public void NotifyHideCatchable() => RequestHideCatchableRpc();

    // Logique pour s'attribuer la propriété de l'objet
    public void RequestOwnership()
    {
        if (IsServer)
        {
            GetComponent<NetworkObject>().ChangeOwnership(NetworkManager.Singleton.LocalClientId);
        }
        else
        {
            RequestOwnershipRpc();
        }
    }

    // La nouvelle API utilise InvokePermission.Everyone au lieu de RequireOwnership = false
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestOwnershipRpc(RpcParams rpcParams = default)
    {
        var netObj = GetComponent<NetworkObject>();
        if (netObj != null)
        {
            // Le serveur change le propriétaire de l'objet pour celui qui a fait la requête
            netObj.ChangeOwnership(rpcParams.Receive.SenderClientId);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestShowCaughtRpc(RpcParams rpcParams = default)
    {
        ShowCaughtRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestShowReleasedRpc(RpcParams rpcParams = default)
    {
        ShowReleasedRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestShowCatchableRpc(RpcParams rpcParams = default)
    {
        ShowCatchableRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestHideCatchableRpc(RpcParams rpcParams = default)
    {
        HideCatchableRpc();
    }

    // SendTo.Everyone remplace l'ancien attribut [ClientRpc]
    [Rpc(SendTo.Everyone)]
    private void ShowCaughtRpc(RpcParams rpcParams = default)
    {
        if (IsOwner) return;
        var comp = GetComponent<NetworkedXRGrabInteractable>();
        comp?.ApplyShowCaughtVisual();
    }

    [Rpc(SendTo.Everyone)]
    private void ShowReleasedRpc(RpcParams rpcParams = default)
    {
        if (IsOwner) return;
        var comp = GetComponent<NetworkedXRGrabInteractable>();
        comp?.ApplyShowReleasedVisual();
    }

    [Rpc(SendTo.Everyone)]
    private void ShowCatchableRpc(RpcParams rpcParams = default)
    {
        if (IsOwner) return;
        var comp = GetComponent<NetworkedXRGrabInteractable>();
        comp?.ApplyShowCatchableVisual();
    }

    [Rpc(SendTo.Everyone)]
    private void HideCatchableRpc(RpcParams rpcParams = default)
    {
        if (IsOwner) return;
        var comp = GetComponent<NetworkedXRGrabInteractable>();
        comp?.ApplyHideCatchableVisual();
    }
}