using System;
using UnityEngine;
using Manasik.Core;

namespace Manasik.Gameplay
{
    /// <summary>
    /// Handles gem rewards and tracks level-completion bonuses.
    /// Subscribe to OnGemsEarned to update the HUD counter in real time.
    /// </summary>
    public class RewardSystem : MonoBehaviour
    {
        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Fired whenever gems are awarded. Carries the amount and new total.</summary>
        public event Action<int, int> OnGemsEarned; // (amountEarned, newTotal)

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Awards a gem bonus and persists it through the GameManager.
        /// </summary>
        public void AwardGems(int amount)
        {
            if (amount <= 0) return;

            GameManager.Instance?.AddGems(amount);
            int newTotal = GameManager.Instance?.ProfileService.GetProfile().Gems ?? 0;

            Debug.Log($"[RewardSystem] Awarded {amount} gems. Total: {newTotal}");
            OnGemsEarned?.Invoke(amount, newTotal);
        }

        /// <summary>
        /// Grants the level-completion bonus defined in LevelData.
        /// Should be called once when a level is successfully completed.
        /// </summary>
        public void GrantCompletionBonus(Data.LevelData levelData)
        {
            if (levelData == null) return;
            AwardGems(levelData.CompletionBonusGems);
        }
    }
}
