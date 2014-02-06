//  Auckland
//  New Zealand
//
//  (c) 2013
//
//  File Name   :   CToolInterface.cs
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

//This Script contains all the tool info, such as:
//		an enum for what type of tool the tool is
//		an ammo counter?
//		slots taken up
//		
//This Script can:
//		Get the type of tool
//		Shoot the Tool
//		Reload the Tool


[RequireComponent(typeof(CActorInteractable))]
[RequireComponent(typeof(CActorBoardable))]
[RequireComponent(typeof(CActorGravity))]
public class CToolInterface : CNetworkMonoBehaviour
{

// Member Types


	public enum EType
	{
		INVALID,

		Ratchet,
		CircuitryKit,
		Calibrator,
		Fluidizer,
		ModuleCreator,
		FireExtinguisher,
		Norbert,
		HealingKit,
		AK47,

		MAX
	}

	public enum ENetworkAction : byte
	{
		PickUp,
	}


// Member Delegates & Events
	public delegate void NotifyObjectInteraction(GameObject _TargetInteractableObject);
	public delegate void NotifyToolInteraction();

	public event NotifyObjectInteraction EventPrimaryActivate;
	public event NotifyObjectInteraction EventPrimaryDeactivate;
	public event NotifyObjectInteraction EventSecondaryActivate;
	public event NotifyObjectInteraction EventSecondaryDeactivate;
	public event NotifyObjectInteraction EventUse;
	
	public event NotifyToolInteraction EventReload;
	public event NotifyToolInteraction EventPickedUp;
	public event NotifyToolInteraction EventDropped;


// Member Properties


	public EType ToolType
	{
		get{ return(m_ToolType); }
	}


    public GameObject OwnerPlayerActor
    {
		get
		{
			if (!IsHeld) 
				return null; 

			return (CGamePlayers.FindPlayerActor(OwnerPlayerId)); 
		}
    }


	public ulong OwnerPlayerId
	{
		get { return (m_ulOwnerPlayerId.Get()); }
	}


    public bool IsHeld
    {
		get { return (m_ulOwnerPlayerId.Get() != 0); }
    }


// Member Functions


    public override void InstanceNetworkVars(CNetworkViewRegistrar _cRegistrar)
    {
        m_ulOwnerPlayerId = _cRegistrar.CreateNetworkVar<ulong>(OnNetworkVarSync, 0);
    }


    public void OnNetworkVarSync(INetworkVar _cVarInstance)
    {
        if (_cVarInstance == m_ulOwnerPlayerId)
        {
			if (IsHeld)
            {
                GameObject cOwnerPlayerActor = OwnerPlayerActor;
              
                Transform[]children = OwnerPlayerActor.GetComponentsInChildren<Transform>();
                foreach(Transform child in children)
                {
                    if(child.name == "RightHandIndex2")
                    {
                        gameObject.transform.parent = child;
                    }
                }     

                if(gameObject.transform.parent.gameObject == null)
                {
                    Debug.LogError("Could not find right hand transform of player model!");
                }

                gameObject.transform.localPosition = new Vector3(0.0f,0.0f,0.0f);
              
                // Turn off dynamic physics
				if(CNetwork.IsServer)
				{
                	rigidbody.isKinematic = true;
					rigidbody.detectCollisions = false;
				}

				// Stop recieving syncronizations
                GetComponent<CActorNetworkSyncronized>().m_SyncPosition = false;
				GetComponent<CActorNetworkSyncronized>().m_SyncRotation = false;
            }
            else
            {
                gameObject.transform.parent = null;

                // Turn on dynamic physics
				if(CNetwork.IsServer)
				{
					rigidbody.isKinematic = false;
					rigidbody.detectCollisions = true;
				}
				
                rigidbody.AddForce(transform.forward * 5.0f, ForceMode.VelocityChange);
                rigidbody.AddForce(Vector3.up * 5.0f, ForceMode.VelocityChange);

				// Recieve syncronizations
				GetComponent<CActorNetworkSyncronized>().m_SyncPosition = true;
				GetComponent<CActorNetworkSyncronized>().m_SyncRotation = true;
            }
        }
    }


	public void Awake()
    {
		// Empty
	}


	public void Start()
	{
		// Empty
	}


