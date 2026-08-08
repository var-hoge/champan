using App.Actor.Gimmick.Crown;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using KanKikuchi.AudioManager;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

namespace App.Actor.Gimmick.Bubble
{
    public class BobbleGenerator : MonoBehaviour
    {
        [SerializeField] private int _numOfBubble = 6;
        [SerializeField] private GameObject _bubblePrafab;
        [SerializeField] private Transform _bubbleRoot;

        void Start()
        {
            // バブルの配置は全員で一致している必要があるため、権威を持つ側だけが生成する
            // (オフラインでは常に権威を持つので、従来通りここで生成される)
            if (!Network.NetworkSession.HasAuthority)
            {
                return;
            }

            List<Bubble> bubbles = new();
            for (var n = 0; n < _numOfBubble; ++n)
            {
                bubbles.Add(Generate());
            }

            Bubble.SetupCrown(bubbles[Random.Range(0, bubbles.Count - 1)]);
            Crown.Manager.Instance.CrownBubble.OnDestroyEvent += TeleportCrown;
        }

        private void TeleportCrown()
        {
            // 王冠バブルが破棄された直後に呼ばれるため、参照が既に外れていることがある
            var crownBubble = Crown.Manager.Instance.CrownBubble;
            if (crownBubble != null)
            {
                crownBubble.OnDestroyEvent -= TeleportCrown;
            }

            if (Crown.Manager.Instance.IsShieldDestroyed)
            {
                return;
            }

            // �����͒x������
            TeleportImpl().Forget();
        }

        private Bubble Generate(bool isCrown = false)
        {
            var position = new Vector3(Random.Range(-10f, 10f), Random.Range(-4.0f, isCrown ? 2.5f : 4.0f), 1f);
            var bubble = Network.NetworkSession.Spawn(_bubblePrafab, position, Quaternion.identity);
            bubble.transform.SetParent(_bubbleRoot);
            bubble.GetComponent<BubbleAnimator>().AnimationEnabled = true;
            return bubble.GetComponent<Bubble>();
        }

        async UniTask TeleportImpl()
        {
            //Transform crown = Crown.Manager.Instance.CrownBubble.CrownSpriteRenderer;
            //crown.SetParent(null);

            if (Manager.Instance.DoFakeFinishStaging)
            {
                // �N���E���������鉉�o���o��܂ő҂�
                await UniTask.WaitForSeconds(0.8f);
            }

            await UniTask.WaitForSeconds(0.05f);

            var bubbles = FindObjectsByType<Bubble>(FindObjectsSortMode.None)
                            .Where(bubble => bubble.IsTeleportableCrown)
                            .ToList();

            if (bubbles.Contains(Crown.Manager.Instance.CrownBubble))
            {
                bubbles.Remove(Crown.Manager.Instance.CrownBubble);
            }

            var target = (bubbles.Count == 0)
                            ? Generate(isCrown: true)
                            : bubbles[Random.Range(0, bubbles.Count - 1)];
            Bubble.SetupCrown(target);
            //crown.DOMove(Crown.Manager.Instance.CrownBubble.transform.position, 1f).OnComplete(() => { Destroy(crown.gameObject); }).Play();

            Crown.Manager.Instance.CrownBubble.OnDestroyEvent += TeleportCrown;

            SEManager.Instance.Play(SEPath.CROWN_BUBBLE_REPOSITION);
        }
    }
}