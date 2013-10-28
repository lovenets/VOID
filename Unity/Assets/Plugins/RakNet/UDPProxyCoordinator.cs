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

public class UDPProxyCoordinator : PluginInterface2 {
  private HandleRef swigCPtr;

  internal UDPProxyCoordinator(IntPtr cPtr, bool cMemoryOwn) : base(RakNetPINVOKE.UDPProxyCoordinator_SWIGUpcast(cPtr), cMemoryOwn) {
    swigCPtr = new HandleRef(this, cPtr);
  }

  internal static HandleRef getCPtr(UDPProxyCoordinator obj) {
    return (obj == null) ? new HandleRef(null, IntPtr.Zero) : obj.swigCPtr;
  }

  ~UDPProxyCoordinator() {
    Dispose();
  }

  public override void Dispose() {
    lock(this) {
      if (swigCPtr.Handle != IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          RakNetPINVOKE.delete_UDPProxyCoordinator(swigCPtr);
        }
        swigCPtr = new HandleRef(null, IntPtr.Zero);
      }
      GC.SuppressFinalize(this);
      base.Dispose();
    }
  }

  public static UDPProxyCoordinator GetInstance() {
    IntPtr cPtr = RakNetPINVOKE.UDPProxyCoordinator_GetInstance();
    UDPProxyCoordinator ret = (cPtr == IntPtr.Zero) ? null : new UDPProxyCoordinator(cPtr, false);
    return ret;
  }

  public static void DestroyInstance(UDPProxyCoordinator i) {
    RakNetPINVOKE.UDPProxyCoordinator_DestroyInstance(UDPProxyCoordinator.getCPtr(i));
  }

  public UDPProxyCoordinator() : this(RakNetPINVOKE.new_UDPProxyCoordinator(), true) {
  }

  public void SetRemoteLoginPassword(RakString password) {
    RakNetPINVOKE.UDPProxyCoordinator_SetRemoteLoginPassword(swigCPtr, RakString.getCPtr(password));
    if (RakNetPINVOKE.SWIGPendingException.Pending) throw RakNetPINVOKE.SWIGPendingException.Retrieve();
  }

}

}
