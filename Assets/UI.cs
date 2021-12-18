using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UNET;
using UnityEngine;

public class UI : MonoBehaviour
{
    public void OnStartHostButtonPressed()
    {
        NetworkManager.Singleton.StartHost();
    }

    public void OnStartClientButtonPressed()
    {
        //var unet = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetComponent<UNetTransport>();
        NetworkManager.Singleton.StartClient();
    }
}
