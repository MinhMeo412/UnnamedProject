using System.Threading.Tasks;
using QFSW.QC;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode.Components;
using UnityEngine;
using Unity.Netcode;
using Unity.Networking.Transport.Relay;

public class Multiplayer : NetworkBehaviour
{
    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Sign in " + AuthenticationService.Instance.PlayerId);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    [Command]
    private async void CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);

            string joincode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log(joincode);

            // AllocationUtils.ToRelayServerData(allocation, "wss");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                AllocationUtils.ToRelayServerData(allocation, "wss"));


            // NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
            //     allocation.RelayServer.IpV4,
            //     (ushort)allocation.RelayServer.Port,
            //     allocation.AllocationIdBytes,
            //     allocation.Key,
            //     allocation.ConnectionData
            // );
            NetworkManager.Singleton.StartHost();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
        }
    }

    [Command]
    private async void JoinRelay(string code)
    {
        try
        {
            Debug.Log("Joining Relay with " + code);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);

            // NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
            //     joinAllocation.RelayServer.IpV4,
            //     (ushort)joinAllocation.RelayServer.Port,
            //     joinAllocation.AllocationIdBytes,
            //     joinAllocation.Key,
            //     joinAllocation.ConnectionData,
            //     joinAllocation.HostConnectionData
            // );

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                AllocationUtils.ToRelayServerData(joinAllocation, "wss"));
            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
        }
    }
}

