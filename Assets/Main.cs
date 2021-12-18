using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Main : MonoBehaviour
{
    void Start()
    {
        Application.wantsToQuit += Application_wantsToQuit;
        NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;
    }

    private bool Application_wantsToQuit()
    {
        NetworkManager.Singleton.Shutdown();
        return true;
    }

    private void Singleton_OnClientDisconnectCallback(ulong clientId)
    {
        Debug.Log($"client disconnected {clientId}"); 
    }

    private void Singleton_OnClientConnectedCallback(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer && NetworkManager.Singleton.ServerClientId == clientId)
        {
            Debug.Log("connected to self as client");
        }
        else
        {
            Debug.Log("remote client connected " + clientId);
        }
    }
}
