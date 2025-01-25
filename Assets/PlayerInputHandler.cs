using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
	[SerializeField] private PlayerInput playerInput;

	public void Move(InputAction.CallbackContext callback)
	{
		Debug.Log($"Player {playerInput.playerIndex}: Move");
	}

	public void DoAction(InputAction.CallbackContext callback)
	{
		Debug.Log($"Player {playerInput.playerIndex}: Action");
	}
}
