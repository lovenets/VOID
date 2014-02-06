﻿//  Auckland
//  New Zealand
//
//  (c) 2013
//
//  File Name   :   CRachetBehaviour.cs
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

[RequireComponent(typeof(CToolInterface))]
public class CModuleGunBehaviour : CNetworkMonoBehaviour
{
	
	// Member Types
	
	
	// Member Delegates & Events


	// Member Fields
	public Transform m_InactiveUITransform = null;
	public GameObject m_DUI = null;
	public float m_UITransitionTime = 0.5f;

	private CToolInterface m_ToolInterface = null;
	private CDUIModuleCreationRoot m_DUIModuleCreationRoot = null;

	private bool m_Transitioning = false;
	private Vector3 m_ActivatedPosition = Vector3.zero;

	private CNetworkVar<bool> m_DUIActive = null;


	// Member Properties
	public bool IsDUIActive
	{
		get { return(m_DUIActive.Get()); }
	}
	
	// Member Methods
	public override void InstanceNetworkVars(CNetworkViewRegistrar _cRegistrar)
	{
		m_DUIActive = _cRegistrar.CreateNetworkVar<bool>(OnNetworkVarSync, false);
	}
	
	
	public void OnNetworkVarSync(INetworkVar _cSyncedVar)
	{
		if (_cSyncedVar == m_DUIActive)
		{
			if (IsDUIActive)
			{
				ActivateDUI();
			}
			else
			{
				DeactivateDUI();
			}
		}
	}
	
	public void Start()
	{
		// Register the interaction events
		m_ToolInterface = gameObject.GetComponent<CToolInterface>();
		m_ToolInterface.EventPrimaryActivate += OnPrimaryStart;
		m_ToolInterface.EventSecondaryActivate += OnSecondaryStart;

		// Register DUI events
		m_DUIModuleCreationRoot = m_DUI.GetComponent<CDUIConsole>().DUI.GetComponent<CDUIModuleCreationRoot>();
		m_DUIModuleCreationRoot.EventBuildModuleButtonPressed += OnDUIBuildButtonPressed;

		// Configure DUI
		m_DUI.transform.position = m_InactiveUITransform.position;
		m_DUI.transform.rotation = m_InactiveUITransform.rotation;
		m_DUI.transform.localScale = m_InactiveUITransform.localScale;
	}

	public void Update()
	{
		UpdateDUITransform();
	}
	
	private void ActivateDUI()
	{
		Transform head = m_ToolInterface.OwnerPlayerActor.GetComponent<CPlayerHead>().ActorHead.transform;

		Vector3 toPos = head.position + (head.forward * 1.0f);
		Quaternion toRot = Quaternion.LookRotation((toPos - head.position).normalized);
		Vector3 toScale = Vector3.one;

		this.StartCoroutine(InterpolateUIActive(toPos, toRot, toScale));
	}
	
	private void DeactivateDUI()
	{
		this.StartCoroutine(InterpolateUIInActive());
	}
	
	private void UpdateDUITransform()
	{
		if(IsDUIActive && !m_Transitioning)
		{
			// Maintain the same postion
			m_DUI.transform.position = m_ActivatedPosition;

			// Rotate towards head
			Transform head = m_ToolInterface.OwnerPlayerActor.GetComponent<CPlayerHead>().ActorHead.transform;
			Quaternion toRot = Quaternion.LookRotation((m_ActivatedPosition - head.position).normalized);
			m_DUI.transform.rotation = toRot;
		}
	}

	[AServerOnly]
	private void OnPrimaryStart(GameObject _InteractableObject)
	{
		if(_InteractableObject != null && !IsDUIActive && !m_Transitioning)
		{
			// Only conserned with selecting module ports
			CModulePortInterface mpi = _InteractableObject.GetComponent<CModulePortInterface>();
			if(mpi != null)
			{
				// Register movement events
				CUserInput.SubscribeClientInputChange(CUserInput.EInput.MoveGround_Forward, OnPlayerMovement);
				CUserInput.SubscribeClientInputChange(CUserInput.EInput.MoveGround_Backwards, OnPlayerMovement);
				CUserInput.SubscribeClientInputChange(CUserInput.EInput.MoveGround_StrafeLeft, OnPlayerMovement);
				CUserInput.SubscribeClientInputChange(CUserInput.EInput.MoveGround_StrafeRight, OnPlayerMovement);
				CUserInput.SubscribeClientInputChange(CUserInput.EInput.MoveGround_Jump, OnPlayerMovement);

				// Change the port selected on the UI
				m_DUIModuleCreationRoot.SetSelectedPort(_InteractableObject.GetComponent<CNetworkView>().ViewId);

				// Make the UI active
				m_DUIActive.Set(true);
			}
		}
	}

