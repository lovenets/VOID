//  Auckland
//  New Zealand
//
//  (c) 2013
//
//  File Name   :   CComponentInterface.cs
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


[RequireComponent(typeof(CNetworkView))]
public class CModuleInterface : MonoBehaviour
{

// Member Types
	public enum ECategory
	{
		INVALID,
		
		Atmosphere,
		Crew,
		Defence,
		Exploration,
		Gravity,
		Power,
		Production,
		Propulsion,
		Research,
		Resources,
		
		MAX
	}
	
	public enum EType
	{
		INVALID,

		AtmosphereGenerator,
		PlayerSpawner,
		LaserCockpit,
		LaserTurret,
		PilotCockpit,
		PowerGenerator,
		PowerCapacitor,
		MiningTurret,
		MiningCockpit,
		AtmosphereConditioner,
        OxygenRefiller,
        Dispenser,
        NaniteCapsule,

        MAX
	}

	public enum ESize
	{
		INVALID,

		Small,
		Medium,
		Large,

		MAX
	}


// Member Delegates & Events


// Member Properties


	public EType ModuleType
	{
		get { return (m_ModuleType); }
	}


	public ECategory ModuleCategory
	{
		get { return (m_ModuleCategory); }
	}


	public ESize ModuleSize
	{
		get { return (m_ModuleSize); }
	}


	public bool IsInternal
	{
		get { return(m_Internal); }
	}


	public bool IsBuildable
	{
		get { return(m_Buildable); }
	}


    public GameObject ParentFacility
    {
        get { return (m_cParentFacility); }
    }


// Member Methods


    public List<GameObject> FindAttachedComponentsByType(CComponentInterface.EType _eAccessoryType)
    {
        if (!m_mAttachedComponents.ContainsKey(_eAccessoryType))
        {
            return (null);
        }

        return (m_mAttachedComponents[_eAccessoryType]);
    }


    public void RegisterAttachedComponent(CComponentInterface _cComponentInterface)
    {
        if (!m_mAttachedComponents.ContainsKey(_cComponentInterface.ComponentType))
        {
            m_mAttachedComponents.Add(_cComponentInterface.ComponentType, new List<GameObject>());
        }

        m_mAttachedComponents[_cComponentInterface.ComponentType].Add(_cComponentInterface.gameObject);
    }


	public static List<GameObject> GetAllModules()
	{
		return (s_mModules);
	}


	public static List<GameObject> FindModulesByType(EType _eModuleType)
	{
		if (!s_mModulesByType.ContainsKey(_eModuleType))
		{
			return (null);
		}

		return (s_mModulesByType[_eModuleType]);
	}


	public static List<GameObject> FindModulesByCategory(ECategory _eModuleCategory)
	{
		if (!s_mModulesByCategory.ContainsKey(_eModuleCategory))
		{
			return (null);
		}
		
		return (s_mModulesByCategory[_eModuleCategory]);
	}


	public static List<GameObject> FindModulesBySize(ESize _eModuleSize)
	{
		if (!s_mModulesBySize.ContainsKey(_eModuleSize))
		{
			return (null);
		}
		
		return (s_mModulesBySize[_eModuleSize]);
	}


    public static void RegisterPrefab(EType _eModuleType, CGameRegistrator.ENetworkPrefab _ePrefab)
    {
        s_mRegisteredPrefabs.Add(_eModuleType, _ePrefab);
    }


    public static CGameRegistrator.ENetworkPrefab GetPrefabType(EType _eModuleType)
    {
        if (!s_mRegisteredPrefabs.ContainsKey(_eModuleType))
        {
            Debug.LogError(string.Format("Module type ({0}) has not been registered a prefab", _eModuleType));

            return (CGameRegistrator.ENetworkPrefab.INVALID);
        }

        return (s_mRegisteredPrefabs[_eModuleType]);
    }


	void Awake()
	{
		// Add self to the list of modules
		s_mModules.Add(gameObject);

		// Add self to the global list of module types
		if (!s_mModulesByType.ContainsKey(m_ModuleType))
		{
			s_mModulesByType.Add(m_ModuleType, new List<GameObject>());
		}
	
		s_mModulesByType[m_ModuleType].Add(gameObject);

		// Add self to the global list of module categories
		if (!s_mModulesByCategory.ContainsKey(m_ModuleCategory))
		{
			s_mModulesByCategory.Add(m_ModuleCategory, new List<GameObject>());
		}
		
		s_mModulesByCategory[m_ModuleCategory].Add(gameObject);

		// Add self to the global list of module sizes
		if (!s_mModulesBySize.ContainsKey(m_ModuleSize))
		{
			s_mModulesBySize.Add(m_ModuleSize, new List<GameObject>());
		}
		
		s_mModulesBySize[m_ModuleSize].Add(gameObject);
	}


	void Start()
	{
		// Ensure a type is defined 
		if (m_ModuleType == EType.INVALID)
		{
			Debug.LogError(string.Format("This module has not been given a module type. GameObjectName({0})", gameObject.name));
		}
		
		// Ensure a category is defined 
		if (m_ModuleCategory == ECategory.INVALID)
		{
			Debug.LogError(string.Format("This module has not been given a module category. GameObjectName({0})", gameObject.name));
		}
		
		// Ensure a size is defined 
		if (m_ModuleSize == ESize.INVALID)
		{
			Debug.LogError(string.Format("This module has not been given a module size. GameObjectName({0})", gameObject.name));
		}

		// Register self with parent facility
		CFacilityInterface fi = NGUITools.FindInParents<CFacilityInterface>(gameObject);

		if(fi != null)
		{
			fi.RegisterModule(this);
			m_cParentFacility = fi.gameObject;
		}
		else
		{
			Debug.LogError("Could not find facility to register to");
		}
	}


	void OnDestroy()
	{
		// Remove self from global list of modules
		s_mModules.Remove(gameObject);
		s_mModulesByType[ModuleType].Remove(gameObject);
		s_mModulesByCategory[ModuleCategory].Remove(gameObject);
		s_mModulesBySize[ModuleSize].Remove(gameObject);
	}


	void Update()
	{
		// Empty
	}


// Member Fields

	public EType m_ModuleType = EType.INVALID;
	public ECategory m_ModuleCategory = ECategory.INVALID;
	public ESize m_ModuleSize = ESize.INVALID;
	public bool m_Internal = true;
	public bool m_Buildable = true;


    GameObject m_cParentFacility = null;


    Dictionary<CComponentInterface.EType, List<GameObject>> m_mAttachedComponents = new Dictionary<CComponentInterface.EType, List<GameObject>>();


	static List<GameObject> s_mModules = new List<GameObject>();
	static Dictionary<EType, List<GameObject>> s_mModulesByType = new Dictionary<EType, List<GameObject>>();
	static Dictionary<ECategory, List<GameObject>> s_mModulesByCategory = new Dictionary<ECategory, List<GameObject>>();
	static Dictionary<ESize, List<GameObject>> s_mModulesBySize = new Dictionary<ESize, List<GameObject>>();

    static Dictionary<EType, CGameRegistrator.ENetworkPrefab> s_mRegisteredPrefabs = new Dictionary<EType, CGameRegistrator.ENetworkPrefab>();


};
