//  Auckland
//  New Zealand
//
//  (c) 2013
//
//  File Name   :   CLaserProjeCTile.cs
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


[RequireComponent(typeof(CNetworkView))]
public class CLaserProjeCTile : CNetworkMonoBehaviour
{

// Member Types


// Member Delegates & Events


// Member Properties


// Member Methods


	static CLaserProjeCTile()
	{
		s_iEnemyLayer   = 0;
	}


	public override void InstanceNetworkVars(CNetworkViewRegistrar _cRegistrar)
	{
        _cRegistrar.RegisterRpc(this, "CreateHitParticles");
	}
	
	public void OnNetworkVarSync(INetworkVar _rSender)
	{

   	}

	public void Start()
	{
		m_vInitialPosition = transform.position;

		// Precalculate velocity
		Vector3 velocity = transform.forward * m_InitialProjectileSpeed;
		
		// Add the relative velocity from the ship
		velocity += CGameShips.ShipGalaxySimulator.GetGalaxyVelocityRelativeToShip(transform.position);

		// Add to the rigid body
		rigidbody.AddForce(velocity, ForceMode.VelocityChange);
	}


	public void Update()
	{
		if (!m_bDestroyed)
		{
			// Life timer
			m_fLifeTimer -= Time.deltaTime;

			if (m_fLifeTimer < 0.0f)
			{
				m_bDestroyed = true;
			}
		}
		else if(CNetwork.IsServer)
		{
			CNetwork.Factory.DestoryObject(GetComponent<CNetworkView>().ViewId);
		}
	}

	[AServerOnly]
	void OnCollisionEnter(Collision _cCollision) 
	{
		if (!m_bDestroyed && CNetwork.IsServer)
		{
			m_bDestroyed = true;

			//InvokeRpc(0, "CreateHitParticles", _cCollision.contacts[0].point, Quaternion.LookRotation(transform.position - _cCollision.transform.position));

			InvokeRpcAll("CreateHitParticles", _cCollision.contacts[0].point, Quaternion.LookRotation(transform.position - _cCollision.transform.position));
		}
	}

	[ANetworkRpc]
	void CreateHitParticles(Vector3 _HitPos, Quaternion _HitRot)
	{
		// Create hit particles
		string prefabFile = CNetwork.Factory.GetRegisteredPrefabFile(CGameRegistrator.ENetworkPrefab.LaserHitParticles);
		GameObject cHitParticles = (GameObject)GameObject.Instantiate(Resources.Load("Prefabs/" + prefabFile));
		
		cHitParticles.transform.position = _HitPos;
		cHitParticles.transform.rotation = _HitRot;

		// Destroy particles are 1 second
		GameObject.Destroy(cHitParticles, cHitParticles.particleSystem.duration);
	}


// Member Fields


	public float m_InitialProjectileSpeed = 500.0f;
	Vector3 m_vInitialPosition;


	float m_fLifeTimer = 5.0f;


	bool m_bDestroyed = false;

	
	static int s_iEnemyLayer = 0;
};
