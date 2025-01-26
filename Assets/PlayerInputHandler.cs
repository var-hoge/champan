using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
	public Action<Vector2> OnMove;
	public Action OnAction;

	[SerializeField] private PlayerInput playerInput;

	public void Move(InputAction.CallbackContext callback)
	{
		Vector2 value = callback.ReadValue<Vector2>();
		OnMove?.Invoke(value);
	}

	public void DoAction(InputAction.CallbackContext callback)
	{
		if (!callback.performed) return;

		OnAction?.Invoke();
	}
}
