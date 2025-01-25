using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameController : MonoBehaviour
{
	public static GameController Instance { get; private set; }

	private readonly List<string> schemeMappings = new List<string>()
	{
		"Arrows",
		"WASD"
	};

	[SerializeField] private GameObject playerPrefab;
	[SerializeField] private PlayerInputManager inputManager;

	public int MaxPlayerCount => schemeMappings.Count;

	private List<PlayerInput> players = new();

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
		{
			if (players.Count < MaxPlayerCount)
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
		int playerIndex = players.Count;
		string schemeMapping = schemeMappings[playerIndex];
		Joystick joystick = Joystick.all.Count > playerIndex ? Joystick.all[playerIndex] : null;
		PlayerInput playerInput = PlayerInput.Instantiate(playerPrefab, playerIndex, schemeMapping, pairWithDevices: new InputDevice[] { Keyboard.current, joystick });

		Debug.Log($"Adding Player {playerIndex} using scheme {schemeMapping}");
		if (joystick != null)
		{
			Debug.Log($"Using joystick {joystick.name}");
		}

		players.Add(playerInput);
	}
}
