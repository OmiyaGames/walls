using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LD48
{
	/// <summary>
	/// Stolen from
	/// https://sharpcoderblog.com/blog/unity-3d-character-controller-moving-platform-support
	/// </summary>
	public class SC_MovingPlatform : MonoBehaviour
	{
		[SerializeField]
		float activateThreshold = 0.01f;
		[SerializeField]
		[OmiyaGames.ReadOnly]
		Transform activePlatform;

		CharacterController controller;
		Vector3 moveDirection;
		Vector3 activeGlobalPlatformPoint;
		Vector3 activeLocalPlatformPoint;
		Quaternion activeGlobalPlatformRotation;
		Quaternion activeLocalPlatformRotation;

		// Start is called before the first frame update
		void Start()
		{
			controller = GetComponent<CharacterController>();
		}

		// Update is called once per frame
		void FixedUpdate()
		{
			if(activePlatform != null)
			{
				Vector3 newGlobalPlatformPoint = activePlatform.TransformPoint(activeLocalPlatformPoint);
				moveDirection = newGlobalPlatformPoint - activeGlobalPlatformPoint;
				if(moveDirection.magnitude > activateThreshold)
				{
					controller.Move(moveDirection);
				}
				if(activePlatform)
				{
					// Support moving platform rotation
					Quaternion newGlobalPlatformRotation = activePlatform.rotation * activeLocalPlatformRotation;
					Quaternion rotationDiff = newGlobalPlatformRotation * Quaternion.Inverse(activeGlobalPlatformRotation);
					// Prevent rotation of the local up vector
					rotationDiff = Quaternion.FromToRotation(rotationDiff * Vector3.up, Vector3.up) * rotationDiff;
					transform.rotation = rotationDiff * transform.rotation;
					transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);

					UpdateMovingPlatform();
				}
			}
			else
			{
				if(moveDirection.magnitude > activateThreshold)
				{
					moveDirection = Vector3.Lerp(moveDirection, Vector3.zero, Time.deltaTime);
					controller.Move(moveDirection);
				}
			}
		}

		void OnControllerColliderHit(ControllerColliderHit hit)
		{
			// Make sure we are really standing on a straight platform *NEW*
			// Not on the underside of one and not falling down from it either!
			if(hit.moveDirection.y < -0.9 && hit.normal.y > 0.41)
			{
				if(activePlatform != hit.collider.transform)
				{
					activePlatform = hit.collider.transform;
					UpdateMovingPlatform();
				}
			}
			else
			{
				activePlatform = null;
			}
		}

		void UpdateMovingPlatform()
		{
			activeGlobalPlatformPoint = transform.position;
			activeLocalPlatformPoint = activePlatform.InverseTransformPoint(transform.position);
			// Support moving platform rotation
			activeGlobalPlatformRotation = transform.rotation;
			activeLocalPlatformRotation = Quaternion.Inverse(activePlatform.rotation) * transform.rotation;
		}
	}
}