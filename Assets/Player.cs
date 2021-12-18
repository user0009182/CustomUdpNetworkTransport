using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    NetworkVariable<Color> playerColor = new NetworkVariable<Color>();
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            //random hue
            playerColor.Value = Random.ColorHSV(0, 1, 1, 1, 1, 1);
            transform.position = Random.insideUnitSphere * 5;
            transform.position -= new Vector3(0, transform.position.y, 0);
        }

        SetColor(playerColor.Value);
    }

    void SetColor(Color color)
    {
        GetComponent<MeshRenderer>().material.color = color;
    }

    void OnEnable()
    {
        //detect changes (from the server) to this player's color 
        playerColor.OnValueChanged = new NetworkVariable<Color>.OnValueChangedDelegate(OnColorChanged);
    }

    void OnColorChanged(Color oldColor, Color newColor)
    {
        SetColor(newColor);
    }

    void Update()
    {
        if (IsOwner && IsClient)
        {
            ClientUpdate();
        }

        if (IsServer)
        { 
            ServerUpdate();
        }
    }

    void ServerUpdate()
    {
        transform.position += serverVelocity * Time.deltaTime;
    }

    void ClientUpdate()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ChangeMyColorServerRpc("456");
        }

        var velocity = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            velocity.z += 1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            velocity.x -= 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            velocity.z -= 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            velocity.x += 1;
        }
        SetPlayerVelocityServerRpc(velocity);
    }

    [ServerRpc()]
    void ChangeMyColorServerRpc(string data)
    {
        playerColor.Value = Random.ColorHSV(0, 1, 1, 1, 1, 1);
    }

    Vector3 serverVelocity;

    [ServerRpc()]
    void SetPlayerVelocityServerRpc(Vector3 delta)
    {
        serverVelocity = delta * 10; 
        //GetComponent<NetworkTransform>().po

        //PlayerColor.Value = Random.ColorHSV(0, 1, 1, 1, 1, 1);
    }
}
