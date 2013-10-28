/* ----------------------------------------------------------------------------
 * This file was automatically generated by SWIG (http://www.swig.org).
 * Version 2.0.10
 *
 * Do not make changes to this file unless you know what you are doing--modify
 * the SWIG interface file instead.
 * ----------------------------------------------------------------------------- */

namespace RakNet {

using System;
using System.Runtime.InteropServices;

public class FileListNode : IDisposable {
  private HandleRef swigCPtr;
  protected bool swigCMemOwn;

  internal FileListNode(IntPtr cPtr, bool cMemoryOwn) {
    swigCMemOwn = cMemoryOwn;
    swigCPtr = new HandleRef(this, cPtr);
  }

  internal static HandleRef getCPtr(FileListNode obj) {
    return (obj == null) ? new HandleRef(null, IntPtr.Zero) : obj.swigCPtr;
  }

  ~FileListNode() {
    Dispose();
  }

  public virtual void Dispose() {
    lock(this) {
      if (swigCPtr.Handle != IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          RakNetPINVOKE.delete_FileListNode(swigCPtr);
        }
        swigCPtr = new HandleRef(null, IntPtr.Zero);
      }
      GC.SuppressFinalize(this);
    }
  }

    private bool dataIsCached = false;
    private byte[] dataCache;

  public RakString filename {
    set {
      RakNetPINVOKE.FileListNode_filename_set(swigCPtr, RakString.getCPtr(value));
    } 
    get {
      IntPtr cPtr = RakNetPINVOKE.FileListNode_filename_get(swigCPtr);
      RakString ret = (cPtr == IntPtr.Zero) ? null : new RakString(cPtr, false);
      return ret;
    } 
  }

  public RakString fullPathToFile {
    set {
      RakNetPINVOKE.FileListNode_fullPathToFile_set(swigCPtr, RakString.getCPtr(value));
    } 
    get {
      IntPtr cPtr = RakNetPINVOKE.FileListNode_fullPathToFile_get(swigCPtr);
      RakString ret = (cPtr == IntPtr.Zero) ? null : new RakString(cPtr, false);
      return ret;
    } 
  }

  public byte[] data {
	set 
	{
	    	dataCache=value;
		dataIsCached = true;
		SetData (value, value.Length);    
	}

        get
        {
            byte[] returnArray;
            if (!dataIsCached)
            {
                IntPtr cPtr = RakNetPINVOKE.FileListNode_data_get (swigCPtr);
                int len = (int) dataLengthBytes;
		if (len<=0)
		{
			return null;
		}
                returnArray = new byte[len];
                byte[] marshalArray = new byte[len];
                Marshal.Copy(cPtr, marshalArray, 0, len);
                marshalArray.CopyTo(returnArray, 0);
                dataCache = returnArray;
                dataIsCached = true;
            }
            else
            {
                returnArray = dataCache;
            }
            return returnArray;
        }
 
  }

  public uint dataLengthBytes {
    set {
      RakNetPINVOKE.FileListNode_dataLengthBytes_set(swigCPtr, value);
    } 
    get {
      uint ret = RakNetPINVOKE.FileListNode_dataLengthBytes_get(swigCPtr);
      return ret;
    } 
  }

  public uint fileLengthBytes {
    set {
      RakNetPINVOKE.FileListNode_fileLengthBytes_set(swigCPtr, value);
    } 
    get {
      uint ret = RakNetPINVOKE.FileListNode_fileLengthBytes_get(swigCPtr);
      return ret;
    } 
  }

  public FileListNodeContext context {
    set {
      RakNetPINVOKE.FileListNode_context_set(swigCPtr, FileListNodeContext.getCPtr(value));
    } 
    get {
      IntPtr cPtr = RakNetPINVOKE.FileListNode_context_get(swigCPtr);
      FileListNodeContext ret = (cPtr == IntPtr.Zero) ? null : new FileListNodeContext(cPtr, false);
      return ret;
    } 
  }

  public bool isAReference {
    set {
      RakNetPINVOKE.FileListNode_isAReference_set(swigCPtr, value);
    } 
    get {
      bool ret = RakNetPINVOKE.FileListNode_isAReference_get(swigCPtr);
      return ret;
    } 
  }

  public FileListNode() : this(RakNetPINVOKE.new_FileListNode(), true) {
  }

  public void SetData(byte[] inByteArray, int numBytes) {
    RakNetPINVOKE.FileListNode_SetData(swigCPtr, inByteArray, numBytes);
  }

}

}
