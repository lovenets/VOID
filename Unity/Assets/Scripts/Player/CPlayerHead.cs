//  Auckland
//  New Zealand
//
//  (c) 2013
//
//  File Name   :   CPlayerMotor.cs
//  Description :   --------------------------
//
//  Author  	:  
//  Mail    	:  @hotmail.com
//


// Namespaces
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;


/* Implementation */


public class CPlayerHead : CNetworkMonoBehaviour
{

// Member Types


// Member Delegates & Events


// Member Fields


	List<Type> m_cInputDisableQueue = new List<Type>();
	
	
	CNetworkVar<float> m_fHeadEulerX = null;
	
	
	float m_LocalXRotation = 0.0f;
	
	
	Vector2 m_vCameraMinRotation = new Vector2(-50.0f, -360.0f);
	Vector2 m_vCameraMaxRotation = new Vector2(60.0f, 360.0f);
	Vector2 m_vHeadMinRotation = new Vector2(-30, -60);
	Vector2 m_vHeadMaxRotation = new Vector2(30, 70); 
	
	
	float m_HeadYRotationLimit = 80.0f;


// Member Properties


	public float HeadEulerX
	{
		get { return (m_fHeadEulerX.Get()); }
	}

	public GameObject Head
	{
		get 
        { 
            return (GetComponent<CPlayerInterface>().Model.transform.FindChild("Head").gameObject); 
        }
	}

	public bool InputDisabled
	{
		get { return (m_cInputDisableQueue.Count > 0); }
	}


// Member Methods


    public override void InstanceNetworkVars(CNetworkViewRegistrar _cRegistrar)
    {
		m_fHeadEulerX = _cRegistrar.CreateNetworkVar<float>(OnNetworkVarSync, 0.0f);
    }
	
	
    public void OnNetworkVarSync(INetworkVar _cSyncedNetworkVar)
    {
		// Head Rotation
		if (CGamePlayers.SelfActor != gameObject && 
			_cSyncedNetworkVar == m_fHeadEulerX)
	    {	
	        Head.transform.localEulerAngles = new Vector3(m_fHeadEulerX.Get(), 0.0f, 0.0f);
	    }
    }
	

	public void Start()
	{	
		if(CGamePlayers.SelfActor == gameObject)
		{
			// Setup the game cameras
			CGameCameras.SetupCameras();

			// Setup the HUD
			CGameHUD.SetupHUD();

			// Set the ship view perspective of the camera to the actors head
			TransferPlayerPerspectiveToShipSpace();
			
			// Register event handler for entering/exiting ship
			gameObject.GetComponent<CActorBoardable>().EventBoard += TransferPlayerPerspectiveToShipSpace;
			gameObject.GetComponent<CActorBoardable>().EventDisembark += TransferPlayerPerspectiveToGalaxySpace;

			// Register for mouse movement input
            CUserInput.SubscribeAxisChange(CUserInput.EAxis.MouseY, OnMouseMoveY);

			// Add audoio listener to head
			Head.AddComponent<AudioListener>();
		}
	}

	void OnDestroy()
	{
		// Unregister
		gameObject.GetComponent<CActorBoardable>().EventBoard -= TransferPlayerPerspectiveToShipSpace;
		gameObject.GetComponent<CActorBoardable>().EventDisembark -= TransferPlayerPerspectiveToGalaxySpace;
        CUserInput.UnsubscribeAxisChange(CUserInput.EAxis.MouseY, OnMouseMoveY);
	}

	[ALocalOnly]
	public void DisableInput(object _cFreezeRequester)
	{
		m_cInputDisableQueue.Add(_cFreezeRequester.GetType());
	}


	[ALocalOnly]
	public void ReenableInput(object _cFreezeRequester)
	{
		m_cInputDisableQueue.Remove(_cFreezeRequester.GetType());
	}


	[ALocalOnly]
	public void SetHeadRotations(float _LocalEulerX)
	{
		m_LocalXRotation = _LocalEulerX;

		// Apply the rotation
		Head.transform.localEulerAngles = new Vector3(m_LocalXRotation, 0.0f, 0.0f);
	}


	public static void SerializePlayerState(CNetworkStream _cStream)
	{
		if (CGamePlayers.SelfActor != null)
		{
			// Retrieve my actors head
			CPlayerHead cMyActorHead = CGamePlayers.SelfActor.GetComponent<CPlayerHead>();

			// Write my head's x-rotation
			_cStream.Write(cMyActorHead.m_LocalXRotation);
		}
	}


	public static void UnserializePlayerState(CNetworkPlayer _cNetworkPlayer, CNetworkStream _cStream)
	{
		// Retrieve player actors head
		CPlayerHead cMyActorHead = CGamePlayers.GetPlayerActor(_cNetworkPlayer.PlayerId).GetComponent<CPlayerHead>();

		// Write my head's x-rotation
		float fRotationX = _cStream.Read<float>();

		cMyActorHead.m_fHeadEulerX.Set(fRotationX);
	}


	private void TransferPlayerPerspectiveToShipSpace()
	{
		CGameCameras.SetObserverSpace(true);

		// Remove the galaxy observer component
		Destroy(gameObject.GetComponent<GalaxyObserver>());
	}
	
	private void TransferPlayerPerspectiveToGalaxySpace()
	{
		CGameCameras.SetObserverSpace(false);

		// Add the galaxy observer component
		gameObject.AddComponent<GalaxyObserver>();
	}


	private void OnMouseMoveY(CUserInput.EAxis _eAxis, float _fAmount)
	{
		if (!InputDisabled)
		{
			// Retrieve new rotations
			m_LocalXRotation += _fAmount;

			// Clamp rotation
			m_LocalXRotation = Mathf.Clamp(m_LocalXRotation, m_vCameraMinRotation.x, m_vCameraMaxRotation.x);

			// Apply the pitch to the camera
			SetHeadRotations(m_LocalXRotation);
		}
	}
};
