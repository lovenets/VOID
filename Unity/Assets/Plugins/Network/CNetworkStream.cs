﻿//  Auckland
//  New Zealand
//
//  (c) 2013 VOID
//
//  File Name   :   TransmissionStream.h
//  Description :   --------------------------
//
//  Author  	:  Programming Team
//  Mail    	:  contanct@spaceintransit.co.nz
//


// Namespaces
using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
using System.Reflection;


/* Implementation */


public class CNetworkStream
{

// Member Types


// Member Functions

	// public:


    public CNetworkStream()
    {
        m_cBitStream = new RakNet.BitStream();
    }


	public CNetworkStream(byte[] _baData)
	{
		m_cBitStream = new RakNet.BitStream(_baData, (uint)_baData.Length, false);
	}


	public CNetworkStream(RakNet.BitStream _cBitStream)
	{
		m_cBitStream = _cBitStream;
	}


	public void Clear()
	{
		m_cBitStream.Reset();
		m_cBitStream.SetWriteOffset(0);
		m_cBitStream.SetReadOffset(0);
	}


    public void SetReadOffset(uint _uiBytes)
    {
        m_cBitStream.SetReadOffset(_uiBytes * 8);
    }


    public void Write(CNetworkStream _cStream)
    {
        m_cBitStream.Write(_cStream.BitStream);
        _cStream.SetReadOffset(0);
    }


    public void Write(object _cObject, Type _cType)
    {
        // Serialize the parameter value
        byte[] baValueSerialized = Converter.ToByteArray(_cObject, _cType);

        // Write string length if type is string
        if (_cType == typeof(string))
        {
            this.Write((byte)((string)_cObject).Length);
        }

        // Write parameter value
        this.Write(baValueSerialized);
    }


    public void Write(MethodInfo _tMethodInfo, object[] _caParameterValues)
    {
        // Extract the parameters from the method
        ParameterInfo[] caParameters = _tMethodInfo.GetParameters();


        for (int i = 0; i < caParameters.Length; ++i)
        {
            Write(_caParameterValues[i], caParameters[i].ParameterType);
        }
    }


	public void Write(byte[] _baData)
	{
		m_cBitStream.Write(_baData, (uint)_baData.Length);
	}


	public void Write(byte _bValue)
	{
		m_cBitStream.Write(_bValue);
	}


	public void Write(short _sValue)
	{
		m_cBitStream.Write(_sValue);
	}


	public void Write(ushort _usValue)
	{
		m_cBitStream.Write(_usValue);
	}


	public void Write(int _iValue)
	{
		m_cBitStream.Write(_iValue);
	}


	public void Write(uint _uiValue)
	{
		m_cBitStream.Write((int)_uiValue);
	}


	public void IgnoreBytes(int _iNumBytes)
	{
		IgnoreBytes((uint)_iNumBytes);
	}


	public void IgnoreBytes(uint _uiNumBytes)
	{
		m_cBitStream.IgnoreBytes(_uiNumBytes);
	}


    public object[] ReadMethodParameters(MethodInfo _tMethodInfo)
    {
        // Extract the parameters from the method
        ParameterInfo[] caParameters = _tMethodInfo.GetParameters();


        object[] caParameterValues = new object[caParameters.Length];


        for (int i = 0; i < caParameters.Length; ++i)
        {
            int iSize = Converter.GetSizeOf(caParameters[i].ParameterType);

            // Read string length if type is string
            if (caParameters[i].ParameterType == typeof(string))
            {
                iSize = this.ReadByte();
            }

            byte[] baSerializedValue = this.ReadBytes(iSize);

            caParameterValues[i] = Converter.ToObject(baSerializedValue, caParameters[i].ParameterType);
        }


        return (caParameterValues);
    }


    public byte[] ReadType(Type _cType)
    {
        int iSize = Converter.GetSizeOf(_cType);

        if (_cType == typeof(string))
        {
            iSize = ReadByte();
        }


        return (ReadBytes(iSize));
    }


	public byte[] ReadBytes(int _iSize)
	{
		return (ReadBytes((uint)_iSize));
	}


	public byte[] ReadBytes(uint _uiSize)
	{
		byte[] baBytes = new byte[_uiSize];


		if (!m_cBitStream.Read(baBytes, _uiSize))
		{
			Logger.WriteError("Could not read bytes");
		}


		return (baBytes);
	}


	public byte ReadByte()
	{
		byte bByte = 0;


		if (!m_cBitStream.Read(out bByte))
		{
			Logger.WriteError("Could not read byte");
		}


		return (bByte);
	}


	public short ReadShort()
	{
		short sValue = 0;


		if (!m_cBitStream.Read(out sValue))
		{
			Logger.WriteError("Could not read short");
		}


		return (sValue);
	}


	public ushort ReadUShort()
	{
		return ((ushort)ReadShort());
	}


	public int ReadInt()
	{
		int iValue = 0;


		if (!m_cBitStream.Read(out iValue))
		{
			Logger.WriteError("Could not read int");
		}

		
		return (iValue);
	}


	public uint ReadUInt()
	{
		return ((uint)ReadInt());
	}


	public RakNet.BitStream BitStream
	{
		get { return (m_cBitStream); }
	}


	public uint Size
	{
		get { return (BitStream.GetNumberOfBytesUsed()); }
	}


	public uint NumReadByes
	{
		get { return (m_cBitStream.GetReadOffset() / 8); }
	}


    public uint NumUnreadBytes
    {
        get { return (Size - NumReadByes); }
    }


	public bool HasUnreadData
	{
		get { return (this.NumReadByes < this.Size); }
	}


	// protected:


	// private:



// Member Variables

	// protected:


	// private:


	RakNet.BitStream m_cBitStream;


};
