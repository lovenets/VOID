﻿//  Auckland
//  New Zealand
//
//  (c) 2013
//
//  File Name   :   CLifeSupportDistribution.cs
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


[RequireComponent(typeof(CModuleInterface))]
public class CAtmosphereGeneratorInterface : CNetworkMonoBehaviour 
{

// Member Types

	
// Member Delegates & Events


// Member Properties


	public float AtmosphereGenerationRate
	{
		get { return(m_fGenerationRate.Get()); }
	}


// Member Methods


    public override void RegisterNetworkComponents(CNetworkViewRegistrar _cRegistrar)
    {
        m_fGenerationRate = _cRegistrar.CreateReliableNetworkVar<float>(OnNetworkVarSync, 0.0f);
    }


    void Awake()
    {
        m_cModuleInterface = GetComponent<CModuleInterface>();

        if (CNetwork.IsServer)
        {
            // Signup for module events
            m_cModuleInterface.EventBuilt += OnEventBuilt;
            m_cModuleInterface.EventEnableChange += OnEventModuleEnableChange;
            m_cModuleInterface.EventFunctionalRatioChange += OnEventModuleFunctionalRatioChange;
        }
    }


    void Start()
    {
        // Empty
    }


    [AServerOnly]
    void OnEventBuilt(CModuleInterface _cSender)
    {
        CGameShips.Ship.GetComponent<CShipAtmosphereSystem>().ChangeMaxGenerationRate(m_fInitialGenerationRate);
    }


    [AServerOnly]
    void OnEventModuleEnableChange(CModuleInterface _cSender, bool _bEnabled)
    {
        if (_bEnabled)
        {
            m_fGenerationRate.Value = m_fInitialGenerationRate * m_cModuleInterface.FunctioanlRatio;
        }
        else
        {
            m_fGenerationRate.Value = 0.0f;
        }
    }


    [AServerOnly]
    void OnEventModuleFunctionalRatioChange(CModuleInterface _cSender, float _fPreviousRatio, float _fNewRatio)
    {
        if (m_cModuleInterface.IsEnabled)
        {
            m_fGenerationRate.Value = m_fInitialGenerationRate * _fNewRatio;
        }
    }


    void OnNetworkVarSync(INetworkVar _VarInstance)
    {
        if (m_fGenerationRate == _VarInstance)
        {
            // Update ship power system
            if (CNetwork.IsServer)
            {
                CGameShips.Ship.GetComponent<CShipAtmosphereSystem>().ChangeGenerationRate(m_fGenerationRate.Value - m_fGenerationRate.PreviousValue);
            }
        }
    }


 // Member Fields


    public float m_fInitialGenerationRate = 0.0f;


    CNetworkVar<float> m_fGenerationRate  = null;


    CModuleInterface m_cModuleInterface = null;


}
