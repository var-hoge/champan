using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace App
{

	public class GameController : TadaLib.Util.SingletonMonoBehaviour<GameController>
	{
		[SerializeField] private GameObject playerPrefab;
		[SerializeField] private InputActionAsset InputActions;

		public int MaxPlayerCount => InputActions.controlSchemes.Count;

		public int PlayerCount => _playerInputs.Count;
		public PlayerInput GetPlayerInput(int idx)
		{
			// force...
			while (PlayerInput.all.Count < MaxPlayerCount)
			{
				AddPlayer();
			}
			return _playerInputs[idx];
		}

		List<PlayerInput> _playerInputs = new List<PlayerInput>();

		private void Start()
		{
			// add max player at initialization
			while (PlayerInput.all.Count < MaxPlayerCount)
			{
				AddPlayer();
			}
		}

		private void Update()
		{
			if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter))
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
			PlayerInput playerInput = PlayerInput.Instantiate(playerPrefab, playerIndex, schemeMapping, pairWithDevices: Keyboard.current);
			DontDestroyOnLoad(playerInput);
			_playerInputs.Add(playerInput);
		}

		public static IEnumerable<string> GetSEPath(string path, int count)
		{
			for (var n = 1; n <= count; ++n)
			{
				yield return (n < 10) ? $"{path}0{n}" : $"{path}{n}";
			}
		}
	}
}