"""Tests of tools/check_debug_isolation.py on small source trees, and on the mod itself."""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import check_debug_isolation as isolation  # noqa: E402

DEBUG_TOOL = """namespace BigWalkArchipelago.Debug
{
    internal static class Tool { internal static void Run() { } }
}
"""

PARTIAL = """namespace BigWalkArchipelago.Core
{
    internal static partial class Shared
    {
        internal static int DebugOnly(int x) => x;
    }
}
"""


def _tree(tmp_path: Path, files: dict[str, str]) -> Path:
    for name, text in files.items():
        path = tmp_path / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")
    return tmp_path


def test_the_mod_passes():
    assert isolation.check(isolation.REPO / "mod" / "src") == []


def test_a_harmony_patch_in_debug_fails(tmp_path):
    src = _tree(tmp_path, {"Debug/Patch.cs": "[HarmonyPatch(typeof(A), \"B\")]\nclass P { }\n"})
    assert any("[HarmonyPatch]" in e for e in isolation.check(src))


def test_a_commented_harmony_patch_passes(tmp_path):
    src = _tree(tmp_path, {"Debug/Patch.cs": "// [HarmonyPatch] was here\nclass P { }\n"})
    assert isolation.check(src) == []


def test_a_debug_type_outside_if_debug_fails(tmp_path):
    src = _tree(tmp_path, {"Debug/Tool.cs": DEBUG_TOOL, "Plugin.cs": "void Load() { Debug.Tool.Run(); }\n"})
    assert any("Debug namespace" in e for e in isolation.check(src))


def test_the_full_namespace_outside_if_debug_fails(tmp_path):
    src = _tree(tmp_path, {"Debug/Tool.cs": DEBUG_TOOL,
                           "Plugin.cs": "void Load() { BigWalkArchipelago.Debug.Tool.Run(); }\n"})
    assert isolation.check(src)


def test_a_debug_type_inside_if_debug_passes(tmp_path):
    src = _tree(tmp_path, {"Debug/Tool.cs": DEBUG_TOOL,
                           "Plugin.cs": "#if DEBUG\nDebug.Tool.Run();\n#endif\n"})
    assert isolation.check(src) == []


def test_the_else_of_if_debug_is_shared(tmp_path):
    src = _tree(tmp_path, {"Debug/Tool.cs": DEBUG_TOOL,
                           "Plugin.cs": "#if DEBUG\nint a;\n#else\nDebug.Tool.Run();\n#endif\n"})
    assert isolation.check(src)


def test_unity_debug_log_passes(tmp_path):
    src = _tree(tmp_path, {"Debug/Tool.cs": DEBUG_TOOL, "Plugin.cs": "Debug.Log(\"hello\");\n"})
    assert isolation.check(src) == []


def test_a_member_of_a_debug_partial_outside_if_debug_fails(tmp_path):
    src = _tree(tmp_path, {"Debug/Partials/Shared.cs": PARTIAL, "Core/Use.cs": "var y = Shared.DebugOnly(1);\n"})
    assert any("Shared.DebugOnly" in e for e in isolation.check(src))


def test_build_outputs_are_skipped(tmp_path):
    src = _tree(tmp_path, {"Debug/Tool.cs": DEBUG_TOOL, "obj/Generated.cs": "Debug.Tool.Run();\n"})
    assert isolation.check(src) == []
