using UnityEngine;
using TadaLib.ActionStd;
using KanKikuchi.AudioManager;
using System.Collections.Generic;
using System.Linq;

namespace App.Actor.Gimmick.RespawnBubble
{
	public class PlayerSpawner : MonoBehaviour
	{
		[SerializeField]
		private RespawnBubble _respawnBubble = null;
		[SerializeField] Bubble.Bubble _bubble = null;
		private MoveInfoCtrl _moveInfoCtrl = null;
		[SerializeField] float _BGMVolumeRate = 1f;

		[SerializeField] private int bubbleCountMin = 1;
		[SerializeField] private int bubbleCountMax = 5;

		private List<string> _SEPath = null;
		private List<string> SEPath => _SEPath ??= GameController.GetSEPath("SE/Player Death/Player_Death_", 9).ToList();

		// Start is called once before the first execution of Update after the MonoBehaviour is created
		void Start()
		{
			_moveInfoCtrl = GetComponent<MoveInfoCtrl>();
			//BGMManager.Instance.Play(BGMPath.FIGHT, _BGMVolumeRate);
		}

		// Update is called once per frame
		void Update()
		{
			foreach (var player in _moveInfoCtrl.RideObjects)
			{
				player.GetComponent<App.Actor.Player.DataHolder>().IsDead = true;

				var path = SEPath[Random.Range(0, SEPath.Count)];
				SEManager.Instance.Play(path, 20f);

				// respawn player
				var camera = Camera.main;
				var topRight = camera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, camera.nearClipPlane));
				var x = Mathf.Clamp(player.transform.position.x, -topRight.x, topRight.x);
				var y = topRight.y + 0.5f;
				var respawnBubble = Instantiate(_respawnBubble, new(x, y, 0), Quaternion.identity);
				respawnBubble.Init(player);
				player.transform.position = new Vector3(0, -30, 0);

				var xPosition = x - 2f;
				var xForce = -15f;

				int count = Random.Range(bubbleCountMin, bubbleCountMax);
				for (var n = 0; n < count; ++n)
				{
					var bubble = Instantiate(_bubble, new(xPosition, -y, 0), Quaternion.identity);
					bubble.Init(xForce);
					xPosition += 2f;
					xForce += 15f;
				}
			}
		}
	}
}