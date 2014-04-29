﻿//  Auckland
//  New Zealand
//
//  (c) 2013
//
//  File Name   :   CTile.cs
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
using System.Linq;


/* Implementation */


public abstract class CTile : MonoBehaviour
{
	// Member Types
	public enum EType
	{
		INVALID = -1,
		
		InteriorFloor,
		InteriorWall,
		InteriorWallCap,
		InteriorCeiling,
		ExteriorWall,
		ExteriorWallCap,
		
		MAX
	}

	[System.Serializable]
	public class CMeta
	{
		public static CMeta Default
		{
			get { return(new CMeta(-1, 0)); }
		}

		public CMeta(int _TileMask, int _MetaType)
		{
			m_TileMask = _TileMask;
			m_NeighbourMask = 0;
			m_MetaType = _MetaType;
			m_Rotations = 0;
			m_Variant = 0;
		}

		public CMeta(CMeta _Other)
		{
			m_TileMask = _Other.m_TileMask;
			m_NeighbourMask = _Other.m_NeighbourMask;
			m_MetaType = _Other.m_MetaType;
			m_Rotations = _Other.m_Rotations;
			m_Variant = _Other.m_Variant;
		}

		public int m_TileMask;
		public int m_NeighbourMask;
		public int m_MetaType;
		public int m_Rotations;
		public int m_Variant;

		public override bool Equals(System.Object obj)
		{
			// If parameter is null return false.
			if (obj == null)
				return false;
			
			// If parameter cannot be cast to Point return false.
			CMeta p = (CMeta)obj;
			if ((System.Object)p == null)
				return false;
			
			// Return true if the fields match:
			return ((m_TileMask == p.m_TileMask) && 
			        (m_NeighbourMask == p.m_NeighbourMask) &&
			        (m_MetaType == p.m_MetaType) &&
			        (m_Rotations == p.m_Rotations) &&
			        (m_Variant == p.m_Variant));
		}

		public bool Equals(CMeta p)
		{
			// If parameter is null return false:
			if ((object)p == null)
				return false;
			
			// Return true if the fields match:
			return ((m_TileMask == p.m_TileMask) && 
			        (m_NeighbourMask == p.m_NeighbourMask) &&
			        (m_MetaType == p.m_MetaType) &&
			        (m_Rotations == p.m_Rotations) &&
			        (m_Variant == p.m_Variant));
		}
	}

	// Member Delegates & Events
	public delegate void HandleTileEvent(CTile _Self);

	public event HandleTileEvent EventTileObjectChanged;

	
	// Member Fields
	public EType m_TileType = EType.INVALID;

	public CMeta m_ActiveTileMeta = CMeta.Default;
	public CMeta m_CurrentTileMeta = CMeta.Default;
	
	public CTileInterface m_TileInterface = null;
	public GameObject m_TileObject = null;

	public List<EDirection> m_NeighbourExemptions = new List<EDirection>();


	// Member Properties
	public CTileInterface TileInterface
	{
		set { m_TileInterface = value; }
	}

	public abstract Dictionary<int, CTile.CMeta> TileMetaDictionary
	{
		get;
	}


	// Member Methods
	protected void Update()
	{
		if(m_ActiveTileMeta.Equals(m_CurrentTileMeta))
			return;

		// Update the visible tile object
		UpdateTileObject();

		// Invoke network RPC to update current meta for all clients
		if(CNetwork.IsServer)
			m_TileInterface.InvokeTileCurrentMetaUpdate(this);
	}

	protected void OnDestroy()
	{
		if(m_TileObject != null)
			ReleaseTileObject();
	}

	[AServerOnly]
	public void UpdateCurrentTileMetaData()
	{
		int tileMask = 0;

		// Define the tile mask given its relevant directions, relevant type and neighbour mask state.
		foreach(CNeighbour neighbour in m_TileInterface.m_NeighbourHood)
		{
			if(!IsNeighbourRelevant(neighbour))
				continue;

			tileMask |= 1 << (int)neighbour.m_Direction;
		}
		
		// Update the tile meta info
		bool metaChanged = RetrieveTileMetaEntry(tileMask);
		
		// Return if the meta data hasn't changed
		if(!metaChanged)
			return;

		// Invoke neighbours to update their tile meta data
		foreach(CNeighbour neighbour in m_TileInterface.m_NeighbourHood)
		{
			neighbour.m_TileInterface.UpdateAllCurrentTileMetaData();
		}
	}

	protected abstract bool IsNeighbourRelevant(CNeighbour _Neighbour);