	public void OnDestroy()
	{
		// Empty
	}


	public void Update()
	{
		if (IsHeld)
        { 
            Transform ActorHead = OwnerPlayerActor.GetComponent<CPlayerHead>().ActorHead.transform;
            gameObject.transform.rotation = ActorHead.rotation;
        }
	}


	[AServerOnly]
	public void SetPrimaryActive(bool _bActive, GameObject _cInteractableObject)
	{
		// Check not already active
		if (_bActive &&
			!m_bPrimaryActive)
		{
			// Set active
			m_bPrimaryActive = true;

			// Notify observers
			if (EventPrimaryActivate != null)
			{
				EventPrimaryActivate(_cInteractableObject);
			}
		}

		// Check currently active
		else if (!_bActive &&
				 m_bPrimaryActive)
		{
			// Set deactive
			m_bPrimaryActive = false;

			// Notify observers
			if (EventPrimaryDeactivate != null)
			{
				EventPrimaryDeactivate(_cInteractableObject);
			}
		}
	}


	public static void RegisterPrefab(EType _ToolType, CGameRegistrator.ENetworkPrefab _ePrefab)
	{
		s_mRegisteredPrefabs.Add(_ToolType, _ePrefab);
	}
	
	
	public static CGameRegistrator.ENetworkPrefab GetPrefabType(EType _ToolType)
	{
		if (!s_mRegisteredPrefabs.ContainsKey(_ToolType))
		{
			Debug.LogError(string.Format("Tool type ({0}) has not been registered a prefab", _ToolType));
			
			return (CGameRegistrator.ENetworkPrefab.INVALID);
		}
		
		return (s_mRegisteredPrefabs[_ToolType]);
	}


	[AServerOnly]
	public void SetSecondaryActive(bool _bActive, GameObject _cInteractableObject)
	{
		// Check not already active
		if (_bActive &&
			!m_bSecondaryActive)
		{
			// Set active
			m_bSecondaryActive = true;

			// Notify observers
			if (EventSecondaryActivate != null)
			{
				EventSecondaryActivate(_cInteractableObject);
			}
		}

		// Check currently active
		else if (!_bActive &&
		         m_bSecondaryActive)
		{
			// Set deactive
			m_bSecondaryActive = false;

			// Notify observers
			if (EventSecondaryDeactivate != null)
			{
				EventSecondaryDeactivate(_cInteractableObject);
			}
		}
	}


	[AServerOnly]
	public void PickUp(ulong _ulPlayerId)
	{
		Logger.WriteErrorOn(!CNetwork.IsServer, "Only servers are allow to invoke this method");

		if (!IsHeld)
		{
            // Set owner player
			m_ulOwnerPlayerId.Set(_ulPlayerId);

            // Notify observers
            if (EventPickedUp != null)
            {
                EventPickedUp();
            }
		}
	}


	[AServerOnly]
	public void Drop()
	{
		Logger.WriteErrorOn(!CNetwork.IsServer, "Only servers are allow to invoke this method");

		// Check currently held
		if (IsHeld)
		{
            // Set owning object view id
            m_ulOwnerPlayerId.Set(0);

            // Notify observers
            if (EventDropped != null)
            {
                EventDropped();
            }
		}
	}


	[AServerOnly]
	public void Reload()
	{
		Logger.WriteErrorOn(!CNetwork.IsServer, "Only servers are allow to invoke this method");

		// Check currently held
		if (IsHeld)
		{
			// Notify observers
			if (EventReload != null)
			{
				EventReload();
			}
		}
	}


	[AServerOnly]
	public void Use(GameObject _cInteractableObject)
	{
		// Check currently held
		if (IsHeld)
		{
			// Notify observers
			if (EventUse != null)
			{
				EventUse(_cInteractableObject);
			}
		}
	}


// Member Fields


	public EType m_ToolType = EType.INVALID;


    CNetworkVar<ulong> m_ulOwnerPlayerId = null;


    bool m_bPrimaryActive = false;
    bool m_bSecondaryActive = false;


	static Dictionary<EType, CGameRegistrator.ENetworkPrefab> s_mRegisteredPrefabs = new Dictionary<EType, CGameRegistrator.ENetworkPrefab>();


};
