//  Auckland
//  New Zealand
//
//  (c) 2013
//
//  File Name   :   CPlayerBelt.cs
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


public class CPlayerBelt : CNetworkMonoBehaviour
{

// Member Types


    const uint k_uiMaxNumTools = 3;


    public enum ENetworkAction
    {
		ActivateToolPrimary,
		DeactivateToolPrimary,
		ActivateToolSecondary,
		DeactivateToolSeconary,
		PickupTool,
		UseTool,
		ChangeTool,
		ReloadActiveTool,
        DropActiveTool
    }


// Member Delegates & Events
	
	
// Member Properties


	public GameObject ActiveTool
	{
		get 
		{
			if (ActiveToolViewId != 0)
			{
				return (CNetwork.Factory.FindObject(ActiveToolViewId));
			}

			return (null);
		}
	}


	public byte ActiveSlotId
	{
		get { return (m_bActiveToolId.Get()); }
	}


	public ushort ActiveToolViewId
	{
		get { return (m_ausToolsViewId[ActiveSlotId].Get()); }
	}


	public byte ToolCapacity
	{
		get { return (m_bToolCapacity.Get()); }
	}
	
	
// Member Functions


	static CPlayerBelt()
	{
		s_aeSelectToolKeys = new KeyCode[3] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3 };
	}


    public override void InstanceNetworkVars()
    {
		m_ausToolsViewId = new CNetworkVar<ushort>[k_uiMaxNumTools];

		for (uint i = 0; i < k_uiMaxNumTools; ++i)
		{
			m_ausToolsViewId[i] = new CNetworkVar<ushort>(OnNetworkVarSync);
		}

		m_bToolCapacity = new CNetworkVar<byte>(OnNetworkVarSync, 2);
		m_bActiveToolId = new CNetworkVar<byte>(OnNetworkVarSync);
    }


	public void OnNetworkVarSync(INetworkVar _cVarInstance)
	{
		for (uint i = 0; i < k_uiMaxNumTools; ++ i)
		{
			if (m_ausToolsViewId[i] == _cVarInstance)
			{
				ushort usNewValue = m_ausToolsViewId[i].Get();
			}
		}
	}


	public void Start()
	{
        gameObject.GetComponent<CPlayerInteractor>().EventInteraction += new CPlayerInteractor.HandleInteraction(OnInteraction);
		gameObject.GetComponent<CPlayerInteractor>().EventNoInteraction += new CPlayerInteractor.HandleNoInteraction(OnNoInteraction);
		gameObject.GetComponent<CNetworkView>().EventPreDestory += new CNetworkView.NotiftyPreDestory(OnPreDestroy);
	}


	public void OnPreDestroy()
	{
        if (CNetwork.IsServer)
        {
            for (uint i = 0; i < k_uiMaxNumTools; ++i)
            {
				if (m_ausToolsViewId[i].Get() != 0)
				{
					GetTool(i).GetComponent<CToolInterface>().Drop();
				}
            }
        }
	}


	public void Update()
	{
		// Empty
	}


	[AServerMethod]
	public void SetToolPrimaryActive(byte _bSlotId, bool _bActive, GameObject _cInteractableObject)
	{
		// Check tool exists
		if (GetToolViewId(_bSlotId) != 0)
		{
			GetTool(_bSlotId).GetComponent<CToolInterface>().SetPrimaryActive(_bActive, _cInteractableObject);

            //Debug.Log(string.Format("Set primary active({0})", _bActive));
		}
	}


	[AServerMethod]
	public void SetToolSecondaryActive(byte _bSlotId, bool _bActive, GameObject _cInteractableObject)
	{
		// Check tool exists
		if (GetToolViewId(_bSlotId) != 0)
		{
			GetTool(_bSlotId).GetComponent<CToolInterface>().SetSecondaryActive(_bActive, _cInteractableObject);

            //Debug.Log(string.Format("Set secondary active({0})", _bActive));
		}
	}


	[AServerMethod]
	void PickupTool(ulong _ulPlayerId, GameObject _cInteractableObject)
	{
		// Check object exists
		if (_cInteractableObject != null)
		{
			// Find free slot
			for (uint i = 0; i < ToolCapacity; ++i)
			{
				if (GetToolViewId(i) == 0)
				{
					// Retrieve tool interface script
					CToolInterface cToolInterface = _cInteractableObject.GetComponent<CToolInterface>();
					CNetworkView cToolNetworkView = _cInteractableObject.GetComponent<CNetworkView>();

					// Check script found
					if (cToolInterface == null)
					{
						Debug.LogError(string.Format("Target tool does not have the CToolInterface component attached! ObjectName({0})", _cInteractableObject.name));
					}
					else
					{
						m_ausToolsViewId[i].Set(cToolNetworkView.ViewId);
						cToolInterface.PickUp(_ulPlayerId);
						ChangeTool((byte)i);
						Debug.Log(string.Format("Picked up tool. PlayerId({0}) ToolObjectName({1}) SlotId({2})", _ulPlayerId, _cInteractableObject.name, i));
					}

					break;
				}
			}
		}
	}


	[AServerMethod]
	public void UseTool(byte _bSlotId, GameObject _cInteractableObject)
	{
		// Check tool exists
		if (GetToolViewId(_bSlotId) != 0)
		{
			GetTool(_bSlotId).GetComponent<CToolInterface>().Use(_cInteractableObject);
		}
	}


	[AServerMethod]
	public void ChangeTool(byte _bSlotId)
	{
		// Check tool exists
		if (GetToolViewId(_bSlotId) != 0)
		{
			m_bActiveToolId.Set(_bSlotId);

            //Debug.Log(string.Format("Changing tool to SlotId({0})", _bSlotId));
		}
	}


	[AServerMethod]
	public void ReloadTool(byte _bSlotId)
	{
		// Check tool exists
		if (GetToolViewId(_bSlotId) != 0)
		{
			GetTool(_bSlotId).GetComponent<CToolInterface>().Reload();
			Debug.Log("Reloading active tool");
		}
	}


	[AServerMethod]
	public void DropTool(byte _bSlotId)
	{
		// Check tool exists
		if (GetToolViewId(_bSlotId) != 0)
		{
			GetTool(_bSlotId).GetComponent<CToolInterface>().Drop();
            m_ausToolsViewId[_bSlotId].Set(0);

            // Change tool to next available tool
            for (int i = (int)k_uiMaxNumTools - 1; i >= 0; --i)
            {
                if (m_ausToolsViewId[i].Get() != 0)
                {
                    ChangeTool((byte)i);
                    break;
                }
            }

            //Debug.Log("Dropping active tool");
		}
	}


	public GameObject GetTool(uint _bSlotId)
	{
		return (CNetwork.Factory.FindObject(GetToolViewId(_bSlotId)));
	}


	public ushort GetToolViewId(uint _bSlotId)
	{
		return (m_ausToolsViewId[_bSlotId].Get());
	}


	[AClientMethod]
    void OnInteraction(CPlayerInteractor.EInteractionType _eType, GameObject _cInteractableObject, RaycastHit _cRayHit)
	{
		bool bWriteViewId = true;

		switch (_eType)
		{
			case CPlayerInteractor.EInteractionType.PrimaryStart: s_cSerializeStream.Write((byte)ENetworkAction.ActivateToolPrimary); break;
			case CPlayerInteractor.EInteractionType.PrimaryEnd: s_cSerializeStream.Write((byte)ENetworkAction.DeactivateToolPrimary); break;
			case CPlayerInteractor.EInteractionType.SecondaryStart: s_cSerializeStream.Write((byte)ENetworkAction.ActivateToolSecondary); break;
			case CPlayerInteractor.EInteractionType.SecondaryEnd: s_cSerializeStream.Write((byte)ENetworkAction.DeactivateToolSeconary); break;
			case CPlayerInteractor.EInteractionType.Use: 
			{
				if (_cInteractableObject != null &&
					_cInteractableObject.GetComponent<CToolInterface>() != null)
				{
					s_cSerializeStream.Write((byte)ENetworkAction.PickupTool);
				}
				else
				{
					s_cSerializeStream.Write((byte)ENetworkAction.UseTool);
				}
			}
			break;

			default:
				bWriteViewId = false;
				break;
		}

		if (bWriteViewId)
		{
			if (_cInteractableObject == null)
			{
				s_cSerializeStream.Write((ushort)0);
			}
			else
			{
				// Target intractable object view id
				s_cSerializeStream.Write(_cInteractableObject.GetComponent<CNetworkView>().ViewId);
			}
		}
	}


	[AClientMethod]
	void OnNoInteraction(CPlayerInteractor.EInteractionType _eType, RaycastHit _cRayHit)
	{
		OnInteraction(_eType, null, _cRayHit);
	}


	[AClientMethod]
    public static void SerializeBeltState(CNetworkStream _cStream)
    {
		GameObject cPlayerObject = CGame.PlayerActor;
		
		
		if (cPlayerObject != null)
		{
			CPlayerBelt cPlayerBelt = cPlayerObject.GetComponent<CPlayerBelt>();
	
	
			if (cPlayerBelt == null)
			{
				Debug.LogError("The player actor does not have the CPlayerBelt component");
			}
			else
			{
				// Reload
				if (Input.GetKeyDown(s_eReloadToolKey))
				{
					_cStream.Write((byte)ENetworkAction.ReloadActiveTool);
				}
	
				// Drop tool
				else if (Input.GetKeyDown(s_eDropToolKey))
				{
					_cStream.Write((byte)ENetworkAction.DropActiveTool);
				}

				// Change tool
				for (uint i = 0; i < cPlayerBelt.ToolCapacity; ++i)
				{
					// Check key for slot was pressed
					if (Input.GetKeyDown(s_aeSelectToolKeys[i]))
					{
						_cStream.Write((byte)ENetworkAction.ChangeTool);

						// Write target tool id
						_cStream.Write((byte)i);

						break;
					}
				}
			}
		}

		// Write in internal stream
		if (s_cSerializeStream.Size > 0)
		{
			_cStream.Write(s_cSerializeStream);
			s_cSerializeStream.Clear();
		}
    }


	[AServerMethod]
    public static void UnserializeBeltState(CNetworkPlayer _cNetworkPlayer, CNetworkStream _cStream)
    {
		GameObject cPlayerObject = CGame.FindPlayerActor(_cNetworkPlayer.PlayerId);
		CPlayerBelt cPlayerBelt = cPlayerObject.GetComponent<CPlayerBelt>();


		// Process stream data
		while (_cStream.HasUnreadData)
		{
			// Extract action
			ENetworkAction eAction = (ENetworkAction)_cStream.ReadByte();
			GameObject cInteractableObject = null;

			switch (eAction)
			{
				case ENetworkAction.ActivateToolPrimary:
				case ENetworkAction.DeactivateToolPrimary:
				case ENetworkAction.ActivateToolSecondary:
				case ENetworkAction.DeactivateToolSeconary:
				case ENetworkAction.PickupTool:
				case ENetworkAction.UseTool:
				{
					ushort usInteractableObjectViewId = _cStream.ReadUShort();

					if (usInteractableObjectViewId != 0)
					{
						cInteractableObject = CNetwork.Factory.FindObject(usInteractableObjectViewId);
					}

					break;
				}
			}

			// Handle action
			switch (eAction)
			{
				case ENetworkAction.ActivateToolPrimary:
					cPlayerBelt.SetToolPrimaryActive(cPlayerBelt.ActiveSlotId, true, cInteractableObject);
					break;

				case ENetworkAction.DeactivateToolPrimary:
					cPlayerBelt.SetToolPrimaryActive(cPlayerBelt.ActiveSlotId, false, cInteractableObject);
					break;

				case ENetworkAction.ActivateToolSecondary:
					cPlayerBelt.SetToolSecondaryActive(cPlayerBelt.ActiveSlotId, true, cInteractableObject);
					break;

				case ENetworkAction.DeactivateToolSeconary:
					cPlayerBelt.SetToolSecondaryActive(cPlayerBelt.ActiveSlotId, false, cInteractableObject);
					break;

				case ENetworkAction.PickupTool:
					cPlayerBelt.PickupTool(_cNetworkPlayer.PlayerId, cInteractableObject);
					break;

				case ENetworkAction.UseTool:
					cPlayerBelt.UseTool(cPlayerBelt.ActiveSlotId, cInteractableObject);
					break;

				case ENetworkAction.ChangeTool:
					byte bSlotId = _cStream.ReadByte();
					cPlayerBelt.ChangeTool(bSlotId);
					break;

				case ENetworkAction.ReloadActiveTool:
					cPlayerBelt.ReloadTool(cPlayerBelt.ActiveSlotId);
					break;

				case ENetworkAction.DropActiveTool:
					cPlayerBelt.DropTool(cPlayerBelt.ActiveSlotId);
					break;
			}
		}
    }


    // Member Fields


	CNetworkVar<ushort>[] m_ausToolsViewId = null;
	CNetworkVar<byte> m_bToolCapacity = null;
	CNetworkVar<byte> m_bActiveToolId = null;


	static KeyCode[] s_aeSelectToolKeys = null;
	static KeyCode s_eReloadToolKey = KeyCode.R;
	static KeyCode s_eDropToolKey = KeyCode.G;
	static CNetworkStream s_cSerializeStream = new CNetworkStream();


};