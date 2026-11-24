using Mirror;

namespace BigWalkArchipelago.Core.Towers
{
    // Sends a tower's check and hints its keys once its opening button has been pressed.
    internal static class TowerReach
    {
        private const string Tag = "[" + nameof(TowerReach) + "]";
        private const string LatchPrefix = "ap_tower_reached_";

        private struct Tower
        {
            internal string Prop;
            internal string Name;
            internal string SaveKey;

            // Towers without a button: reached by standing within the radius. Hints only.
            internal UnityEngine.Vector3 Centre;
            internal float Radius;
        }

        // In the apworld's order (data.reachable_towers).
        private static readonly Tower[] Towers =
        {
            new Tower { Prop = "bigKeyRedZone", Name = "Red Funnel Tower", SaveKey = "LookoutLightRed" },
            new Tower { Prop = "bigKeyGreenZone", Name = "Green Cup Tower", SaveKey = "LookoutLightGreen" },
            new Tower { Prop = "bigKeyBlueZone", Name = "Blue Castle Tower", SaveKey = "LookoutLightBlue" },
            new Tower { Prop = "bigKeyYellowZone", Name = "Yellow Twist Tower", SaveKey = "LookoutLightYellow" },
            new Tower { Prop = "bigKeyBoss", Name = "Black Tower", SaveKey = "BlackTowerInteriorDoor" },
            new Tower { Prop = "bigKeyIntro", Name = "Drawbridge", Centre = new UnityEngine.Vector3(-387.4f, 7.1f, -738.3f), Radius = 40f },
            new Tower { Prop = "bigKeyOverflow", Name = "Green Dome", Centre = new UnityEngine.Vector3(-126.9f, 46.3f, -447.4f), Radius = 20f },
        };

        internal static void Tick()
        {
            if (!NetworkServer.active || !WorldManager.isReadyForEffects || !ModConfig.ArchipelagoEnabled.Value)
                return;

            var slot = ApRuntime.SlotData;
            if (slot == null)
                return;

            foreach (var tower in Towers)
            {
                var hasCheck = Array.IndexOf(slot.TowerReachedKeys, tower.Prop) >= 0;
                var hasHints = slot.TowerReachHints.ContainsKey(tower.Prop);
                if ((!hasCheck && !hasHints) || Latched(tower.Prop) || !Reached(tower))
                    continue;

                SaveManager.SetIntValue(LatchPrefix + tower.Prop, 1);
                Plugin.Log.LogInfo($"{Tag} The {tower.Name} is reached ({tower.SaveKey ?? "a player is there"}).");
                if (hasCheck)
                    ApRuntime.QueueLocation(ApLocationIds.TowerPrefix + tower.Prop);
                if (hasHints)
                    ApRuntime.ScoutAsHints(slot.TowerReachHints[tower.Prop]);
                ApNotices.Post(!hasCheck ? $"Reached the {tower.Name}: hints for what waits here"
                    : hasHints ? $"Check: reached the {tower.Name}, hints for what waits here" : $"Check: reached the {tower.Name}");
            }
        }

        private static bool Reached(Tower tower)
        {
            if (tower.SaveKey != null)
                return SaveManager.GetIntValue(tower.SaveKey, 0, false) != 0;

            return PlayerWithin(tower);
        }

        internal static bool PlayerAt(string prop)
        {
            foreach (var tower in Towers)
            {
                if (tower.Prop == prop)
                    return tower.Radius > 0f && PlayerWithin(tower);
            }

            return false;
        }

        private static bool PlayerWithin(Tower tower)
        {
            var players = PlayerCharacter.allPlayerCharacters;
            if (players == null)
                return false;

            foreach (var player in players)
            {
                if (player != null && UnityEngine.Vector3.Distance(player.transform.position, tower.Centre) <= tower.Radius)
                    return true;
            }

            return false;
        }

        private static bool Latched(string prop) => SaveManager.GetIntValue(LatchPrefix + prop, 0, false) != 0;
    }
}
