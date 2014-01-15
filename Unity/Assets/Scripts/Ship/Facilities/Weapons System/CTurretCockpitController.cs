//  Auckland
//  New Zealand
//
//  (c) 2013
//
//  File Name   :   CTurretCockpitController.cs
//  Description :   --------------------------
//
//  Author  	:  
//  Mail    	:  @hotmail.com
//


// Namespaces
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


/* Implementation */


[RequireComponent(typeof(CCockpit))]
public class CTurretCockpitController : CNetworkMonoBehaviour
{

// Member Types


// Member Delegates & Events


// Member Properties


	public GameObject ActiveTurretObject
	{
		get { return (CNetwork.Factory.FindObject(m_cTurretViewId.Get())); }
	}


// Member Methods
	

	public override void InstanceNetworkVars()
    {
		m_cTurretViewId = new CNetworkVar<CNetworkViewId>(OnNetworkVarSync, null);
    }

	
	public void OnNetworkVarSync(INetworkVar _cSyncedVar)
	{
		if (_cSyncedVar == m_cTurretViewId)
		{
			// Check is connected to turret
			if (m_cTurretViewId.Get() != null)
			{
				// Check player is in cockpit
				CCockpit cCockpit = gameObject.GetComponent<CCockpit>();
				if (cCockpit.MountedPlayerId != 0)
				{
					if (CNetwork.IsServer)
					{
						// Notify turret that it has been mounted by player
						ActiveTurretObject.GetComponent<CTurretController>().Mount(cCockpit.MountedPlayerId);
					}
				}

				// There is no player in the cockpit
				else
				{
					if (CNetwork.IsServer)
					{
						// Notify turret that it has been mounted by player
						ActiveTurretObject.GetComponent<CTurretController>().Unmount();
					}
				}
			}
		}
	}


	public void Start()
	{
		CCockpit cCockpit = gameObject.GetComponent<CCockpit>();

		// Subscribe to cockpit events
		cCockpit.EventPlayerEnter += new CCockpit.HandlePlayerEnter(OnPlayerEnterCockpit);
		cCockpit.EventPlayerLeave += new CCockpit.HandlePlayerLeave(OnPlayerLeaveCockpit);
	}


	public void OnDestroy()
	{
		// Empty
	}


	public void Update()
	{
		// Empty
	}


	[AServerMethod]
	[AClientMethod]
	void OnPlayerEnterCockpit(ulong _ulPlayerId)
	{
		// Debug - Search for avaiable turret nodes from parent
		if (CNetwork.IsServer)
		{	
			List<GameObject> unmountedTurrets = transform.parent.GetComponent<CFacilityTurrets>().GetAllUnmountedTurrets();

			if(unmountedTurrets.Count != 0)
			{
				m_cTurretViewId.Set(unmountedTurrets[0].GetComponent<CNetworkView>().ViewId);
			}
		}
	}


	[AServerMethod]
	[AClientMethod]
	void OnPlayerLeaveCockpit(ulong _ulPlayerId)
	{
		// Debug - Set default turret
		if (CNetwork.IsServer)
		{
			ActiveTurretObject.GetComponent<CTurretController>().Unmount();
			m_cTurretViewId.Set(null);
		}
	}


// Member Fields


	CNetworkVar<CNetworkViewId> m_cTurretViewId = null;

	
	float m_fFireTimer		= 0.0f;
	float m_fFireInterval	= 0.2f;


	bool m_bUpdateRotation = false;



};
