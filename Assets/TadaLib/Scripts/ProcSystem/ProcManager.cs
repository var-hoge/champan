using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TadaLib.ProcSystem
{
    /// <summary>
    /// 更新マネージャー
    /// このクラスのUpdate関数は最後に呼ばれる想定
    /// </summary>
    public class ProcManager : Util.SingletonMonoBehaviour<ProcManager>
    {
        #region メソッド
        /// <summary>
        /// 更新処理用の関数呼び出し追加
        /// 1フレーム限り
        /// @memo: 1フレーム限りにしているのは、処理順を狂わせないようにするため
        /// </summary>
        /// <param name="proc"></param>
        public void AddUpdateInvoke(BaseProc proc)
        {
            _procListForUpdate.Add(proc);
        }

        /// <summary>
        /// マネージャー更新処理用の関数呼び出しを追加
        /// </summary>
        /// <param name="proc"></param>
        public void AddManagerUpdateInvoke(IProcManagerUpdate proc, ManagerProcSection section)
        {
            EnsureSections();
            _procManagerList[section].Add(proc);
        }
        #endregion

        #region Monobehaviorの実装
        void Start()
        {
            EnsureSections();
        }

        void Update()
        {
            // シーンの再ロード中は Start より先に Update が走ることがあるため、
            // 順序に依存しないようにする
            // (ネットワーク対戦では Fusion がシーンを読み直す)
            EnsureSections();

            // ====================================================
            // イベント関数の処理順はコードを読んでください
            // ※ 各イベント関数内の呼び出し順はUnity標準機能の「Script Execution Order」の順番通り
            // ====================================================

            foreach (var proc in _procManagerList[ManagerProcSection.BeforeUpdate])
            {
                proc.OnUpdate();
            }

            foreach (var proc in _procListForUpdate)
            {
                // フレーム途中で破棄されている場合がある
                // (ネットワーク対戦では Runner.Despawn が即座に破棄するため。
                //  Unity の Destroy はフレーム末まで遅延するのでオフラインでは起きない)
                if (proc == null)
                {
                    continue;
                }

                (proc as IProcUpdate)?.OnUpdate();
            }

            foreach (var proc in _procManagerList[ManagerProcSection.BeforeMove])
            {
                proc.OnUpdate();
            }

            foreach (var proc in _procListForUpdate)
            {
                if (proc == null)
                {
                    continue;
                }

                (proc as IProcMove)?.OnMove();
            }

            foreach (var proc in _procManagerList[ManagerProcSection.BeforePhysicsMove])
            {
                proc.OnUpdate();
            }

            foreach (var proc in _procListForUpdate)
            {
                if (proc == null)
                {
                    continue;
                }

                (proc as IProcPhysicsMove)?.OnPhysicsMove();
            }

            foreach (var proc in _procManagerList[ManagerProcSection.BeforePostMove])
            {
                proc.OnUpdate();
            }

            foreach (var proc in _procListForUpdate)
            {
                if (proc == null)
                {
                    continue;
                }

                (proc as IProcPostMove)?.OnPostMove();
            }

            _procListForUpdate.Clear();

            foreach (var proc in _procManagerList[ManagerProcSection.Last])
            {
                proc.OnUpdate();
            }

            foreach (ManagerProcSection section in System.Enum.GetValues(typeof(ManagerProcSection)))
            {
                _procManagerList[section].Clear();
            }
        }
        #endregion


        #region private メソッド
        /// <summary>
        /// セクションごとのリストを用意する
        /// 何度呼んでも既存のリストは壊さない
        /// </summary>
        void EnsureSections()
        {
            foreach (ManagerProcSection section in System.Enum.GetValues(typeof(ManagerProcSection)))
            {
                if (!_procManagerList.ContainsKey(section))
                {
                    _procManagerList[section] = new List<IProcManagerUpdate>();
                }
            }
        }
        #endregion

        #region private フィールド
        List<BaseProc> _procListForUpdate = new List<BaseProc>();
        Dictionary<ManagerProcSection, List<IProcManagerUpdate>> _procManagerList = new Dictionary<ManagerProcSection, List<IProcManagerUpdate>>();
        #endregion
    }
}