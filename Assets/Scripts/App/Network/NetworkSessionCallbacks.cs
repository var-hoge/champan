using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

namespace App.Network
{
    /// <summary>
    /// セッションの出入りを受け取る
    ///
    /// 誰かが部屋を出たら、その席を空けて選択もやり直しにする。
    /// 放っておくと、出た人のキャラが選ばれたまま残り続ける。
    ///
    /// Runner と同じオブジェクトに付けておくと Fusion が呼んでくれる。
    /// </summary>
    public class NetworkSessionCallbacks
        : MonoBehaviour
        , INetworkRunnerCallbacks
    {
        #region Fusion.INetworkRunnerCallbacks の実装
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"[NetworkSessionCallbacks] 部屋から出ました: {player}");

            // 席の割り当てを持っているのはホストだけ
            if (!NetworkSession.HasAuthority)
            {
                return;
            }

            var seatTable = NetworkSeatTable.Instance;
            if (seatTable == null)
            {
                return;
            }

            // 席を空ける前に、どの席だったかを控えておく
            var leftSeatIndices = new List<int>();
            for (int idx = 0; idx < seatTable.Seats.Length; ++idx)
            {
                if (seatTable.Seats[idx].Owner == player)
                {
                    leftSeatIndices.Add(idx);
                }
            }

            seatTable.ReleaseSeats(player);

            // キャラの選択もやり直しにする
            var charaSelectState = NetworkCharaSelectState.Instance;
            if (charaSelectState == null)
            {
                return;
            }

            foreach (var seatIdx in leftSeatIndices)
            {
                charaSelectState.ResetSeat(seatIdx);
            }
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        // NetworkInput はこの名前空間にも同名のものがあるため、どちらか明示する
        public void OnInput(NetworkRunner runner, Fusion.NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, Fusion.NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        #endregion
    }
}
