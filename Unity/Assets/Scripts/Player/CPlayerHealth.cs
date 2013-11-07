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

public class CPlayerHealth : MonoBehaviour 
{

    // Member Types


    // Member Delegates & Events


    // Member Properties


    // Member Functions


    // Get player HP from XML
    public float m_fActorHp = 100.0f;
	public bool m_bIsAlive = true;
	void Start () 
    {
	
	}

    public void OnDestroy()
    { 
    }

	// Update is called once per frame
	void Update () {
	    // Get the player health from the XML
		if (CNetwork.IsServer)
        {
			if(m_fActorHp <= 0.0f)
			{
				if(m_bIsAlive == true)
				{
					Destroy(gameObject.GetComponent<CharacterController>());
					gameObject.GetComponent<CPlayerBodyMotor>().enabled = false;
					gameObject.GetComponent<CPlayerHeadMotor>().enabled = false;
					gameObject.AddComponent<CapsuleCollider>();
					Rigidbody ActorDeathBooody = gameObject.AddComponent<Rigidbody>();
				
					ActorDeathBooody.AddTorque(new Vector3(1.0f, 1.0f, 100.0f) );
					m_bIsAlive = false;
				}
			}
		}
		else
		{
			
		// Do nothing at the moment
		}
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


    public void ApplyDamage(float _fDamage, float _fPlayerHealth)
    {
        m_fActorHp = _fPlayerHealth - _fDamage;
    }
    // Member Fields
	
}

