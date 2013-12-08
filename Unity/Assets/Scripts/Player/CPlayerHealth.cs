//  Auckland
//  New Zealand
//
//  (c) 2013
//
//  File Name   :   CActorHealth.cs
//  Description :   --------------------------
//
//  Author  	:  Scott Emery
//  Mail    	:  scott.ipod@gmail.com
//


// Namespaces
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* Implementation */

public class CPlayerHealth : CNetworkMonoBehaviour
{

    // Member Types


    // Member Delegates & Events
	public delegate void OnPlayerApplyHeal(GameObject _TargetPlayer, float _fHealAmount);
	public delegate void OnPlayerApplyDamage(GameObject _TargetPlayer, float _fDamageAmount);
	public delegate void OnPlayerDeath(GameObject _SourcePlayer);
	public delegate void OnPlayerRevive(GameObject _SourcePlayer);
	
	public event OnPlayerApplyHeal   EventOnPlayerApplyHeal;
	public event OnPlayerApplyDamage EventOnPlayerApplyDamage;
	public event OnPlayerDeath       EventOnPlayerDeath;
	public event OnPlayerRevive      EventOnPlayerRevive;

    // Member Properties
	public float Health
	{ 
		get { return (m_fActorHp.Get()); }
		set { m_fActorHp.Set(value); }
	}
	
	public bool Alive
	{
		get { return (m_bIsAlive.Get()); }
		set { m_bIsAlive.Set(value); }
	}
	
	public float Breath
	{
		get { return(m_fOxygenUseRate.Get()); }
		set { m_fOxygenUseRate.Set(value); }
	}

    // Member Functions
	public override void InstanceNetworkVars()
	{
		m_fActorHp = new CNetworkVar<float>(OnNetworkVarSync, k_fMaxHealth);
		m_bIsAlive = new CNetworkVar<bool>(OnNetworkVarSync, true);
		m_fOxygenUseRate = new CNetworkVar<float>(OnNetworkVarSync, 5.0f);
	}

	
	void Start() 
    {
		AudioCue[] audioCues = gameObject.GetComponents<AudioCue>();
		foreach(AudioCue cue in audioCues)
		{
			if(cue.m_strCueName == "LaughTrack")
			{
				m_LaughTrack = 	cue;
			}
		}
				
		/*
		//Set everything to kinematic
		foreach(Transform child in transform.GetComponentsInChildren<Transform>())
		{		
			if(child.GetComponent<Rigidbody>() != null)
			{
				Rigidbody childRigidBody = child.GetComponent<Rigidbody>();
				childRigidBody.isKinematic = true;
			}
			
			if(child.GetComponent<CapsuleCollider>() != null)
			{
				CapsuleCollider childCapCollider = child.GetComponent<CapsuleCollider>();
				childCapCollider.isTrigger = true;	
			}
			
			if(child.GetComponent<BoxCollider>() != null)
			{
				BoxCollider childBoxCollider = child.GetComponent<BoxCollider>();
				childBoxCollider.isTrigger = true;	
			}			
		}
		
		// Change the parents values back
		//rigidbody.isKinematic = false;* */
	}
		 

    public void OnDestroy()
    { 
    }

	// Update is called once per frame
	void Update () 
	{
			// Below needs to be syncronized to the clients not only the server. Otherwise the clients will never get these changes.
			// Use CNetworkVars to do this - not RPC calls. As RPC calls will not be queued up when a new player joins.
			

			
			// CPlayerCamera only exists on the CLIENT player actor. This will only exist on the server if this gameObject == CGame.PlayerActor.
			// In other words the below stuff needs to be syncronized to each client indivisually. 
			// See InvokeRPC() and CNetworkView.ViewID to send just to the lient to modify their camera.
			
			/*
			Camera headCam = transform.GetComponent<CPlayerHeadMotor>().ActorHead.GetComponent<CPlayerCamera>().camera;
			Vector3 pos = headCam.transform.localPosition;
			pos.z -= 0.05f;
			headCam.transform.localPosition = pos;
			*/					

			// Same deal as above
			
			/*
			transform.GetComponent<CPlayerHeadMotor>().ActorHead.GetComponent<CPlayerCamera>().camera.transform.LookAt(transform);
			*/
			
		
			// Do nothing at the moment
		
	        // Check if the player is in harmful atmosphere leading to suffocation
	            // Decrease stamina first and incrimenting rate
	            // Then decrease health
	
	        // Check if player has been shot
	            // Bullet does initial amount of damage but also applies more over time.
	            // Head shot is 80-105% damage
	            // Heart damage is 50% damage 50% damage applied over time
	            // Knee damage is 10% but movement slowed 
	
	        // Physical trauma of a projectile
	            // Damage applied to player in relevence to the velocity travelling
	            
	        // Check health points
	
	        // Send a complete loss of health to manager that handles death screen
		
	}

