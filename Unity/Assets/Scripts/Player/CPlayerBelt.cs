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


    const uint k_uiMaxNumTools = 4;


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


	public delegate void EquipTool(GameObject _equippedTool);
	public event EquipTool EventEquipTool;


    [AClientOnly]
    public delegate void HandleToolChanged(CNetworkViewId _cViewId);
    public event HandleToolChanged EventToolChanged;


    [AClientOnly]
    public delegate void HandleToolDropped(CNetworkViewId _cViewId);
    public event HandleToolDropped EventToolDropped;


    [AClientOnly]
    public delegate void HandleToolPickedup(CNetworkViewId _cViewId);
    public event HandleToolPickedup EventToolPickedup;
	

// Member Properties


	public GameObject ActiveTool
	{
		get 
		{
			if (ActiveToolViewId != null)
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


	public CNetworkViewId ActiveToolViewId
	{
		get { return (m_acToolsViewId[ActiveSlotId].Get()); }
	}


	public byte ToolCapacity
	{
		get { return (m_bToolCapacity.Get()); }
	}
	
	
// Member Functions


    public override void InstanceNetworkVars(CNetworkViewRegistrar _cRegistrar)
    {
		m_acToolsViewId = new CNetworkVar<CNetworkViewId>[k_uiMaxNumTools];

		for (uint i = 0; i < k_uiMaxNumTools; ++i)
		{
			m_acToolsViewId[i] = _cRegistrar.CreateNetworkVar<CNetworkViewId>(OnNetworkVarSync);
		}

		m_bToolCapacity = _cRegistrar.CreateNetworkVar<byte>(OnNetworkVarSync, 2);
		m_bActiveToolId = _cRegistrar.CreateNetworkVar<byte>(OnNetworkVarSync);
    }


	[AServerOnly]
	public void SetToolPrimaryActive(byte _bSlotId, bool _bActive, GameObject _cInteractableObject)
	{
		// Check tool exists
		if (GetToolViewId(_bSlotId) != null)
		{
			GetTool(_bSlotId).GetComponent<CToolInterface>().SetPrimaryActive(_bActive, _cInteractableObject);

            //Debug.Log(string.Format("Set primary active({0})", _bActive));
		}
	}


	[AServerOnly]
	public void SetToolSecondaryActive(byte _bSlotId, bool _bActive, GameObject _cInteractableObject)
	{
		// Check tool exists
		if (GetToolViewId(_bSlotId) != null)
		{
			GetTool(_bSlotId).GetComponent<CToolInterface>().SetSecondaryActive(_bActive, _cInteractableObject);

            //Debug.Log(string.Format("Set secondary active({0})", _bActive));
		}
	}


	[AServerOnly]
	public void PickupTool(ulong _ulPlayerId, GameObject _cInteractableObject)
	{
		// Check object exists
		if (_cInteractableObject != null)
		{
			// Find free slot
			for (uint i = 0; i < ToolCapacity; ++i)
			{
				if (GetToolViewId(i) == null)
				{
					// Retrieve tool interface script
					CToolInterface cToolInterface = _cInteractableObject.GetComponent<CToolInterface>();
					CNetworkView cToolNetworkView = _cInteractableObject.GetComponent<CNetworkView>();

					// Check script found
					if (cToolInterface == null)
					{
						Debug.LogError(string.Format("Target tool does not have the CToolInterface component attached! ObjectName({0})", _cInteractableObject.name));
					}
                    else if (cToolInterface.IsHeld)
                    {
                        break;
                    }
					else
					{
						m_acToolsViewId[i].Set(cToolNetworkView.ViewId);
						cToolInterface.PickUp(_ulPlayerId);
						ChangeTool((byte)i);
						Debug.Log(string.Format("Picked up tool. PlayerId({0}) ToolObjectName({1}) SlotId({2})", _ulPlayerId, _cInteractableObject.name, i));
					}

					break;
				}
			}
		}
	}


	[AServerOnly]
	public void UseTool(byte _bSlotId, GameObject _cInteractableObject)
	{
		// Check tool exists
		if (GetToolViewId(_bSlotId) != null)
		{
			GetTool(_bSlotId).GetComponent<CToolInterface>().Use(_cInteractableObject);
		}
	}


	[AServerOnly]
	public void ChangeTool(byte _bSlotId)
	{
		// Check tool exists
		if (_bSlotId >= 0 &&
            _bSlotId < k_uiMaxNumTools &&
            GetToolViewId(_bSlotId) != null)
		{
			m_bActiveToolId.Set(_bSlotId);

            Debug.Log(string.Format("Changing tool to SlotId({0})", _bSlotId));
		}
	}


	[AServerOnly]
	public void ReloadTool(byte _bSlotId)
	{
		// Check tool exists
		if (GetToolViewId(_bSlotId) != null)
		{
			GetTool(_bSlotId).GetComponent<CToolInterface>().Reload();
			Debug.Log("Reloading active tool");
		}
	}


	[AServerOnly]
	public void DropTool(byte _bSlotId)
	{
		// Check tool exists
		if (GetToolViewId(_bSlotId) != null)
		{
			GetTool(_bSlotId).GetComponent<CToolInterface>().Drop();
            m_acToolsViewId[_bSlotId].Set(null);

            // Change tool to next available tool
            for (int i = (int)k_uiMaxNumTools - 1; i >= 0; --i)
            {
                if (m_acToolsViewId[i].Get() != null)
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


	public CNetworkViewId GetToolViewId(uint _bSlotId)
	{
		return (m_acToolsViewId[_bSlotId].Get());
	}


    [AClientOnly]
    public static void SerializeBeltState(CNetworkStream _cStream)
    {
        // Write in internal stream
        _cStream.Write(s_cSerializeStream);
        s_cSerializeStream.Clear();
    }


    [AServerOnly]
    public static void UnserializeBeltState(CNetworkPlayer _cNetworkPlayer, CNetworkStream _cStream)
    {
        GameObject cPlayerObject = CGamePlayers.FindPlayerActor(_cNetworkPlayer.PlayerId);
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
                        CNetworkViewId cInteractableObjectViewId = _cStream.ReadNetworkViewId();

                        if (cInteractableObjectViewId != null)
                        {
                            cInteractableObject = CNetwork.Factory.FindObject(cInteractableObjectViewId);
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
                    if(cPlayerBelt.EventEquipTool != null)cPlayerBelt.EventEquipTool(cInteractableObject);                   
                    break;

                case ENetworkAction.UseTool:
                    cPlayerBelt.UseTool(cPlayerBelt.ActiveSlotId, cInteractableObject);
                    break;

                case ENetworkAction.ChangeTool:
                    byte bSlotId = _cStream.ReadByte();
                    cPlayerBelt.ChangeTool(bSlotId);
                    if(cPlayerBelt.EventEquipTool != null) cPlayerBelt.EventEquipTool(cInteractableObject);
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


    void Start()
    {
        gameObject.GetComponent<CPlayerInteractor>().EventInteraction += new CPlayerInteractor.HandleInteraction(OnInteraction);
        gameObject.GetComponent<CPlayerInteractor>().EventNoInteraction += new CPlayerInteractor.HandleNoInteraction(OnNoInteraction);
        gameObject.GetComponent<CNetworkView>().EventPreDestory += new CNetworkView.NotiftyPreDestory(OnPreDestroy);

        CUserInput.SubscribeInputChange(CUserInput.EInput.Tool_Reload, OnReloadToolKey);
        CUserInput.SubscribeInputChange(CUserInput.EInput.Tool_Drop, OnDropToolKey);
        CUserInput.SubscribeInputChange(CUserInput.EInput.Tool_SelectSlot1, OnSelectToolSlot);
        CUserInput.SubscribeInputChange(CUserInput.EInput.Tool_SelectSlot2, OnSelectToolSlot);
        CUserInput.SubscribeInputChange(CUserInput.EInput.Tool_SelectSlot3, OnSelectToolSlot);
        CUserInput.SubscribeInputChange(CUserInput.EInput.Tool_SelectSlot4, OnSelectToolSlot);
    }


	void OnDestroy()
	{
		gameObject.GetComponent<CPlayerInteractor>().EventInteraction -= OnInteraction;
		gameObject.GetComponent<CPlayerInteractor>().EventNoInteraction -= OnNoInteraction;
		gameObject.GetComponent<CNetworkView>().EventPreDestory -= OnPreDestroy;

        CUserInput.UnsubscribeInputChange(CUserInput.EInput.Tool_Reload, OnReloadToolKey);
        CUserInput.UnsubscribeInputChange(CUserInput.EInput.Tool_Drop, OnDropToolKey);
        CUserInput.UnsubscribeInputChange(CUserInput.EInput.Tool_SelectSlot1, OnSelectToolSlot);
        CUserInput.UnsubscribeInputChange(CUserInput.EInput.Tool_SelectSlot2, OnSelectToolSlot);
        CUserInput.UnsubscribeInputChange(CUserInput.EInput.Tool_SelectSlot3, OnSelectToolSlot);
        CUserInput.UnsubscribeInputChange(CUserInput.EInput.Tool_SelectSlot4, OnSelectToolSlot);
	}


    void Update()
    {
        // Empty
    }


	[AClientOnly]
    void OnInteraction(CPlayerInteractor.EInteractionType _eType, GameObject _cInteractableObject, RaycastHit _cRayHit)
	{
		bool bWriteViewId = true;
		
		switch (_eType)
		{
			case CPlayerInteractor.EInteractionType.PrimaryStart: s_cSerializeStream.Write((byte)ENetworkAction.ActivateToolPrimary); break;
			case CPlayerInteractor.EInteractionType.PrimaryEnd: s_cSerializeStream.Write((byte)ENetworkAction.DeactivateToolPrimary); break;
			case CPlayerInteractor.EInteractionType.SecondaryStart: s_cSerializeStream.Write((byte)ENetworkAction.ActivateToolSecondary); break;
			case CPlayerInteractor.EInteractionType.SecondaryEnd: s_cSerializeStream.Write((byte)ENetworkAction.DeactivateToolSeconary); break;
			case CPlayerInteractor.EInteractionType.UseStart: 
			{
				if (_cInteractableObject != null &&
					_cInteractableObject.GetComponent<CToolInterface>() != null)
				{
				    //Debug.LogError("Started to pick up tool" + _eType);
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
				s_cSerializeStream.Write((CNetworkViewId)null);
			}
			else
			{
				// Target intractable object view id
				s_cSerializeStream.Write(_cInteractableObject.GetComponent<CNetworkView>().ViewId);
			}
		}
	}


	[AClientOnly]
	void OnNoInteraction(CPlayerInteractor.EInteractionType _eType, RaycastHit _cRayHit)
	{
		OnInteraction(_eType, null, _cRayHit);
	}


    void OnNetworkVarSync(INetworkVar _cSyncedVar)
    {
        for (uint i = 0; i < k_uiMaxNumTools; ++i)
        {
            if (m_acToolsViewId[i] == _cSyncedVar)
            {
                CNetworkViewId cToolNetworkViewId = m_acToolsViewId[i].Get();

                if (cToolNetworkViewId == null)
                {
                    if (EventToolDropped != null) EventToolDropped(m_acToolsViewId[i].GetPrevious());
                }
                else
                {
                    if (EventToolPickedup != null) EventToolPickedup(cToolNetworkViewId);
                }
            }
        }

        // Changing tool
        if (_cSyncedVar == m_bActiveToolId)
        {
            if (EventToolChanged != null) EventToolChanged(m_acToolsViewId[m_bActiveToolId.Get()].Get());
        }
    }


    void OnPreDestroy()
    {
        if (CNetwork.IsServer)
        {
            for (uint i = 0; i < k_uiMaxNumTools; ++i)
            {
                if (m_acToolsViewId[i].Get() != null)
                {
                    GetTool(i).GetComponent<CToolInterface>().Drop();
                }
            }
        }
    }


    void OnGUI()
    {
        if (gameObject == CGamePlayers.SelfActor)
        {
            string sToolText = "";

            for (uint i = 0; i < m_bToolCapacity.Get(); ++i)
            {
                sToolText += "Slot "+ i +": ";

                if (GetToolViewId(i) != null)
                {
                    sToolText += GetTool(i).name;

                    if (ActiveSlotId == i)
                    {
                        sToolText += " - ACTIVE";
                    }
                }
                else
                {
                    sToolText += "None";
                }

                sToolText += "\n";
            }

            GUI.Box(new Rect(10, Screen.height - 90, 240, 56),
                    "[Belt]\n" +
                    sToolText);
        }
    }


    void OnDropToolKey(CUserInput.EInput _eInput, bool _bDown)
    {
        if (_bDown)
        {
            s_cSerializeStream.Write((byte)ENetworkAction.DropActiveTool);
        }
    }


    void OnReloadToolKey(CUserInput.EInput _eInput, bool _bDown)
    {
        if (_bDown)
        {
            s_cSerializeStream.Write((byte)ENetworkAction.ReloadActiveTool);
        }
    }


    void OnSelectToolSlot(CUserInput.EInput _eInput, bool _bDown)
    {
        if (_bDown)
        {
            s_cSerializeStream.Write((byte)ENetworkAction.ChangeTool);

            switch (_eInput)
            {
                case CUserInput.EInput.Tool_SelectSlot1:
                    s_cSerializeStream.Write((byte)0);
                    break;

                case CUserInput.EInput.Tool_SelectSlot2:
                    s_cSerializeStream.Write((byte)1);
                    break;

                case CUserInput.EInput.Tool_SelectSlot3:
                    s_cSerializeStream.Write((byte)2);
                    break;

                case CUserInput.EInput.Tool_SelectSlot4:
                    s_cSerializeStream.Write((byte)3);
                    break;

                default:
                    Debug.LogError("Unknown input");
                    break;
            }  
        }
    }


    // Member Fields


	CNetworkVar<CNetworkViewId>[] m_acToolsViewId = null;
	CNetworkVar<byte> m_bToolCapacity = null;
	CNetworkVar<byte> m_bActiveToolId = null;


	static CNetworkStream s_cSerializeStream = new CNetworkStream();


};
