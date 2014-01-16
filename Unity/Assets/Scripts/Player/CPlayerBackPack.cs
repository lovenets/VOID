//  Auckland
//  New Zealand
//
//  (c) 2013
//
//  File Name   :   CPlayerBackPack.cs
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


public class CPlayerBackPack : CNetworkMonoBehaviour
{

// Member Types


	public enum ENetworkAction
	{
		PickupModule,
		DropModule,
		InsertCell
	}


// Member Delegates & Events


// Member Properties


	public GameObject ModuleObject
	{
		get
		{
			if (CarryingModuleViewId != null)
			{
				return (CNetwork.Factory.FindObject(CarryingModuleViewId));
			}

			return (null);
		}
	}


	public CNetworkViewId CarryingModuleViewId
	{
		get { return (m_cCarryingModuleViewId.Get()); }
	}


	public bool IsCarryingModule
	{
		get { return (CarryingModuleViewId != null); }
	}


// Member Functions


	public override void InstanceNetworkVars()
	{
		m_cCarryingModuleViewId = new CNetworkVar<CNetworkViewId>(OnNetworkVarSync);
	}


    [AServerOnly]
    public void PickupModule(ulong _ulPlayerId, CNetworkViewId _cModuleViewId)
    {
        if (!IsCarryingModule)
        {
            m_cCarryingModuleViewId.Set(_cModuleViewId);
            CNetwork.Factory.FindObject(_cModuleViewId).GetComponent<CModuleInterface>().Pickup(_ulPlayerId);
        }
    }


    [AServerOnly]
    public void DropModule()
    {
        if (IsCarryingModule)
        {
            CNetwork.Factory.FindObject(CarryingModuleViewId).GetComponent<CModuleInterface>().Drop();
            m_cCarryingModuleViewId.Set(null);
        }
    }


    [AServerOnly]
    public void InsertCell(ulong _ulPlayerId, CNetworkViewId _cCellSlotViewId)
    {
        if (IsCarryingModule)
        {
            CNetworkViewId CellToInsert = m_cCarryingModuleViewId.Get();

            DropModule();

            CNetworkViewId replacementCell = CNetwork.Factory.FindObject(_cCellSlotViewId).GetComponent<CCellSlot>().Insert(CellToInsert);

            if (replacementCell != null)
            {
                PickupModule(_ulPlayerId, replacementCell);
            }
        }
    }


	[AClientOnly]
	public void OnPickupModuleRequest(CPlayerInteractor.EInteractionType _eType, GameObject _cInteractableObject, RaycastHit _cRayHit)
	{
		if (_eType == CPlayerInteractor.EInteractionType.Use &&
			_cInteractableObject.GetComponent<CModuleInterface>() != null)
		{
			// Action
			s_cSerializeStream.Write((byte)ENetworkAction.PickupModule);

			// Target tool view id
			s_cSerializeStream.Write(_cInteractableObject.GetComponent<CNetworkView>().ViewId);
		}
	}
	
	[AClientOnly]
	public void OnCellInsertRequest(CPlayerInteractor.EInteractionType _eType, GameObject _cInteractableObject, RaycastHit _cRayHit)
	{
		if (_eType == CPlayerInteractor.EInteractionType.PrimaryStart &&
			_cInteractableObject.GetComponent<CCellSlot>() != null &&
			IsCarryingModule)
		{
			CModuleInterface.EType carryingCellType = CNetwork.Factory.FindObject(CarryingModuleViewId).GetComponent<CModuleInterface>().m_eType;
			CModuleInterface.EType cellSlotType = _cInteractableObject.GetComponent<CCellSlot>().m_CellSlotType;
			
			if(carryingCellType == cellSlotType)
			{
				// Function to insert the cell here
				Debug.Log("Ima sliding my " + carryingCellType.ToString() + " into a " + cellSlotType.ToString() + " slot.");
				
				// Action
				s_cSerializeStream.Write((byte)ENetworkAction.InsertCell);

				// Target tool view id
				s_cSerializeStream.Write(_cInteractableObject.GetComponent<CNetworkView>().ViewId);
			}
		}
	}


	[AClientOnly]
    public static void SerializeOutbound(CNetworkStream _cStream)
    {
		// Drop
		if (Input.GetKeyDown(s_eDropKey))
		{
			_cStream.Write((byte)ENetworkAction.DropModule);
		}

		// Write in internal stream
		if (s_cSerializeStream.Size > 0)
		{
			_cStream.Write(s_cSerializeStream);
			s_cSerializeStream.Clear();
		}
	}


	[AServerOnly]
	public static void UnserializeInbound(CNetworkPlayer _cNetworkPlayer, CNetworkStream _cStream)
	{
		GameObject cPlayerObject = CGame.FindPlayerActor(_cNetworkPlayer.PlayerId);
		CPlayerBackPack cPlayerBackPack = cPlayerObject.GetComponent<CPlayerBackPack>();

		// Process stream data
		while (_cStream.HasUnreadData)
		{
			// Extract action
			ENetworkAction eAction = (ENetworkAction)_cStream.ReadByte();

			// Handle action
			switch (eAction)
			{
				case ENetworkAction.PickupModule:
					CNetworkViewId cModuleViewId = _cStream.ReadNetworkViewId();
					cPlayerBackPack.PickupModule(_cNetworkPlayer.PlayerId, cModuleViewId);
					
					break;

				case ENetworkAction.DropModule:
					cPlayerBackPack.DropModule();
					break;
				
				case ENetworkAction.InsertCell:
					CNetworkViewId cCellSlotViewId = _cStream.ReadNetworkViewId();
					cPlayerBackPack.InsertCell(_cNetworkPlayer.PlayerId, cCellSlotViewId);
					break;
			}
		}
	}


    void Start()
    {
        gameObject.GetComponent<CPlayerInteractor>().EventInteraction += new CPlayerInteractor.HandleInteraction(OnPickupModuleRequest);
        gameObject.GetComponent<CPlayerInteractor>().EventInteraction += new CPlayerInteractor.HandleInteraction(OnCellInsertRequest);
        gameObject.GetComponent<CNetworkView>().EventPreDestory += new CNetworkView.NotiftyPreDestory(OnPreDestroy);
    }


    void OnPreDestroy()
    {
        if (CNetwork.IsServer)
        {
            if (IsCarryingModule)
            {
                DropModule();
            }
        }
    }


    void Update()
    {
        // Empty
    }


    void OnNetworkVarSync(INetworkVar _cSyncedNetworkVar)
    {
        if (_cSyncedNetworkVar == m_cCarryingModuleViewId)
        {
            if (m_cCarryingModuleViewId.Get() == null)
            {

            }
        }
    }


    void OnGUI()
    {
        if (gameObject == CGame.SelfActor)
        {
            string sModuleText = "[Module] ";

            if (m_cCarryingModuleViewId.Get() != null)
            {
                sModuleText += ModuleObject.name;
            }
            else
            {
                sModuleText += "None";
            }


            GUI.Label(new Rect(10, Screen.height - 80, 500, 50), sModuleText);
        }
    }


// Member Fields


	CNetworkVar<CNetworkViewId> m_cCarryingModuleViewId = null;


	static KeyCode s_eDropKey = KeyCode.H;
	static CNetworkStream s_cSerializeStream = new CNetworkStream();


};
