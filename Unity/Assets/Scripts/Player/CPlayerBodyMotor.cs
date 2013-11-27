﻿//  Auckland
//  New Zealand
//
//  (c) 2013 VOID
//
//  File Name   :   CActorMotor.cs
//  Description :   --------------------------
//
//  Author      :  Programming Team
//  Mail        :  contanct@spaceintransit.co.nz
//


// Namespaces
using UnityEngine;
using System.Collections;


/* Implementation */


public class CPlayerBodyMotor : CNetworkMonoBehaviour
{

// Member Types
	public enum EPlayerMovementState : uint
	{
		MoveForward 	= 1 << 0,
		MoveBackward 	= 1 << 1,
		MoveLeft 		= 1 << 2,
		MoveRight 		= 1 << 3,
		Jump 			= 1 << 4,
		Sprint 			= 1 << 5,
	}
	
	public class CPlayerMovementState
	{
		uint m_CurrentMovementState = 0;
		float m_LastUpdateTimeStamp = 0.0f;
		
		public uint CurrentState { get { return(m_CurrentMovementState); } }
		public float TimeStamp { get { return(m_LastUpdateTimeStamp); } }
		
		public void SetCurrentState(uint _NewState, float _TimeStamp)
		{
			if(CNetwork.IsServer)
			{
				if(m_LastUpdateTimeStamp < _TimeStamp)
				{
					m_CurrentMovementState = _NewState;
				}
			}
			else
			{
				Logger.Write("Player MotorState: Only server can direcly set the motor state!");
			}
		}
		
		public bool MovingForward
		{
			set { SetState(value, CPlayerBodyMotor.EPlayerMovementState.MoveForward); }
			get { return(GetState(CPlayerBodyMotor.EPlayerMovementState.MoveForward)); }
		}
		
		public bool MovingBackward
		{
			set { SetState(value, CPlayerBodyMotor.EPlayerMovementState.MoveBackward); }
			get { return(GetState(CPlayerBodyMotor.EPlayerMovementState.MoveBackward)); }
		}
		
		public bool MovingLeft
		{
			set { SetState(value, CPlayerBodyMotor.EPlayerMovementState.MoveLeft); }
			get { return(GetState(CPlayerBodyMotor.EPlayerMovementState.MoveLeft)); }
		}
		
		public bool MovingRight
		{
			set { SetState(value, CPlayerBodyMotor.EPlayerMovementState.MoveRight); }
			get { return(GetState(CPlayerBodyMotor.EPlayerMovementState.MoveRight)); }
		}
		
		public bool Jumping
		{
			set { SetState(value, CPlayerBodyMotor.EPlayerMovementState.Jump); }
			get { return(GetState(CPlayerBodyMotor.EPlayerMovementState.Jump)); }
		}
		
		public bool Sprinting
		{
			set { SetState(value, CPlayerBodyMotor.EPlayerMovementState.Sprint); }
			get { return(GetState(CPlayerBodyMotor.EPlayerMovementState.Sprint)); }
		}
		
		private void SetState(bool _Value, EPlayerMovementState _State)
		{
			if(_Value)
			{
				m_CurrentMovementState |= (uint)_State;
			}
			else
			{
				m_CurrentMovementState &= ~(uint)_State;
			}
			
			m_LastUpdateTimeStamp = Time.time;
		}
		
		private bool GetState(EPlayerMovementState _State)
		{
			return((m_CurrentMovementState & (uint)_State) != 0);
		}
		
		public void ResetStates()
		{
			m_CurrentMovementState = 0;
		}
	}
	
// Member Fields
	public float m_Gravity = 9.81f;
	public float m_MovementSpeed = 4.0f;
	public float m_SprintSpeed = 7.0f;
	public float m_JumpSpeed = 4.0f;

	
	public CPlayerMovementState m_MotorState = new CPlayerMovementState();
	
	
	private bool m_FreezeMovmentInput = false;
	private bool m_UsingGravity = true;
	private bool m_bGrounded = false;
	
	
	private Vector3 m_GravityForce = Vector3.zero;
	private Vector3 m_Velocity = Vector3.zero;
	

