﻿//  Auckland
//  New Zealand
//
//  (c) 2013
//
//  File Name   :   CPlayerIKController
//  Description :   Controls player IK, allowing a rigged character to place body parts at specified locations
//
//  Author      :  
//  Mail        :  @hotmail.com
//


// Namespaces
using UnityEngine;
using System.Collections;

/* Implementation */

public class CPlayerRagdoll : CNetworkMonoBehaviour 
{
    //Member Types
    public enum ERagdollState : byte
    {
        Invalid,

        PlayerDown,
        PlayerRevive,

        Max,
    }
    
    //Member Delegates & Events   
    
    //Member Properties  
    
    //Member variables

    public Transform m_RootSkeleton = null;
   
    public GameObject m_PlayerHead = null;
    public GameObject m_RagdollHead = null;

    private Vector3 m_initialOffset = new Vector3(0,0,0);

    CNetworkVar<byte>       m_bRagdollState;

    static CNetworkStream   s_cSerializeStream = new CNetworkStream();  

    public override void InstanceNetworkVars(CNetworkViewRegistrar _cRegistrar)
    {
       m_bRagdollState = _cRegistrar.CreateNetworkVar<byte>(OnNetworkVarSync);
    }
    
    void OnNetworkVarSync(INetworkVar _cSyncedNetworkVar)
    { 
        if (_cSyncedNetworkVar == m_bRagdollState)
        {
            switch((ERagdollState) m_bRagdollState.Get())
            {
                case ERagdollState.PlayerDown:
                {
                    SetRagdollActive();                    
                    break;                
                }
                
                case ERagdollState.PlayerRevive:
                {
                    DeactivateRagdoll();                
                    break;                
                }
            }
        }
    } 

    [AClientOnly]
    public static void Serialize(CNetworkStream _cStream)
    {
        // Write in internal stream
        _cStream.Write(s_cSerializeStream);
        s_cSerializeStream.Clear();        
    }
    
    [AServerOnly]
    public static void Unserialize(CNetworkPlayer _cNetworkPlayer, CNetworkStream _cStream)
    {
        //Extract network action
        ERagdollState eNetworkAction = (ERagdollState)_cStream.ReadByte();
        
        switch (eNetworkAction)
        {
            case ERagdollState.PlayerDown:
            {
                ulong uPlayerID = _cStream.ReadULong();

                GameObject cPlayerActor = CGamePlayers.GetPlayerActor(uPlayerID);
                CPlayerRagdoll ragdoll = cPlayerActor.GetComponent<CPlayerRagdoll>();

                ragdoll.m_bRagdollState.Set((byte)eNetworkAction);

                break;
            }
            case ERagdollState.PlayerRevive:
            {
                ulong uPlayerID = _cStream.ReadULong();
                
                GameObject cPlayerActor = CGamePlayers.GetPlayerActor(uPlayerID);
                CPlayerRagdoll ragdoll = cPlayerActor.GetComponent<CPlayerRagdoll>();

                ragdoll.m_bRagdollState.Set((byte)eNetworkAction);

                break;
            }
        }
    }
    
    // Use this for initialization
    void Start ()
	{
        SetKinematicRagdoll();		
        SetRagdollLayer();

        m_initialOffset = transform.localPosition;
        gameObject.rigidbody.isKinematic = false;

        gameObject.GetComponent<CPlayerHealth>().EventHealthStateChanged += OnHealthStateChanged;
    }
	
	// Update is called once per frame
	void Update () 
	{
        //Constrain the skeleton to the player
        if ((ERagdollState)m_bRagdollState.Get() == ERagdollState.PlayerDown)
        {
           // transform.position = m_RootSkeleton.position;
        }
	}

    [AServerOnly]
    void OnHealthStateChanged(GameObject _SourcePlayer, CPlayerHealth.HealthState _eHealthCurrentState, CPlayerHealth.HealthState _eHealthPreviousState)
	{  
        switch (_eHealthCurrentState)
        {
            case CPlayerHealth.HealthState.DOWNED:
            {
                s_cSerializeStream.Write((byte)ERagdollState.PlayerDown);
                //Send in player ID
                s_cSerializeStream.Write((ulong)CGamePlayers.GetPlayerActorsPlayerId(_SourcePlayer));

                break;
            }           
            case CPlayerHealth.HealthState.ALIVE:
            {           
                s_cSerializeStream.Write((byte)ERagdollState.PlayerRevive);
                //Send in player ID
                s_cSerializeStream.Write((ulong)CGamePlayers.GetPlayerActorsPlayerId(_SourcePlayer));
                break;
            }
        }       
    }

    [AClientOnly]
    void SetRagdollActive()
    {       
        //Enable ragdoll and set position
        SetDynamicRagdoll();     

        //Transfer camera pos
        if (gameObject == CGamePlayers.SelfActor)
        {
            gameObject.GetComponent<CPlayerHead>().ActorHead = m_RagdollHead;        
            gameObject.GetComponent<CPlayerHead>().TransferPlayerPerspectiveToShipSpace();;
        }

    }

    [AClientOnly]
    void DeactivateRagdoll()
    {         
        //Disable ragdoll and set position
        SetKinematicRagdoll();

        if (gameObject == CGamePlayers.SelfActor)
        {
            gameObject.GetComponent<CPlayerHead>().ActorHead = m_PlayerHead;        
            gameObject.GetComponent<CPlayerHead>().TransferPlayerPerspectiveToShipSpace();;
        }
    }

    void SetKinematicRagdoll()
    {
        Transform[] ragdollBones = m_RootSkeleton.GetComponentsInChildren<Transform>();

        foreach (Transform body in ragdollBones)
        {
            if(body.gameObject.GetComponent<Rigidbody>())
            {
                body.rigidbody.velocity = new Vector3(0, 0, 0);
                body.rigidbody.isKinematic = true;
                body.rigidbody.useGravity = false;
                body.rigidbody.mass = 9.0f;
            }
            if(body.gameObject.GetComponent<Collider>())
            {
                body.collider.enabled = false;
            }
        } 

        //gameObject.collider.enabled = true;     
     
        Debug.LogWarning("Ragdoll deactivated!");
    }

    void SetDynamicRagdoll()
    {
        Transform[] ragdollBones = m_RootSkeleton.GetComponentsInChildren<Transform>();

        foreach (Transform body in ragdollBones)
        {
            if(body.gameObject.GetComponent<Rigidbody>())
            {
                body.rigidbody.isKinematic = false;
                body.rigidbody.velocity = rigidbody.velocity;
            }
            if(body.gameObject.GetComponent<Collider>())
            {
                body.collider.enabled = true;
            }
        }      

        //gameObject.collider.enabled = false;
        Debug.LogWarning("Ragdoll activated!");
    }

    void SetRagdollLayer()
    {
        Transform[] ragdollBones = m_RootSkeleton.GetComponentsInChildren<Transform>();

        foreach (Transform body in ragdollBones)
        {
            body.gameObject.layer = 14;
        }
    }
}
