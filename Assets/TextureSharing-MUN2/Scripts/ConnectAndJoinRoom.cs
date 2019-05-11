using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MonobitEngine;

public class ConnectAndJoinRoom : MonobitEngine.MonoBehaviour
{
    [SerializeField] string gameVersion = "v1.0";
    [SerializeField] string roomName = "AutoLoginRoom";

	public void Awake()
    {
		if (!MonobitNetwork.isConnect)
        {
    		MonobitNetwork.autoJoinLobby = true;
			MonobitNetwork.ConnectServer(gameVersion);
        }
    }

    void OnConnectedToMonobit()
    {
        Debug.Log("OnConnectedToMonobit");
    }

    void OnConnectToServerFailed(DisconnectCause cause)
    {
        Debug.Log("OnConnectToServerFailed cause="+cause);
    }

    void OnJoinedLobby()
    {
        Debug.Log("OnJoinedLobby");
        MonobitNetwork.JoinOrCreateRoom(roomName, new RoomSettings(), LobbyInfo.Default);
    }

    void OnJoinRoomFailed()
    {
        Debug.Log("OnJoinRoomFailed");
    }

    void OnJoinedRoom()
    {
        Debug.Log("OnJoinedRoom");
    }

    void OnDisconnectedFromServer()
    {
        Debug.Log("OnDisconnectedFromServer");
    }
}
