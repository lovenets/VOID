//  Auckland
//  New Zealand
//
//  (c) 2013
//
//  File Name   :   CLASSNAME.cs
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


public class CMiningDrillBehaviour : CNetworkMonoBehaviour
{

// Member Types


    public enum ENetworkAction
    {
        INVALID,

        DrillStart,
        DrillEnd,

        MAX
    }


// Member Delegates & Events


// Member Properties


// Member Methods


    public override void InstanceNetworkVars(CNetworkViewRegistrar _cRegistrar)
    {
        m_bActive = _cRegistrar.CreateNetworkVar<bool>(OnNetworkVarSync, false);
    }


    [AClientOnly]
    public static void SerializeOutbound(CNetworkStream _cStream)
    {
        _cStream.Write(s_cSerializeStream);
        s_cSerializeStream.Clear();
    }


    [AServerOnly]
    public static void UnserializeInbound(CNetworkPlayer _cNetworkPlayer, CNetworkStream _cStream)
    {
        ENetworkAction eAction = (ENetworkAction)_cStream.ReadByte();
        CNetworkViewId cMiningDrillViewId = _cStream.ReadNetworkViewId();

        switch (eAction)
        {
            case ENetworkAction.DrillStart:
                cMiningDrillViewId.GameObject.GetComponent<CMiningDrillBehaviour>().m_bLaserActive = true;
                break;

            case ENetworkAction.DrillEnd:
                cMiningDrillViewId.GameObject.GetComponent<CMiningDrillBehaviour>().m_bLaserActive = false;
                break;

            default:
                Debug.LogError("Unknown network action");
                break;
        }
    }


    void Awake()
    {
        m_cSprayParticalSystem = transform.FindChild("ParticalSprayer").particleSystem;
    }


	void Start()
	{
        GetComponent<CToolInterface>().EventPrimaryActiveChange += OnEventPrimaryActiveChange;
	}


	void OnDestroy()
	{
        GetComponent<CToolInterface>().EventPrimaryActiveChange -= OnEventPrimaryActiveChange;
	}


	void Update()
	{
		if (CNetwork.IsServer)
		{
            bool bActive = false;

			if (m_bLaserActive)
			{
				RaycastHit _rh;
				Ray ray = new Ray(m_cSprayParticalSystem.gameObject.transform.position, m_cSprayParticalSystem.gameObject.transform.forward);

				if (Physics.Raycast(ray, out _rh, 4.0f))
				{
                    CMinerals cMinerals = _rh.collider.gameObject.GetComponent<CMinerals>();

                    if (cMinerals != null)
                    {
                        bActive = true;
   
                        cMinerals.DecrementQuanity(100.0f * Time.deltaTime);
                    }
				}
			}

            m_bActive.Set(bActive);
        }
	}


    [AClientOnly]
    void OnEventPrimaryActiveChange(bool _bActive)
    {
        if (_bActive)
        {
            s_cSerializeStream.Write((byte)ENetworkAction.DrillStart);
        }
        else
        {
            s_cSerializeStream.Write((byte)ENetworkAction.DrillEnd);
        }

        s_cSerializeStream.Write(ThisNetworkView.ViewId);
    }


    void OnNetworkVarSync(INetworkVar _cSyncedVar)
    {
        if (_cSyncedVar == m_bActive)
        {
            if (m_bActive.Get())
            {
                m_cSprayParticalSystem.Play();
            }
            else
            {
                m_cSprayParticalSystem.Stop();
            }
        }
    }


// Member Fields


    bool m_bLaserActive = false;


    CNetworkVar<bool> m_bActive = null;


    ParticleSystem m_cSprayParticalSystem = null;


    static CNetworkStream s_cSerializeStream = new CNetworkStream();


};