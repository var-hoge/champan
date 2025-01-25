using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
	[SerializeField] private PlayerInput playerInput;

	public void Move(InputAction.CallbackContext callback)
	{
		Vector2 value = callback.ReadValue<Vector2>();
		Debug.Log($"Player {playerInput.playerIndex}: Move {value}");
	}

	public void DoAction(InputAction.CallbackContext callback)
	{
		if (!callback.performed) return;
		Debug.Log($"Player {playerInput.playerIndex}: Action");
	}
}
