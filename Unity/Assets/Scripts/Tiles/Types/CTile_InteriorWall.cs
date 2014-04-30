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


public class CTile_InteriorWall : CTile 
{
	// Member Types
	public enum EType
	{
		INVALID = -1,

		None,
		Corner, 
		Edge,
		End,
		Hall,
		Cell,

		MAX,
	}

	public enum EVariant
	{
		INVALID = -1,
		
		Wall,
		Door,
		Window,
		
		MAX
	}


	// Member Delegates & Events
	
	
	// Member Fields
	static protected List<EDirection> s_RelevantDirections = new List<EDirection>();
	static protected Dictionary<int, CTile.CMeta> s_MetaDictionary = new Dictionary<int, CTile.CMeta>();

	
	// Member Properties
	public override Dictionary<int, CTile.CMeta> TileMetaDictionary
	{
		get { return(s_MetaDictionary); }
	}

	
	// Member Methods
	static CTile_InteriorWall()
	{
		// Fill relevant neighbours
		s_RelevantDirections.AddRange(new EDirection[]{ EDirection.North, EDirection.East, EDirection.South, EDirection.West });

		// Fill meta dictionary
		AddMetaEntry(EType.Cell,
		             new EDirection[]{ });
		
		AddMetaEntry(EType.None,
		             new EDirection[]{ EDirection.North, EDirection.East, EDirection.South, EDirection.West });
		
		AddMetaEntry(EType.Hall,
		             new EDirection[]{ EDirection.North, EDirection.South });
		
		AddMetaEntry(EType.Edge,
		             new EDirection[]{ EDirection.North, EDirection.West, EDirection.South });
		
		AddMetaEntry(EType.Corner,
		             new EDirection[]{ EDirection.North, EDirection.West });
		
		AddMetaEntry(EType.End,
		             new EDirection[]{ EDirection.West });
	}

	private static void AddMetaEntry(EType _Type, EDirection[] _MaskNeighbours)
	{
		CMeta meta = CreateMetaEntry((int)_Type, _MaskNeighbours);
		s_MetaDictionary.Add(meta.m_TileMask, meta);
	}

	private void Awake()
	{
		m_TileType = CTile.EType.InteriorWall;
	}

	protected override bool IsNeighbourRelevant(CNeighbour _Neighbour)
	{
		if(!s_RelevantDirections.Contains(_Neighbour.m_Direction))
			return(false);
		
		if(!_Neighbour.m_TileInterface.GetTileTypeState(CTile.EType.InteriorWall))
			return(false);
		
		if(CUtility.GetMaskState((int)_Neighbour.m_Direction, m_CurrentTileMeta.m_NeighbourMask))
			return(false);
		
		return(true);
	}
}