using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

public class VRConnectionManager : MonoBehaviour
{
    private int _maxPlayers = 10;
    private ISession _session;
    private NetworkManager m_NetworkManager;

    private enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
    }

    private ConnectionState _state = ConnectionState.Disconnected;

    private async void Awake()
    {
        m_NetworkManager = GetComponent<NetworkManager>();

        m_NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;
        m_NetworkManager.OnSessionOwnerPromoted += OnSessionOwnerPromoted;

        await UnityServices.InitializeAsync();
    }

    private void OnSessionOwnerPromoted(ulong sessionOwnerPromoted)
    {
        if (m_NetworkManager.LocalClient.IsSessionOwner)
        {
            Debug.Log($"Client-{m_NetworkManager.LocalClientId} is the session owner!");
        }
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        if (m_NetworkManager.LocalClientId == clientId)
        {
            Debug.Log($"Client-{clientId} joined the shared world.");
        }
    }

    private void OnDestroy()
    {
        _session?.LeaveAsync();
    }

    // =========================
    // NEW METHOD FOR VR
    // =========================

public void JoinDefaultWorld()
{
    JoinSharedWorld("VRUser", "SharedSession");
}

public async void JoinSharedWorld(string userName, string sessionName)
{
    if (_state != ConnectionState.Disconnected)
        return;

    _state = ConnectionState.Connecting;

    try
    {
        AuthenticationService.Instance.SwitchProfile(userName);
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        var options = new SessionOptions()
        {
            Name = sessionName,
            MaxPlayers = _maxPlayers
        }.WithDistributedAuthorityNetwork();

        _session = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionName, options);

        _state = ConnectionState.Connected;

        Debug.Log("Connected to shared world");
    }
    catch (Exception e)
    {
        _state = ConnectionState.Disconnected;
        Debug.LogException(e);
    }
}
}