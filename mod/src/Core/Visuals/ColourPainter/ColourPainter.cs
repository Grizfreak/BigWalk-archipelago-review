using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigWalkArchipelago.Core.Visuals
{
    // Paints buoys, guns, gadgets, lasers and fireworks in the seed's colours, on each machine.
    internal static partial class ColourPainter
    {
        private const string Tag = "[" + nameof(ColourPainter) + "]";
        internal const string RainbowMarker = " rainbow";

        private static bool _dirty = true;
        private static bool _wasReady;
        private static readonly List<ParticleSystem> RainbowShots = new();

        internal static void Repaint() => _dirty = true;

        // Every frame: the world once ready or recoloured, and the rainbow guns' shots.
        internal static void Tick()
        {
            var ready = WorldManager.isReadyForEffects;
            if (ready && !_wasReady)
                _dirty = true;
            if (!ready)
            {
                RainbowShots.Clear();
                Lamps.Clear();
            }
            _wasReady = ready;

            if (ready && _dirty)
            {
                _dirty = false;
                PaintWorld();
            }

            TickRainbow();
            if (ready)
                KeepTheLamps();
        }

        private static void PaintWorld()
        {
            int buoys = 0, guns = 0, gadgets = 0;
            foreach (var prop in UnityEngine.Object.FindObjectsByType<Prop>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (prop == null)
                    continue;
                try
                {
                    var key = KeyOf(prop);
                    if (PaintIfBuoy(prop.gameObject, key))
                        buoys++;
                    else if (PaintIfGun(prop.gameObject))
                        guns++;
                    else if (PaintIfLaser(prop.gameObject, key))
                        gadgets++;
                    else if (PaintIfGadget(prop.gameObject, key))
                        gadgets++;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"{Tag} Could not paint {prop.gameObject.name}: {ex.Message}");
                }
            }

            var fireworks = PaintFireworks();

            if (Palette.Active || RainbowShots.Count > 0)
                Plugin.Log.LogInfo($"{Tag} Painted {buoys} buoy(s), {guns} flare gun(s), {gadgets} gadget(s) and {fireworks} firework launcher(s){(Palette.Active ? string.Empty : " (rainbow only)")}.");
        }

        // A clone just built (GadgetItemSpawner), on every machine.
        internal static void PaintClone(GadgetKind kind, GameObject clone, bool rainbow)
        {
            try
            {
                if (rainbow)
                {
                    clone.name += RainbowMarker;
                    PaintIfGun(clone);
                    return;
                }

                var prop = clone.GetComponent<Prop>();
                if (kind == GadgetKind.Lamp)
                    PaintIfBuoy(clone, prop != null ? prop.savablePropGuid : clone.name);
                else if (kind == GadgetKind.Laser)
                    PaintIfLaser(clone, prop != null ? prop.savablePropGuid : clone.name);
                else if (!PaintIfGun(clone))
                    PaintIfGadget(clone, prop != null ? prop.savablePropGuid : clone.name);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Could not paint a {kind} clone: {ex.Message}");
            }
        }

        // A received copy by its netId, the same everywhere: its own colour, not its template's.
        internal static void PaintCopy(GameObject copy, uint netId)
        {
            if (copy == null || netId == 0 || copy.name.Contains(RainbowMarker))
                return;
            try
            {
                var key = CopyKey(netId);
                if (!PaintIfBuoy(copy, key) && !PaintIfLaser(copy, key))
                    PaintIfGadget(copy, key);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Could not paint copy {netId}: {ex.Message}");
            }
        }

        private static string CopyKey(uint netId) => "copy " + netId;

        // A world repaint keeps each copy's own colour (a guest repaints after copies arrive).
        private static string KeyOf(Prop prop)
        {
            if (!ReceivedItemSpawner.IsCosmeticClone(prop))
                return prop.savablePropGuid;
            var identity = prop.GetComponent<Mirror.NetworkIdentity>();
            return identity != null && identity.netId != 0 ? CopyKey(identity.netId) : prop.savablePropGuid;
        }

        private static bool StartsWithAny(string name, string[] names)
        {
            foreach (var candidate in names)
            {
                if (name.StartsWith(candidate, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }
}
