using UnityEngine;

public class CameraCollisionDetector : MonoBehaviour
{
	[SerializeField] Collider _collider = null;

	private bool _hasCollision = false;


	private void OnTriggerEnter(Collider other)
	{
		_hasCollision = true;
	}

	private void OnTriggerExit(Collider other)
	{
		_hasCollision = false;
	}

	public bool HasCollisionHorizontal(float xAxis)
	{
		if (!_hasCollision)
		{
			return false;
		}

		Vector3 targetDir = xAxis > 0 ? -transform.right : transform.right;
		Debug.DrawRay(transform.position, targetDir, Color.red);
		return Physics.Raycast(transform.position, targetDir, _collider.bounds.extents.x);
	}

	public bool HasCollisionVertical(float yAxis)
	{
		if (!_hasCollision)
		{
			return false;
		}

		Vector3 targetDir = yAxis > 0 ? -transform.up : transform.up;
		Debug.DrawRay(transform.position, targetDir, Color.red);
		return Physics.Raycast(transform.position, targetDir, _collider.bounds.extents.y);
	}
}