	[AServerOnly]
	private void OnPlayerMovement(CUserInput.EInput _eInput, ulong _ulPlayerId, bool _bDown)
	{
		// Turn off the DUI from any movement
		if(!m_Transitioning)
			m_DUIActive.Set(false);
	}

	[AServerOnly]
	private void OnSecondaryStart(GameObject _InteractableObject)
	{
		// Turn off DUI when secondary is used
		if(!m_Transitioning)
			m_DUIActive.Set(false);
	}

	[AServerOnly]
	private void OnDUIBuildButtonPressed()
	{
		CModulePortInterface currentPort = m_DUIModuleCreationRoot.CurrentPortSelected.GetComponent<CModulePortInterface>();

		// Debug: Create the module instantly
		currentPort.CreateModule(m_DUIModuleCreationRoot.SelectedModuleType);

		// Deactivate the UI
		m_DUIActive.Set(false);
	}

	private IEnumerator InterpolateUIActive(Vector3 _ToPostion, Quaternion _ToRotation, Vector3 _ToScale)
	{
		float timer = 0.0f;

		bool lerping = true;
		m_Transitioning = true;
		while(lerping)
		{
			timer += Time.deltaTime;
			if(timer > m_UITransitionTime)
			{
				timer = m_UITransitionTime;
				lerping = false;
			}

			float lerpValue = timer/m_UITransitionTime;

			m_DUI.transform.position = Vector3.Lerp(m_InactiveUITransform.position, _ToPostion, lerpValue);
			m_DUI.transform.rotation = Quaternion.Slerp(m_InactiveUITransform.rotation, _ToRotation, lerpValue);
			m_DUI.transform.localScale = Vector3.Lerp(m_InactiveUITransform.localScale, _ToScale, lerpValue);

			if(!lerping)
			{
				m_Transitioning = false;

				// Set the position where it is activated
				m_ActivatedPosition = _ToPostion;
			}

			yield return null;
		}


	}

	private IEnumerator InterpolateUIInActive()
	{
		float timer = 0.0f;
		
		bool lerping = true;
		m_Transitioning = true;
		while(lerping)
		{
			timer += Time.deltaTime;
			if(timer > m_UITransitionTime)
			{
				timer = m_UITransitionTime;
				lerping = false;
			}
			
			float lerpValue = timer/m_UITransitionTime;
			
			m_DUI.transform.position = Vector3.Lerp(m_DUI.transform.position, m_InactiveUITransform.position, lerpValue);
			m_DUI.transform.rotation = Quaternion.Slerp(m_DUI.transform.rotation, m_InactiveUITransform.rotation, lerpValue);
			m_DUI.transform.localScale = Vector3.Lerp(m_DUI.transform.localScale, m_InactiveUITransform.localScale, lerpValue);

			if(!lerping)
			{
				m_Transitioning = false;

				// Unregister movement events
				if(CNetwork.IsServer)
				{
					CUserInput.UnsubscribeClientInputChange(CUserInput.EInput.MoveGround_Forward, OnPlayerMovement);
					CUserInput.UnsubscribeClientInputChange(CUserInput.EInput.MoveGround_Backwards, OnPlayerMovement);
					CUserInput.UnsubscribeClientInputChange(CUserInput.EInput.MoveGround_StrafeLeft, OnPlayerMovement);
					CUserInput.UnsubscribeClientInputChange(CUserInput.EInput.MoveGround_StrafeRight, OnPlayerMovement);
					CUserInput.UnsubscribeClientInputChange(CUserInput.EInput.MoveGround_Jump, OnPlayerMovement);
				}
			}

			yield return null;
		}
	}
};
