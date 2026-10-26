#!/usr/bin/env python3
"""Static TB19 integration checks, not a Unity compile."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
a = (root / "StoriesOfYggdrasilOSCContactSystem.cs").read_bytes()
b = (root / "Assets/Stories Of Yggdrasil/Editor/StoriesOfYggdrasilOSCContactSystem.cs").read_bytes()
assert a == b, "Editor copies diverged"
s = a.decode("utf-8-sig")

def exactly_one(fragment):
    assert s.count(fragment) == 1, f"Expected exactly once: {fragment}"

for name in (
    "private void DrawMenuBuilder()",
    "private void ApplyManualMenuDraft()",
    "private void DrawMovementLockAuthoring()",
    "private void InstallMovementLockLayer(Type behaviourType)",
    "private string BuildContactProvenanceReport()",
):
    exactly_one(name)

# Menu assets can only be written by explicit manual Apply, not Install All Hooks.
installer = s.split("private void InstallAllBridgeHooks()", 1)[1].split("\n        private ", 1)[0]
assert "AddCombatToggle(" not in installer
assert "InstallMovementLockLayer(" not in installer
assert "AssetDatabase.CopyAsset(targetPath, backupPath)" in s
assert "AssetDatabase.SaveAssets();" in s
assert "ManualMenuHasChanges()" in s
assert "manualMenuDraft.Count > ManualMenuCapacity" in s
assert "ManualMenuReaches(" in s
assert 'GUILayout.Button("APPLY TO SELECTED MENU"' in s
assert 'GUILayout.Button("INSTALL MOVEMENT LOCK LAYER (OPT-IN)"' in s
assert "AddBoolTransition(unlocked, locked, MovementLockParameter, true)" in s
assert "AddBoolTransition(locked, unlocked, MovementLockParameter, false)" in s
assert "layer.stateMachine.defaultState = unlocked" in s

# Avoid claiming this passes a compiler. It only protects the integration contract.
print("TB19 static integration checks passed (NOT Unity/VRChat SDK compiled).")
