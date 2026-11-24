# lib/

Local copies of the DLLs the build needs, **not committed to git**. They come from an installation of the game with
BepInEx `6.0.0-be.781`, launched once: that first launch generates `BepInEx/interop/`. Copy them again after a game
update.

From `<game>/BepInEx/core/`:

- `0Harmony.dll`
- `BepInEx.Core.dll`
- `BepInEx.Unity.IL2CPP.dll`
- `Il2CppInterop.Runtime.dll`

From `<game>/BepInEx/interop/`:

- `Assembly-CSharp.dll`
- `AudioSystem.dll`
- `DissonanceVoip.dll`
- `Il2Cppmscorlib.dll`
- `Mirror.Transports.dll`
- `Mirror.dll`
- `Rewired_Core.dll`
- `Unity.RenderPipelines.Core.Runtime.dll`
- `Unity.RenderPipelines.Universal.Runtime.dll`
- `Unity.TextMeshPro.dll`
- `UnityEngine.AudioModule.dll`
- `UnityEngine.CoreModule.dll`
- `UnityEngine.IMGUIModule.dll`
- `UnityEngine.ParticleSystemModule.dll`
- `UnityEngine.PhysicsModule.dll`
- `UnityEngine.UI.dll`

Referencing one of the game's types needs its whole base-class chain, each link in its own DLL.

`<Nullable>disable</Nullable>` in the csproj is needed: the interop DLLs carry an incomplete `NullableAttribute`,
which fails the build (CS0656) otherwise.
