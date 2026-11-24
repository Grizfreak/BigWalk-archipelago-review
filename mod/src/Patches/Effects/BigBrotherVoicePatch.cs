using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine.Audio;

namespace BigWalkArchipelago.Patches.Effects
{
    // Big Brother trap: raises "VoicePitch"; SetFloat is invoked natively, its interop proxy fails.
    [HarmonyPatch(typeof(GlobalAudioEffects), "Update")]
    internal static class BigBrotherVoicePatch
    {
        private static IntPtr _setFloat;
        private static bool _broken;
        private static bool _raised;
        private static bool _logged;

        private static void Postfix(GlobalAudioEffects __instance)
        {
            if (_broken)
                return;
            var raise = TrapEffects.BrotherVoicePitch;
            if (raise == 1f && !_raised)
                return;

            try
            {
                var mixer = __instance.VoiceMixer;
                if (mixer == null)
                    return;
                var written = SetFloat(mixer, GlobalAudioEffects.PARAM_VOICE_PITCH, raise);
                if (!_logged)
                {
                    _logged = true;
                    Plugin.Log.LogInfo($"[TrapEffects] Big Brother voices: {GlobalAudioEffects.PARAM_VOICE_PITCH} on "
                                       + $"{mixer.name} {(written ? "written" : "not exposed there")}.");
                }

                if (!written)
                    _broken = true;
                _raised = raise != 1f;
            }
            catch (Exception ex)
            {
                _broken = true;
                Plugin.Log.LogWarning($"[TrapEffects] Big Brother voices off for this session: {ex.Message}");
            }
        }

        private static unsafe bool SetFloat(AudioMixer mixer, string name, float value)
        {
            if (_setFloat == IntPtr.Zero)
                _setFloat = IL2CPP.GetIl2CppMethod(Il2CppClassPointerStore<AudioMixer>.NativeClassPtr, false, "SetFloat",
                    "System.Boolean", "System.String", "System.Single");
            var args = stackalloc IntPtr[2];
            args[0] = IL2CPP.ManagedStringToIl2Cpp(name);
            args[1] = (IntPtr)(&value);
            var exception = IntPtr.Zero;
            var result = IL2CPP.il2cpp_runtime_invoke(_setFloat, IL2CPP.Il2CppObjectBaseToPtrNotNull(mixer), (void**)args,
                ref exception);
            Il2CppException.RaiseExceptionIfNecessary(exception);
            return *(bool*)IL2CPP.il2cpp_object_unbox(result);
        }
    }
}
