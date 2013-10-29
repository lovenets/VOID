using UnityEngine;
using System.Collections;

/// MouseLook rotates the transform based on the mouse delta.
/// Minimum and Maximum values can be used to constrain the possible rotation

/// To make an FPS style character:
/// - Create a capsule.
/// - Add the MouseLook script to the capsule.
///   -> Set the mouse look to use LookX. (You want to only turn character but not tilt it)
/// - Add FPSInputController script to the capsule
///   -> A CharacterMotor and a CharacterController component will be automatically added.

/// - Create a camera. Make the camera a child of the capsule. Reset it's transform.
/// - Add a MouseLook script to the camera.
///   -> Set the mouse look to use LookY. (You want the camera to tilt up and down like a head. The character already turns.)
[AddComponentMenu("Camera-Control/Mouse Look")]
public class MouseLook : MonoBehaviour 
{
	public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
	public RotationAxes axes = RotationAxes.MouseXAndY;
	public float sensitivityX = 0.5f;
	public float sensitivityY = 0.5f;

	public float minimumX = -360F;
	public float maximumX = 360F;

	public float minimumY = -60F;
	public float maximumY = 60F;

    public Texture CrossHair = null;

	float rotationY = 0F;

	void Update ()
	{
		Screen.lockCursor = true;
		
		if(Screen.lockCursor)
		{
			if (axes == RotationAxes.MouseXAndY)
			{
				float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;
				
				rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
				rotationY = Mathf.Clamp (rotationY, minimumY, maximumY);
				
				transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
			}
			else if (axes == RotationAxes.MouseX)
			{
				transform.Rotate(0, Input.GetAxis("Mouse X") * sensitivityX, 0);
			}
			else
			{
				rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
				rotationY = Mathf.Clamp (rotationY, minimumY, maximumY);
				
				transform.localEulerAngles = new Vector3(-rotationY, transform.localEulerAngles.y, 0);
			}
			
			if(Input.GetKey(KeyCode.W))
			{
                transform.position += transform.forward * 2.0f * Time.deltaTime;
			}
			else if(Input.GetKey(KeyCode.S))
			{
                transform.position -= transform.forward * 2.0f * Time.deltaTime;
			}
			
			if(Input.GetKey(KeyCode.A))
			{
                transform.position -= transform.right * 2.0f * Time.deltaTime;
			}
			else if(Input.GetKey(KeyCode.D))
			{
				transform.position += transform.right * 2.0f * Time.deltaTime;
			}
			
			if(Input.GetKey(KeyCode.E))
			{
                transform.position += transform.up * 2.0f * Time.deltaTime;
			}
			else if(Input.GetKey(KeyCode.Q))
			{
                transform.position -= transform.up * 2.0f * Time.deltaTime;
			}
		}
	}

    void OnGUI()
    {
        GUI.DrawTexture(new Rect((Screen.width - CrossHair.width) * 0.5f, (Screen.height * 0.5f - CrossHair.height * 0.75f), CrossHair.width, CrossHair.height), CrossHair);
    }
}