	void OnGUI()
	{
		const float kHealthBarMargin = 10.0f;
		const float kHealthBarLength = 150.0f;
		const float kHealthBarHeight = 20.0f;


		if (gameObject == CGame.PlayerActor)
			GUI.Box(new Rect(Screen.width - kHealthBarLength - kHealthBarMargin, 
							 Screen.height - kHealthBarHeight - kHealthBarMargin,
							 kHealthBarLength, kHealthBarHeight), 
							 "Health: " + m_fActorHp.Get() + "/" + k_fMaxHealth);
	}

	void OnNetworkVarSync(INetworkVar _cVarInstance)
	{
		if(_cVarInstance == m_bIsAlive)
		{
			foreach(Transform child in transform.GetComponentsInChildren<Transform>())
			{
				Rigidbody rigidBody;
				if(child.GetComponent<Rigidbody>() != null)
				{
					rigidBody = child.GetComponent<Rigidbody>();
					rigidBody.isKinematic = false;	
				}
				
				CapsuleCollider capCollider;
				if(child.GetComponent<CapsuleCollider>() != null)
				{
					capCollider = child.GetComponent<CapsuleCollider>();
					capCollider.isTrigger = false;	
				}
				
				BoxCollider boxCollider;
				if(child.GetComponent<BoxCollider>() != null)
				{
					boxCollider = child.GetComponent<BoxCollider>();
					boxCollider.isTrigger = false;	
				}				
			}
		
			transform.GetComponent<CPlayerMotor>().enabled = false;
			transform.GetComponent<CPlayerHead>().enabled = false;			
			
		}
		
		if(_cVarInstance == m_fActorHp)
		{
			m_LaughTrack.Play(1.0f, false, -1);
			
			if(CNetwork.IsServer)
			{
				if(m_fActorHp.Get() <= 0.0f)
				{
					m_bIsAlive.Set(false);
				}
			}
		}
	}
		
    public void ApplyDamage(float _fDamage)
	{
		// Event callback for damage application
		if (EventOnPlayerApplyDamage != null)
		{
			EventOnPlayerApplyDamage(gameObject, _fDamage);
		}
		
        m_fActorHp.Set(m_fActorHp.Get() - _fDamage);
    }
	
	public void ApplyHeal(float _fHeal)
	{
		// Event callback for heal applciation
		if (EventOnPlayerApplyHeal != null)
		{
			EventOnPlayerApplyHeal(gameObject, _fHeal);
		}
	}
	
	public void PlayerDied()
	{
		// Event callback for player death
		if (EventOnPlayerDeath != null)
		{
			EventOnPlayerDeath(gameObject);
		}
	}
	
	public void PlayerRevived()
	{
		// Event callback for player revived
		if (EventOnPlayerRevive != null)
		{
			EventOnPlayerRevive(gameObject);	
		}
	}
	
	public void OnOxygenChange(float _fOxygenAmount)
	{
		
	}
	
    // Member Fields

	const float k_fMaxHealth = 100.0f;

	CNetworkVar<float> m_fActorHp;
	CNetworkVar<float> m_fOxygenUseRate;
	CNetworkVar<bool> m_bIsAlive;
	AudioCue m_LaughTrack;
}

