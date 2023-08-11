using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using System.Threading.Tasks;

public class TestRelay : MonoBehaviour
{
    public const int m_MaxPlayers = 4;

    private string m_EnterLobbyCode = "Enter code";

    private string m_LobbyCode = "IF YOU SEE THIS THEN YOU'RE OFFLINE, YARIK FORGOT TO CHANGE UNITY TRANSFORM PROTOCOL TYPE !!!!!!!!!!!!!!!!!!!!!!!!!!! hi yarik :)";

    [SerializeField]
    private GameObject m_NetworkManager;

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private async void CreateRelay()
    {
        try
        { 
            // -1, not including host
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(m_MaxPlayers - 1);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log(joinCode);

            m_LobbyCode = joinCode;

            RelayServerData relayServerData = new RelayServerData(allocation, "udp");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError(ex);
        }
    }

    private async void JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log("Joining relay with " +  joinCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            m_LobbyCode = joinCode;

            RelayServerData relayServerData = new RelayServerData(joinAllocation, "udp");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError(ex);
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(100, 100, 400, 1000));

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Host"))
            {
                if (m_NetworkManager.GetComponent<UnityTransport>().Protocol == UnityTransport.ProtocolType.RelayUnityTransport)
                    CreateRelay();
                else
                {
                    AuthenticationService.Instance.SignOut();

                    NetworkManager.Singleton.StartHost();
                }
            }
            m_EnterLobbyCode = GUILayout.TextField(m_EnterLobbyCode);
            if (GUILayout.Button("Client"))
            {
                if (m_NetworkManager.GetComponent<UnityTransport>().Protocol == UnityTransport.ProtocolType.RelayUnityTransport)
                    JoinRelay(m_EnterLobbyCode);
                else
                {
                    AuthenticationService.Instance.SignOut();

                    NetworkManager.Singleton.StartClient();
                }
            }
        }

        if (NetworkManager.Singleton.IsClient)
        {
            GUILayout.Label(m_LobbyCode);
        }

        GUILayout.EndArea();
    }
}