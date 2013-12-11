﻿
//  Auckland
//  New Zealand
//
//  (c) 2013 VOID
//
//  File Name   :   CDynamicActor.cs
//  Description :   --------------------------
//
//  Author      :  Programming Team
//  Mail        :  contanct@spaceintransit.co.nz
//


// Namespaces
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class CDynamicActor : CNetworkMonoBehaviour 
{
	
// Member Delegates and Events
	public delegate void EnterExitShipHandler();
	
	public event EnterExitShipHandler EventExitedShip;
	public event EnterExitShipHandler EventEnteredShip;
	
	
// Member Fields
	private int m_OriginalLayer = 0;
	private Vector3 m_GravityAcceleration = Vector3.zero;
	
    private CNetworkVar<float> m_cPositionX    = null;
    private CNetworkVar<float> m_cPositionY    = null;
    private CNetworkVar<float> m_cPositionZ    = null;
	
    private CNetworkVar<float> m_EulerAngleX    = null;
    private CNetworkVar<float> m_EulerAngleY    = null;
    private CNetworkVar<float> m_EulerAngleZ    = null;
	
	private CNetworkVar<bool> m_bIsOnboardShip = null;
	private CNetworkVar<bool> m_bIsInBoardingZone = null;
	
// Member Properties	
	public Vector3 GravityAcceleration
	{
		set { m_GravityAcceleration = value; }
		get { return(m_GravityAcceleration); }
	}
	
	public bool IsOnboardShip
	{
		set { m_bIsOnboardShip.Set(value); }
		get { return (m_bIsOnboardShip.Get()); }
	}
	
	public bool IsInBoardingZone
	{
		set { m_bIsInBoardingZone.Set(value); }
		get { return (m_bIsInBoardingZone.Get()); }
	}
	
	public Vector3 Position
    {
        set 
		{ 
			m_cPositionX.Set(value.x); m_cPositionY.Set(value.y); m_cPositionZ.Set(value.z); 
		}
        get 
		{ 
			return (new Vector3(m_cPositionX.Get(), m_cPositionY.Get(), m_cPositionZ.Get())); 
		}
    }
	
	public Vector3 EulerAngles
    {
        set 
		{ 
			m_EulerAngleX.Set(value.x); m_EulerAngleY.Set(value.y); m_EulerAngleZ.Set(value.z);
		}
        get 
		{ 
			return (new Vector3(m_EulerAngleX.Get(), (RotationYDisabled) ? transform.eulerAngles.y: m_EulerAngleY.Get(), m_EulerAngleZ.Get())); 
		}
    }

	public bool RotationYDisabled
	{
		set;
		get;
	}

// Member Methods
	public void Start()
	{
		if(!CNetwork.IsServer)
		{
			rigidbody.isKinematic = true;
		}
		
		EventEnteredShip += new EnterExitShipHandler(TransferActorToShipSpace);
		EventExitedShip += new EnterExitShipHandler(TransferActorToGalaxySpace);
		
		m_OriginalLayer = gameObject.layer;
	}
	
	public void Update()
	{
		if(CNetwork.IsServer)
		{
			//SyncTransform();
		}
	}
	
	public void FixedUpdate()
	{
		if(rigidbody != null && CNetwork.IsServer)
		{	
			// Apply the gravity to the rigid body
			rigidbody.AddForce(m_GravityAcceleration, ForceMode.Acceleration);
		}
	}
	
    public override void InstanceNetworkVars()
    {
		m_cPositionX = new CNetworkVar<float>(OnNetworkVarSync, 0.0f);
		m_cPositionY = new CNetworkVar<float>(OnNetworkVarSync, 0.0f);
		m_cPositionZ = new CNetworkVar<float>(OnNetworkVarSync, 0.0f);
		
        m_EulerAngleX = new CNetworkVar<float>(OnNetworkVarSync, 0.0f);
		m_EulerAngleY = new CNetworkVar<float>(OnNetworkVarSync, 0.0f);
        m_EulerAngleZ = new CNetworkVar<float>(OnNetworkVarSync, 0.0f);
		
		m_bIsOnboardShip = new CNetworkVar<bool>(OnNetworkVarSync, false);
		m_bIsInBoardingZone = new CNetworkVar<bool>(OnNetworkVarSync, false);
	}
	
	public void OnNetworkVarSync(INetworkVar _rSender)
	{
		if(!CNetwork.IsServer)
		{
			// Position
	        if (_rSender == m_cPositionX || _rSender == m_cPositionY || _rSender == m_cPositionZ)
			{
				transform.position = Position;
			}
			
			// Rotation
	        else if (_rSender == m_EulerAngleX || _rSender == m_EulerAngleY || _rSender == m_EulerAngleZ)
	        {	
	            transform.eulerAngles = EulerAngles;
	        }
		}
		
		// Boarding/Unboarding ship
		if(_rSender == m_bIsOnboardShip)
		{
			if(IsOnboardShip)
			{
				if(EventEnteredShip != null)
					EventEnteredShip();
			}
			else
			{
				if(EventExitedShip != null)
					EventExitedShip();
			}
		}
	}
	
	private void SyncTransform()
	{
		Position = rigidbody.position;
		EulerAngles = transform.eulerAngles;
	}
	
	private void TransferActorToGalaxySpace()
	{	 	
		GameObject galaxyShip =  CGame.Ship.GetComponent<CShipGalaxySimulatior>().GalaxyShip;
		bool childOfPlayer = false;
		
		if(transform.parent != null)
			if(transform.parent.tag == "Player")
				childOfPlayer = true;
		
		if(!childOfPlayer)
		{
			// Get the actors position relative to the ship
			Vector3 relativePos = transform.position - CGame.Ship.transform.position;
			Quaternion relativeRot = transform.rotation * Quaternion.Inverse(CGame.Ship.transform.rotation);
			
			// Temporarily parent to galaxy ship
			transform.parent = galaxyShip.transform;
			
			// Update the position and unparent
			transform.localPosition = relativePos;
			transform.localRotation = relativeRot;	
			transform.parent = null;
			
			// Sync over the network
			if(CNetwork.IsServer && GetComponent<CNetworkView>() != null)
			{
				GetComponent<CNetworkView>().SyncParent();
			}
		}
		
		// Resursively set the galaxy layer on the actor
		CUtility.SetLayerRecursively(gameObject, LayerMask.NameToLayer("Galaxy"));
	}
	
	private void TransferActorToShipSpace()
	{
		GameObject galaxyShip = CGame.Ship.GetComponent<CShipGalaxySimulatior>().GalaxyShip;
		bool childOfPlayer = false;
		
		if(transform.parent != null)
			if(transform.parent.tag == "Player")
				childOfPlayer = true;
		
		if(!childOfPlayer)
		{
			// Get the actors position relative to the ship
			Vector3 relativePos = transform.position - galaxyShip.transform.position;
			Quaternion relativeRot = transform.rotation * Quaternion.Inverse(galaxyShip.transform.rotation);
			
			// Parent the actor to the ship
			transform.parent = CGame.Ship.transform;
			
			// Update the position and unparent
			transform.localPosition = relativePos;
			transform.localRotation = relativeRot;	
			
			// Sync over the network
			if(CNetwork.IsServer && GetComponent<CNetworkView>() != null)
			{
				GetComponent<CNetworkView>().SyncParent();
			}
		}
		
		// Resursively set the default layer on the actor
		CUtility.SetLayerRecursively(gameObject, m_OriginalLayer);
	}
}