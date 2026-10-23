"""Regression for Unity Editor audit and shared 64-bit economy aggregate.

The Unity Cloud Build #6 failed with CS0266 when its Editor audit assigned
MirEconomy.TotalKnownMoney() (long) to int. This source gate prevents that drift.
"""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]

class UnityEditorEconomyCompatibilityTests(unittest.TestCase):
    def test_editor_audit_tracks_long_ledger_aggregate(self):
        audit = (ROOT / "Assets/MIR/Editor/MirIntegrityAudit.cs").read_text(encoding="utf-8")
        domain = (ROOT / "Assets/MIR/Scripts/MirEconomy.cs").read_text(encoding="utf-8")
        self.assertRegex(domain, r"\bpublic\s+long\s+TotalKnownMoney\s*\(")
        self.assertRegex(audit, r"\blong\s+before\s*=\s*econ\.TotalKnownMoney\s*\(")
        self.assertNotRegex(audit, r"\bint\s+before\s*=\s*econ\.TotalKnownMoney\s*\(")
        self.assertIn("econ.TotalKnownMoney()==before", audit)

if __name__ == "__main__":
    unittest.main()
