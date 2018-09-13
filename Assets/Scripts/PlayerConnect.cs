using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerConnect : NetworkBehaviour 
{
	public GameObject playerObject;
	GameObject go;

	void Start()
	{
		if (!isLocalPlayer)
			return;
		
		CmdSpawnPlayers ();
	}

	[Command]
	void CmdSpawnPlayers()
	{
		go = Instantiate (playerObject);
		NetworkServer.SpawnWithClientAuthority(go, connectionToClient);
	}
}
