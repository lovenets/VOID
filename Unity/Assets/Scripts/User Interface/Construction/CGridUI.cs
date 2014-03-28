﻿//  Auckland
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


[RequireComponent(typeof(CGrid))]
public class CGridUI : MonoBehaviour 
{
	// Member Types
	public enum EMode
	{
		AutoLayout,
		ManualWallLayout,
	}

	public enum EInteraction
	{
		INVALID,
		
		Nothing,
		CursorPaint,
		DragSelection,
		DragRotation,
	}
	
	// Member Delegates & Events
	
	
	// Member Fields
	private CGrid m_Grid = null;

	private GameObject m_GridPlane = null;
	private GameObject m_GridSphere = null;
	private GameObject m_GridCursor = null;

	public float m_GridScale = 0.1f;
	public Vector2 m_GridScaleLimits = new Vector2(0.05f, 0.2f);

	public EMode m_CurrentInteractionMode = EMode.AutoLayout;
	public EInteraction m_CurrentInteraction = EInteraction.INVALID;
	
	public Vector3 m_CurrentMousePoint = Vector3.zero;
	public TGridPoint m_CurrentMouseGridPoint;
	public Vector3 m_CurrentMousePosition = Vector3.zero;
	
	public Vector3 m_MouseDownPoint = Vector3.zero;
	public TGridPoint m_MouseDownGridPoint;
	public Vector3 m_MouseDownPosition = Vector3.zero;

