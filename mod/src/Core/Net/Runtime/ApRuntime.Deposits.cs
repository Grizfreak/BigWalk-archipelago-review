using UnityEngine;

namespace BigWalkArchipelago.Core.Net
{
    internal partial class ApRuntime : MonoBehaviour
    {
        // Counted across all monuments, never per tower, so no gourd distribution can lock a seed.
        private static void SetDepositBudgetOnce()
        {
            if (_depositBudgetSet || Connection.Status != ApConnection.ConnectionStatus.Connected)
                return;

            _depositBudgetSet = true;
            var filled = CosmeticMonumentFillTracker.GetFilledMonumentCount();
            var owed = Math.Max(0, Connection.StoredDeposits - filled);
            _depositsOnServer = Connection.StoredDeposits;
            _skipGourdSpawns = owed > 0 ? CosmeticMonumentFillTracker.PutDepositsBack(owed) : 0;
            Plugin.Log.LogInfo(
                $"[{nameof(ApRuntime)}] Deposits: the server remembers {Connection.StoredDeposits}, this save holds "
                + $"{filled} -> {_skipGourdSpawns} slot(s) marked filled, their gourds come back as the monuments load.");
        }

        private static void StoreDepositsIfRaised()
        {
            if (Connection.Status != ApConnection.ConnectionStatus.Connected)
                return;

            if (_depositCount > _depositsOnServer)
            {
                _depositsOnServer = _depositCount;
                Connection.StoreDeposits(_depositCount);
            }
        }

        private static void ReportDeposits()
        {
            var amounts = Connection.SlotData.DepositLocationAmounts;
            if (amounts.Length == 0)
                return;

            var filled = _depositCount;
            if (filled <= _lastReportedDepositCount)
                return;

            _lastReportedDepositCount = filled;
            SendBuffer.Clear();

            foreach (var amount in amounts)
            {
                if (amount > filled)
                    break;

                var id = ApLocationIds.DepositLocationId(amount);
                if (Connection.BelongsToSlot(id))
                    SendBuffer.Add(id);
            }

            if (SendBuffer.Count > 0)
                Connection.SendChecks(SendBuffer.ToArray());
        }
    }
}
