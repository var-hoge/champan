using UnityEngine;
using UnityEngine.InputSystem;

public class GameController : MonoBehaviour
{
	public static GameController Instance { get; private set; }

	[SerializeField] private GameObject playerPrefab;
	[SerializeField] private InputActionAsset InputActions;

	public int MaxPlayerCount => InputActions.controlSchemes.Count;

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
		{
			if (PlayerInput.all.Count < MaxPlayerCount)
			{
				AddPlayer();
			}
			else
			{
				Debug.LogWarning($"Max player count reached");
			}
		}
	}

	private void AddPlayer()
	{
		int playerIndex = PlayerInput.all.Count;
		string schemeMapping = InputActions.controlSchemes[playerIndex].name;
		Joystick joystick = Joystick.all.Count > playerIndex ? Joystick.all[playerIndex] : null;
		PlayerInput playerInput = PlayerInput.Instantiate(playerPrefab, playerIndex, schemeMapping, pairWithDevices: new InputDevice[] { Keyboard.current, joystick });

		Debug.Log($"Adding Player {playerIndex} using scheme {playerInput.currentControlScheme}");
		if (joystick != null)
		{
			Debug.Log($"Using joystick {joystick.name}");
		}
	}
}