	private RaycastHit[] m_RaycastHits;
	private Quaternion m_DragRotateStart = Quaternion.identity;

	
	// Member Properties
	public bool IsShiftKeyDown
	{
		get { return(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)); }
	}
	
	public bool IsCtrlKeyDown
	{
		get { return(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)); }
	}
	
	public bool AltKeyDown
	{
		get { return(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)); }
	}		          
	
	// Member Methods
	void Awake()
	{
		m_Grid = gameObject.GetComponent<CGrid>();
	}
	
	void Start() 
	{
		// Create the grid objects
		CreateGridUIObjects();
	}
	
	void CreateGridUIObjects()
	{
		m_GridPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
		m_GridPlane.name = "Raycast Plane";
		m_GridPlane.renderer.material.shader = Shader.Find("Transparent/Diffuse");
		m_GridPlane.renderer.material.color = new Color(1.0f, 1.0f, 1.0f, 0.1f);
		m_GridPlane.collider.isTrigger = true;
		m_GridPlane.transform.parent = m_Grid.transform;
		m_GridPlane.transform.localPosition = Vector3.zero;
		m_GridPlane.transform.localRotation = Quaternion.identity;
		
		m_GridCursor = GameObject.CreatePrimitive(PrimitiveType.Cube);
		m_GridCursor.name = "Cursor";
		m_GridCursor.renderer.material.shader = Shader.Find("Transparent/Diffuse");
		m_GridCursor.renderer.material.color = new Color(0.0f, 1.0f, 0.0f, 0.5f);
		Destroy(m_GridCursor.collider);
		m_GridCursor.transform.parent = m_Grid.transform;
		m_GridCursor.transform.localScale = Vector3.one * m_Grid.m_TileSize;
		m_GridCursor.transform.localPosition = Vector3.zero;
		m_GridCursor.transform.localRotation = Quaternion.identity;
		
		// Update scale and clamp
		m_GridScale = Mathf.Clamp(m_GridScale, m_GridScaleLimits.x, m_GridScaleLimits.y);
		UpdateGridScale(m_GridScale);
	}
	
	void Update() 
	{
		m_CurrentMousePosition = Input.mousePosition;

		// Get the raycast hits against all objects
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		m_RaycastHits = Physics.RaycastAll(ray, Mathf.Infinity);

		// Update default input
		UpdateDefaultInput();

		// Update input based on interaction mode
		switch(m_CurrentInteractionMode)
		{
			case EMode.AutoLayout: UpdateLayoutInput(); break;
			case EMode.ManualWallLayout: UpdateLayoutInput(); break;
			default: break;
		}
	}

	void UpdateDefaultInput()
	{
		// Toggle modes
		if(Input.GetKeyDown(KeyCode.Alpha1))
		{
			m_CurrentInteractionMode = EMode.AutoLayout;
		}
		else if(Input.GetKeyDown(KeyCode.Alpha2))
		{
			m_CurrentInteractionMode = EMode.ManualWallLayout;
		}

		// Check for collisions for drag rotations
		foreach(RaycastHit hit in m_RaycastHits)
		{
			// If a grid plane collision
			if(hit.collider.gameObject == m_GridPlane)
			{
				UpdateCursor();
			}
			// else if a sphere collision
			else if(hit.collider.gameObject == gameObject)
			{
				m_CurrentMousePoint = hit.point;
				
				// Right Click Down
				if(Input.GetMouseButtonDown(1))
				{
					m_MouseDownPoint = hit.point;
					m_MouseDownPosition = Input.mousePosition;
					m_DragRotateStart = m_Grid.transform.rotation;
					
					m_CurrentInteraction = EInteraction.DragRotation;
				}
				
				// Mouse Scroll
				float sw = Input.GetAxis("Mouse ScrollWheel");
				if(sw != 0.0f)
				{
					if(IsShiftKeyDown)
					{
						int direction = (int)Mathf.Sign(sw);
						ChangeVerticalLayer(direction);
					}
					else
					{
						UpdateGridScale(Mathf.Clamp(m_GridScale + sw * 0.1f, m_GridScaleLimits.x, m_GridScaleLimits.y));
					}
				}
			}
		}

		// Right Click Hold
		if(Input.GetMouseButton(1) && m_CurrentInteraction == EInteraction.DragRotation)
		{
			DragRotateGrid();
		}
		
		// Right Click Up
		else if(Input.GetMouseButtonUp(1) && m_CurrentInteraction == EInteraction.DragRotation)
		{
			m_CurrentInteraction = EInteraction.Nothing;
		}
	}
	
	void UpdateLayoutInput()
	{
		foreach(RaycastHit hit in m_RaycastHits)
		{
			// If a grid plane collision
			if(hit.collider.gameObject == m_GridPlane)
			{
				m_CurrentMousePoint = hit.point;
				m_CurrentMouseGridPoint = m_Grid.GetGridPoint(hit.point);
				
				// Left Click Down
				if(Input.GetMouseButtonDown(0))
				{
					m_MouseDownPoint = hit.point;
					m_MouseDownGridPoint = m_CurrentMouseGridPoint;
					m_MouseDownPosition = Input.mousePosition;
					
					if(IsShiftKeyDown)
						m_CurrentInteraction = EInteraction.DragSelection;
					else
						m_CurrentInteraction = EInteraction.CursorPaint;
				}
				
				// Left Click Hold
				if(Input.GetMouseButton(0))
				{
					if(m_CurrentInteraction == EInteraction.CursorPaint)
					{
						if(IsCtrlKeyDown)
						{
							if(m_CurrentInteractionMode == EMode.AutoLayout)
							{
								m_Grid.RemoveTile(m_CurrentMouseGridPoint);
							}
						}
						else
						{
							if(m_CurrentInteractionMode == EMode.AutoLayout)
							{
								m_Grid.CreateTile(m_CurrentMouseGridPoint);
							}
							else if(m_CurrentInteractionMode == EMode.ManualWallLayout)
							{
								CTile tile = m_Grid.GetTile(m_CurrentMouseGridPoint);
								tile.UpdateInternalWallTile(true);
							}
						}
					}
				}
				
				// Left Click Up
				if(Input.GetMouseButtonUp(0))
				{
					if(m_CurrentInteraction == EInteraction.DragSelection)
					{
						if(IsCtrlKeyDown)
							SelectionManipulateTiles(true);
						else
							SelectionManipulateTiles(false);
					}
					
					m_CurrentInteraction = EInteraction.Nothing;
				}
			}
		}
	}

	void UpdateManualWallsInput()
	{
		foreach(RaycastHit hit in m_RaycastHits)
		{
			// If a grid plane collision
			if(hit.collider.gameObject == m_GridPlane)
			{
				m_CurrentMousePoint = hit.point;
				m_CurrentMouseGridPoint = m_Grid.GetGridPoint(hit.point);
				
				// Left Click Down
				if(Input.GetMouseButtonDown(0))
				{
					m_MouseDownPoint = hit.point;
					m_MouseDownGridPoint = m_CurrentMouseGridPoint;
					m_MouseDownPosition = Input.mousePosition;
					
					if(IsShiftKeyDown)
						m_CurrentInteraction = EInteraction.DragSelection;
					else
						m_CurrentInteraction = EInteraction.CursorPaint;
				}
				
				// Left Click Hold
				if(Input.GetMouseButton(0))
				{
					if(m_CurrentInteraction == EInteraction.CursorPaint)
					{
						if(IsCtrlKeyDown)
							m_Grid.RemoveTile(m_Grid.GetGridPoint(m_CurrentMousePoint));
						else
							m_Grid.CreateTile(m_Grid.GetGridPoint(m_CurrentMousePoint));
					}
				}
				
				// Left Click Up
				if(Input.GetMouseButtonUp(0))
				{
					if(m_CurrentInteraction == EInteraction.DragSelection)
					{
						if (IsCtrlKeyDown)
							SelectionManipulateTiles(true);
						else
							SelectionManipulateTiles(false);
					}
					
					m_CurrentInteraction = EInteraction.Nothing;
				}
				

			}
		}
	}
	
	void UpdateGridScale(float _GridScale)
	{
		m_GridScale = _GridScale;

		// Update the grid root scale
		m_Grid.transform.localScale = Vector3.one * _GridScale;
		
		// Update the raycast plane to be the same
		m_GridPlane.transform.localScale = Vector3.one * 0.5f / _GridScale;
	}

	void ChangeVerticalLayer(int _Direction)
	{
		// Update the raycast plane to be the same
		//m_GridPlane.transform.localScale = Vector3.one * 0.5f / _GridScale;
	}
	
	void UpdateCursor()
	{
		if(m_CurrentInteraction == EInteraction.DragSelection)
		{
			Vector3 centerPos = (m_Grid.GetLocalPosition(m_CurrentMouseGridPoint) + m_Grid.GetLocalPosition(m_MouseDownGridPoint)) * 0.5f;
			float width = Mathf.Abs(m_CurrentMouseGridPoint.x - m_MouseDownGridPoint.x) + 1.0f;
			float depth = Mathf.Abs(m_CurrentMouseGridPoint.z - m_MouseDownGridPoint.z) + 1.0f;
			
			m_GridCursor.transform.localScale = new Vector3(width, 1.0f, depth) * m_Grid.m_TileSize;
			m_GridCursor.transform.localPosition = centerPos + Vector3.up * m_Grid.m_TileSize * 0.5f;
		}
		else if(m_CurrentInteraction != EInteraction.DragRotation)
		{
			m_GridCursor.transform.localScale = Vector3.one * m_Grid.m_TileSize;
			m_GridCursor.transform.localPosition = m_Grid.GetLocalPosition(m_CurrentMouseGridPoint) + Vector3.up * m_Grid.m_TileSize * 0.5f;
		}
	}

	public void DragRotateGrid()
	{
		// Get the screen mouse positions
		Vector3 point1 = m_MouseDownPosition;
		Vector3 point2 = m_CurrentMousePosition;
		
		// Get the difference of the two
		Vector3 diff = (point1 - point2);
		
		// Rotate plane using the axis of camera to rotate pitch and yaw
		Quaternion rotPitch = Quaternion.AngleAxis(-diff.y * 0.1f, Camera.main.transform.right);
		Quaternion rotYaw = Quaternion.AngleAxis(diff.x * 0.2f, m_Grid.transform.parent.up);
		
		// Lerp to the final rotation
		m_Grid.transform.rotation = rotPitch * m_DragRotateStart * rotYaw;
	}
	
	public void SelectionManipulateTiles(bool _Remove)
	{
		// Get the diagonal corner points
		TGridPoint point1 = m_MouseDownGridPoint;
		TGridPoint point2 = m_CurrentMouseGridPoint;
		
		// Determine the rect properties
		int left = point1.x < point2.x ? point1.x : point2.x;
		int bottom = point1.z < point2.z ? point1.z : point2.z;
		int width = Mathf.Abs(point1.x - point2.x) + 1;
		int height = Mathf.Abs(point1.z - point2.z) + 1;
		
		// Itterate through the positions
		for(int x = left; x < (left + width); ++x)
		{
			for(int z = bottom; z < (bottom + height); ++z)
			{
				if(_Remove) m_Grid.RemoveTile(new TGridPoint(x, point1.y, z));
				else m_Grid.CreateTile(new TGridPoint(x, point1.y, z));
			}
		}
	}
}