    static KeyCode m_eMoveForwardKey = KeyCode.W;
    static KeyCode m_eMoveBackwardsKey = KeyCode.S;
    static KeyCode m_eMoveLeftKey = KeyCode.A;
    static KeyCode m_eMoveRightKey = KeyCode.D;
	static KeyCode m_eJumpKey = KeyCode.Space;
	static KeyCode m_eSprintKey = KeyCode.LeftShift;
	
	
// Member Properties	
	public bool FreezeMovmentInput
	{
		set { m_FreezeMovmentInput = value; }
		get { return(m_FreezeMovmentInput); }
	}
	
	public bool UsingGravity
	{
		set { m_UsingGravity = value; }
		get { return(m_UsingGravity); }
	}
	
	public Vector3 GravityForce
	{
		set { m_GravityForce = value; }
		get { return(m_GravityForce); }
	}
	
// Member Methods
	public void Start()
	{

	}
	
    public void Update()
    {	
		if(CGame.PlayerActor == gameObject && !FreezeMovmentInput)
		{
			UpdatePlayerInput();
		}
		
		if(CNetwork.IsServer)
		{
			// Placeholder: Make gravity relative to the ship
			m_GravityForce = CGame.Ship.transform.up * -m_Gravity;
			
			// Process the movement of the player
			ProcessMovement();
		}
    }
	
	public override void InstanceNetworkVars()
	{
		
	}
	
    public static void SerializePlayerState(CNetworkStream _cStream)
    {
		CPlayerBodyMotor actorMotor = CGame.PlayerActor.GetComponent<CPlayerBodyMotor>();
		
		_cStream.Write(actorMotor.m_MotorState.CurrentState);
		_cStream.Write(actorMotor.m_MotorState.TimeStamp);
		
		actorMotor.m_MotorState.ResetStates();
    }

	public static void UnserializePlayerState(CNetworkPlayer _cNetworkPlayer, CNetworkStream _cStream)
    {
		CPlayerBodyMotor actorMotor = CGame.FindPlayerActor(_cNetworkPlayer.PlayerId).GetComponent<CPlayerBodyMotor>();
		
		uint motorState = _cStream.ReadUInt();
		float timeStamp = _cStream.ReadFloat();
		
		actorMotor.m_MotorState.SetCurrentState(motorState, timeStamp);
    }
	
    private void UpdatePlayerInput()
	{	
		// Move forwards
        if (Input.GetKey(m_eMoveForwardKey))
        {
			m_MotorState.MovingForward = true;
        }

        // Move backwards
        if (Input.GetKey(m_eMoveBackwardsKey))
        {
			m_MotorState.MovingBackward = true;
        }

        // Move left
        if ( Input.GetKey(m_eMoveLeftKey))
        {
            m_MotorState.MovingLeft = true;
        }

        // Move right
        if (Input.GetKey(m_eMoveRightKey))
        {
            m_MotorState.MovingRight = true;
        }
		
		// Jump
		if(Input.GetKey(m_eJumpKey))
		{
			m_MotorState.Jumping = true;
		}
		
		// Sprint
		if (Input.GetKey(m_eSprintKey))
		{
			m_MotorState.Sprinting = true;
		}
	}
	
	private void ProcessMovement()
    {		
		CharacterController charController = GetComponent<CharacterController>();
		
		// Only if grounded
		if(charController.isGrounded)
		{
			float moveSpeed = m_MovementSpeed;
			m_Velocity = Vector3.zero;
			
			// Sprinting
			if(m_MotorState.Sprinting)
			{
				moveSpeed = m_SprintSpeed;
			}
			
			// Moving 
			if(m_MotorState.MovingForward != m_MotorState.MovingBackward)
			{
				m_Velocity.z = m_MotorState.MovingForward ? 1.0f : -1.0f;
			}
			
			// Strafing
			if(m_MotorState.MovingLeft != m_MotorState.MovingRight)
			{
				m_Velocity.x = m_MotorState.MovingLeft ? -1.0f : 1.0f;
			}
			
			// Normalize the movewment/strafing and multiply by movement speed
			m_Velocity = m_Velocity.normalized * moveSpeed;
			
			// Jumping
			if(m_MotorState.Jumping)
			{
				m_Velocity.y = m_JumpSpeed;
			}
			
			// Transform the velocity ammount for relative velocity
			m_Velocity = transform.rotation * m_Velocity;
		}
		else
		{
			// Apply the gravity
			m_Velocity += m_GravityForce * Time.deltaTime;
		}
		
		// Apply the movement
		charController.Move(m_Velocity * Time.deltaTime);
	}
};