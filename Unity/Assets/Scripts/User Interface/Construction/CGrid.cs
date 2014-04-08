//  Auckland
//  New Zealand
//
//  (c) 2013
//
//  File Name   :   CGridManager.cs
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


public class CGrid : MonoBehaviour 
{	
	// Member Types
	public struct TCreateTileInfo
	{
		public TCreateTileInfo(TGridPoint _GridPoint, CTile.ETileType[] _TileTypes)
		{
			m_GridPoint = _GridPoint;
			m_TileTypes = _TileTypes;
		}

		public TGridPoint m_GridPoint;
		public CTile.ETileType[] m_TileTypes;
	}

	
	// Member Delegates & Events
	public delegate void HandleTileEvent(CTile _Tile);
	
	public event HandleTileEvent EventTileAdded;
	public event HandleTileEvent EventTileRemoved;

	
	// Member Fields
	private Transform m_TileContainer = null;

	public float m_TileSize = 4.0f;
	public CTileFactory m_TileFactory = null;

	private List<TCreateTileInfo> m_CreateQueue = new List<TCreateTileInfo>();
	private List<TGridPoint> m_DestroyQueue = new List<TGridPoint>();

	private Dictionary<string, CTile> m_GridBoard = new Dictionary<string, CTile>();

	
	// Member Properties
	public Transform TileContainer
	{
		get { return(m_TileContainer); }
	}

	public List<CTile> Tiles
	{
		get { return(new List<CTile>(m_GridBoard.Values)); }
	}

	// Member Methods
	private void Start() 
	{
		// Create the grid objects
		CreateGridObjects();
	}

	private void Update()
	{
		foreach(TCreateTileInfo createInfo in m_CreateQueue)
		{
			CreateTile(createInfo);
		}
		m_CreateQueue.Clear();

		foreach(TGridPoint point in m_DestroyQueue)
		{
			RemoveTile(point);
		}
		m_DestroyQueue.Clear();
	}

	private void CreateGridObjects()
	{
		m_TileContainer = new GameObject("Tile Container").transform;
		m_TileContainer.parent = transform;
		m_TileContainer.localScale = Vector3.one;
		m_TileContainer.localPosition = Vector3.zero;
		m_TileContainer.localRotation = Quaternion.identity;
	}

	public TGridPoint GetGridPoint(Vector3 worldPosition)
	{
		return(new TGridPoint(GetGridPosition(worldPosition)));
	}

	public Vector3 GetGridPosition(Vector3 worldPosition)
	{
		// Convert the world space to grid space
		Vector3 gridpos = Quaternion.Inverse(transform.rotation) * (worldPosition - transform.position);

		// Scale the position to tilesize and scale
		gridpos = gridpos / m_TileSize / transform.localScale.x;

		// Round each position to be an integer number
		gridpos.x = Mathf.Round(gridpos.x);
		gridpos.y = Mathf.Round(gridpos.y);
		gridpos.z = Mathf.Round(gridpos.z);

		return gridpos;
	}

	public Vector3 GetLocalPosition(TGridPoint _GridPoint)
	{
		// Convert from grid space to local space
		return(_GridPoint.ToVector * m_TileSize);
	}

	public CTile GetTile(TGridPoint _GridPoint)
	{
		CTile tile = null;
		if(m_GridBoard.ContainsKey(_GridPoint.ToString()))
		{
			tile = m_GridBoard[_GridPoint.ToString()];
		}
		return(tile);
	}

	public void AddNewTile(TGridPoint _GridPoint, CTile.ETileType[] _TileTypes)
	{
		m_CreateQueue.Add(new TCreateTileInfo(_GridPoint, _TileTypes));
	}

	public void ReleaseTile(TGridPoint _GridPoint)
	{
		m_DestroyQueue.Add(_GridPoint);
	}

	public void ImportPreExistingTiles(CTile[] _Tiles)
	{
		foreach(CTile tile in _Tiles)
		{
			if(!m_GridBoard.ContainsKey(tile.m_GridPosition.ToString()))
			{
				m_GridBoard.Add(tile.m_GridPosition.ToString(), tile);
				tile.m_Grid = this;
			}
			else
			{
				Debug.LogWarning("Tile already exists at position " + tile.m_GridPosition.ToString() + ". Tile was not imported.");
			}
		}
	}

	public void ImportTileInformation(CTile[] _Tiles)
	{
		foreach(CTile tile in _Tiles)
		{
			// Get the meta information of the tile
			int typeIdentifier = tile.m_TileTypeIdentifier;

			// Change the existing tile to match this tile
			if(m_GridBoard.ContainsKey(tile.m_GridPosition.ToString()))
			{
				CTile existingTile = m_GridBoard[tile.m_GridPosition.ToString()];
				existingTile.m_TileTypeIdentifier = typeIdentifier;

				// Replace the meta data
				for(int i = (int)CTile.ETileType.INVALID + 1; i < (int)CTile.ETileType.MAX; ++i)
				{
					existingTile.SetMetaData((CTile.ETileType)i, tile.GetMetaData((CTile.ETileType)i));
				}
			}
			else
			{
				// Get the active tile types
				List<CTile.ETileType> tileTypes = new List<CTile.ETileType>();
				for(int i = (int)CTile.ETileType.INVALID + 1; i < (int)CTile.ETileType.MAX; ++i)
				{
					if(tile.GetTileTypeState((CTile.ETileType)i))
						tileTypes.Add((CTile.ETileType)i);
				}

				AddNewTile(tile.m_GridPosition, tileTypes.ToArray());
			}
		}
	}
	
	private void CreateTile(TCreateTileInfo _TileInfo)
	{
		if(!m_GridBoard.ContainsKey(_TileInfo.m_GridPoint.ToString()))
		{
			GameObject newtile = new GameObject("Tile");
			newtile.transform.parent = m_TileContainer;
			newtile.transform.localScale = Vector3.one;
			newtile.transform.localRotation = Quaternion.identity;
			newtile.transform.localPosition = GetLocalPosition(_TileInfo.m_GridPoint);

			CTile tile = newtile.AddComponent<CTile>();
			tile.m_Grid = this;
			tile.m_GridPosition = _TileInfo.m_GridPoint;

			// Set the active tile types
			foreach(CTile.ETileType type in _TileInfo.m_TileTypes)
			{
				tile.SetTileTypeState(type, true);
			}
			m_GridBoard.Add(_TileInfo.m_GridPoint.ToString(), tile);

			// Find neighbours
			tile.FindNeighbours();

			// Fire event for tile creation
			if(EventTileAdded != null)
				EventTileAdded(tile);
		}
	}

	private void RemoveTile(TGridPoint _GridPoint)
	{
		if (m_GridBoard.ContainsKey(_GridPoint.ToString()))
		{
			CTile tile = m_GridBoard[_GridPoint.ToString()];

			// Fire event for tile removal
			if(EventTileRemoved != null)
				EventTileRemoved(tile);

			// Release
			tile.Release();

			// Destroy
			m_GridBoard.Remove(_GridPoint.ToString());
			Destroy(tile.gameObject);
		}
	}
}


