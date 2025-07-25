// using System;
// using System.Collections.Generic;
// using System.Net.Security;
// using System.Threading.Tasks;
// using QFSW.QC;
// using Unity.Services.Authentication;
// using Unity.Services.Core;
// using Unity.Services.Lobbies;
// using Unity.Services.Lobbies.Models;
// using UnityEngine;

// public class Lobby : MonoBehaviour
// {
//     private Unity.Services.Lobbies.Models.Lobby hostLobby;
//     private float hearBeatTimer;
//     private Unity.Services.Lobbies.Models.Lobby lobby;
//     private async void Start()
//     {
//         await UnityServices.InitializeAsync();

//         AuthenticationService.Instance.SignedIn += () =>
//         {
//             Debug.Log("sign in " + AuthenticationService.Instance.PlayerId);
//         };
//         // vào nhanh như kiểu random phòng r vào
//         await AuthenticationService.Instance.SignInAnonymouslyAsync();

//     }

//     private void Update()
//     {
//         HandleLobbyHeartBeat();
//     }

//     private async void HandleLobbyHeartBeat()
//     {
//         if (hostLobby != null)
//         {
//             hearBeatTimer -= Time.deltaTime;
//             if (hearBeatTimer < 0f)
//             {
//                 float hearBeatTimerMax = 15;
//                 hearBeatTimer = hearBeatTimerMax;

//                 await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
//             }
//         }
//     }

//     [Command]
//     private async void CreateLobby()
//     {
//         try
//         {
//             string lobbyName = "hey";
//             int maxPlayer = 4;
//             // var option = new CreateLobbyOptions();
//             // option.IsPrivate = true;
//             lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayer);
//             hostLobby = lobby;
//         }
//         catch (LobbyServiceException e)
//         {
//             Debug.LogError(e);
//         }
//     }
//     [Command]
//     private async void ListLobbies()
//     {
//         try
//         {
//             QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
//             {
//                 Count = 25,
//                 Filters = new List<QueryFilter>
//                 {
//                     new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
//                 },
//                 Order = new List<QueryOrder>
//                 {
//                     new QueryOrder(false, QueryOrder.FieldOptions.Created)
//                 },
//             };

//             QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();

//             Debug.Log("Lobbies found " + queryResponse.Results);
//             foreach (Unity.Services.Lobbies.Models.Lobby lobby in queryResponse.Results)
//             {
//                 Debug.Log(lobby.Name + " " + lobby.MaxPlayers);
//             }
//         }
//         catch (LobbyServiceException e)
//         {
//             Debug.LogError(e);
//         }
//     }

//     [Command]
//     private async void joinLobby()
//     {
//         try
//         {

//             QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();

//             await LobbyService.Instance.JoinLobbyByIdAsync(queryResponse.Results[0].Id);
//         }
//         catch (LobbyServiceException e)
//         {
//             Debug.LogError(e);
//         }
//     }
// }