	[AServerOnly]
	protected bool RetrieveTileMetaEntry(int _TileMask)
	{	
		bool currentMetaChanged = false;

		// Find the tile mask out of possible 4 rotations
		for(int i = 0; i < 4; ++i)
		{
			int tileMask = 0;
			for(int j = 0; j < (int)EDirection.MAX; ++j)
			{
				// Define a new mask based on neighbours that exist in the mask
				if((_TileMask & (1 << j)) != 0)
				{
					// "Rotate" the mask by 90*, 2 bits ac for each rotation
					int dirMask = j - (2 * i);
					if(dirMask < 0)
						dirMask += 8;
					
					tileMask |= 1 << dirMask;
				}
			}
			
			// Continue if it was not a result
			if(!TileMetaDictionary.ContainsKey(tileMask))
				continue;

			// Get the meta entry for the result with a correct rotation
			CTile.CMeta newTileMeta = new CMeta(TileMetaDictionary[tileMask].m_TileMask, TileMetaDictionary[tileMask].m_MetaType);
			newTileMeta.m_Rotations = i;
			newTileMeta.m_Variant = m_CurrentTileMeta.m_Variant;
			newTileMeta.m_NeighbourMask = m_CurrentTileMeta.m_NeighbourMask;
			
			// Update the current tile meta data
			currentMetaChanged = !m_CurrentTileMeta.Equals(newTileMeta);
			m_CurrentTileMeta = newTileMeta;

			return(currentMetaChanged);
		}

		return(false);

		Debug.LogWarning("Tile Meta data wasn't found for: " + m_TileType + " Mask: " + _TileMask);
	}

	protected void UpdateTileObject()
	{
		// Release the tile object
		ReleaseTileObject();
		
		// Create the new tile type object as long as it is not marked as none ("0")
		if(m_CurrentTileMeta.m_MetaType != 0)
		{
			m_TileObject = m_TileInterface.m_Grid.TileFactory.InstanceNewTile(m_TileType, m_CurrentTileMeta.m_MetaType, m_CurrentTileMeta.m_Variant);
			m_TileObject.transform.parent = transform;
			m_TileObject.transform.localPosition = Vector3.zero;
			m_TileObject.transform.localScale = Vector3.one;
			m_TileObject.transform.localRotation = Quaternion.Euler(0.0f, m_CurrentTileMeta.m_Rotations * 90.0f, 0.0f);

			if(EventTileObjectChanged != null)
				EventTileObjectChanged(this);
		}
		
		// Update the tiles active meta data
		m_ActiveTileMeta = new CTile.CMeta(m_CurrentTileMeta);
	}

	[AServerOnly]
	public void SetTileTypeVariant(int _TileVariant)
	{
		m_CurrentTileMeta.m_Variant = _TileVariant;
	}
	
	public int GetTileTypeVariant()
	{
		return(m_CurrentTileMeta.m_Variant);
	}

	[AServerOnly]
	public void SetNeighbourExemptionState(EDirection _Direction, bool _State)
	{
		if(m_NeighbourExemptions.Contains(_Direction) && _State)
			return;
		
		if(!m_NeighbourExemptions.Contains(_Direction) && !_State)
			return;

		if(_State)
		{
			m_NeighbourExemptions.Add(_Direction);
		}
		else
		{
			m_NeighbourExemptions.Remove(_Direction);
		}

		CUtility.SetMaskState((int)_Direction, _State, ref m_CurrentTileMeta.m_NeighbourMask);
	}
	
	public bool GetNeighbourExemptionState(EDirection _Direction)
	{
		return(CUtility.GetMaskState((int)_Direction, m_CurrentTileMeta.m_NeighbourMask));
	}

	public void ReleaseTileObject()
	{
		if(m_TileObject == null)
			return;

		if(m_TileInterface.m_Grid != null)
			m_TileInterface.m_Grid.TileFactory.ReleaseTileObject(m_TileObject);

		m_TileObject = null;
	}
	
	protected static CMeta CreateMetaEntry(int _MetaType, EDirection[] _Neighbours)
	{
		// Define the neighbours into a mask
		int mask = 0;
		foreach(EDirection direction in _Neighbours)
		{
			mask |= 1 << (int)direction;
		}
		
		return(new CMeta(mask, (int)_MetaType));
	}
	
	public static Type GetTileClassType(EType _TileType)
	{
		Type classType = null;
		
		switch(_TileType)
		{
		case EType.InteriorFloor: 		classType = typeof(CTile_InteriorFloor); break;
		case EType.ExteriorWall: 		classType = typeof(CTile_ExteriorWall); break;
		case EType.InteriorWall: 		classType = typeof(CTile_InteriorWall); break;
		case EType.InteriorCeiling: 	classType = typeof(CTile_InteriorCeiling); break;
		case EType.ExteriorWallCap: 	classType = typeof(CTile_ExternalWallCap); break;
		case EType.InteriorWallCap: 	classType = typeof(CTile_InteriorWallCap); break;
		}
		
		return(classType);
	}
}