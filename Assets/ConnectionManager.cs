using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
   private string _profileName;
   private string _sessionName;
   private int _maxPlayers = 10;
   private ConnectionState _state = ConnectionState.Disconnected;
   private ISession _session;
   private NetworkManager m_NetworkManager;

   private enum ConnectionState
   {
       Disconnected,
       Connecting,
       Connected,
   }

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
            Debug.Log($"Client-{clientId} is connected and can spawn {nameof(NetworkObject)}s.");
        }
    }

   private void OnGUI()
   {
       if (_state == ConnectionState.Connected)
        return;

    // Agrandit toute l'interface (change 2f → 3f si encore trop petit)
    GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * 2f);

    GUILayout.BeginArea(new Rect(30, 30, 400, 200));

    GUI.enabled = _state != ConnectionState.Connecting;

    GUILayout.Label("Profile Name");
    _profileName = GUILayout.TextField(_profileName, GUILayout.Height(40));

    GUILayout.Space(20);

    GUILayout.Label("Session Name");
    _sessionName = GUILayout.TextField(_sessionName, GUILayout.Height(40));

    GUILayout.Space(30);

    GUI.enabled = GUI.enabled && 
                  !string.IsNullOrEmpty(_profileName) && 
                  !string.IsNullOrEmpty(_sessionName);

    if (GUILayout.Button("Create or Join Session", GUILayout.Height(50)))
    {
        _ = CreateOrJoinSessionAsync();
    }
    GUILayout.EndArea();

   }

   private void OnDestroy()
   {
       _session?.LeaveAsync();
   }

   private async Task CreateOrJoinSessionAsync()
   {
       _state = ConnectionState.Connecting;

       try
       {
           AuthenticationService.Instance.SwitchProfile(_profileName);
           await AuthenticationService.Instance.SignInAnonymouslyAsync();

            var options = new SessionOptions() {
                Name = _sessionName,
                MaxPlayers = _maxPlayers
            }.WithDistributedAuthorityNetwork();

            _session = await MultiplayerService.Instance.CreateOrJoinSessionAsync(_sessionName, options);

           _state = ConnectionState.Connected;
       }
       catch (Exception e)
       {
           _state = ConnectionState.Disconnected;
           Debug.LogException(e);
       }
   }
}