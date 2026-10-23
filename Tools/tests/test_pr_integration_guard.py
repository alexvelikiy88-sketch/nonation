"""Pure, offline regression tests for NONATION integration guard."""
import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import pr_integration_guard as guard


class OwnershipTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        agents = Path(__file__).resolve().parents[2] / "AGENTS.md"
        cls.rules = guard.parse_ownership(agents.read_text(encoding="utf-8"))

    def test_rules_read_actual_document_not_a_hardcoded_snapshot(self):
        self.assertEqual(
            guard.owner_of("Assets/MIR/Scripts/MirGame.cs.meta", self.rules),
            "agent/unity-webgl")
        self.assertEqual(
            guard.owner_of("Assets/MIR/Scripts/MirEconomy.cs", self.rules),
            "agent/backend-simulation")
        self.assertEqual(
            guard.owner_of("Assets/MIR/Scripts/MirHumanFactory.cs", self.rules),
            "agent/graphics-ui")
        self.assertEqual(
            guard.owner_of("Server/Nonation.Tests/Program.cs", self.rules),
            "agent/backend-simulation")
        self.assertEqual(guard.owner_of("TASKS.md", self.rules), "manager")

    def test_report_is_only_its_own_agents(self):
        for branch, report in guard.REPORTS.items():
            self.assertEqual(guard.owner_of(report, self.rules), branch)
        errors, _ = guard.check_changes(
            [guard.Change("M", guard.REPORTS["agent/backend-simulation"])],
            "manager/audit", "", self.rules)
        self.assertTrue(errors)

    def test_agent_cannot_overwrite_other_agent_or_tasks(self):
        changes = [
            guard.Change("M", "TASKS.md"),
            guard.Change("M", "Assets/MIR/Scripts/MirHumanFactory.cs"),
        ]
        errors, _ = guard.check_changes(
            changes, "agent/backend-simulation", "", self.rules)
        self.assertEqual(len(errors), 2)

    def test_agent_can_change_only_its_own_code_and_report(self):
        files = [
            guard.Change("M", "Assets/MIR/Scripts/MirEconomy.cs"),
            guard.Change("M", "Server/Nonation.Tests/Program.cs"),
            guard.Change("M", guard.REPORTS["agent/backend-simulation"]),
        ]
        errors, warnings = guard.check_changes(
            files, "agent/backend-simulation", "", self.rules)
        self.assertEqual((errors, warnings), ([], []))

    def test_new_unassigned_scripts_require_manager_contract(self):
        errors, _ = guard.check_changes(
            [guard.Change("A", "Assets/MIR/Scripts/UnassignedNewCode.cs")],
            "agent/graphics-ui", "", self.rules)
        self.assertTrue(errors)

    def test_manager_cross_owner_requires_per_file_explanation(self):
        path = "Assets/MIR/Scripts/MirEconomy.cs"
        ch = [guard.Change("M", path)]
        bad, _ = guard.check_changes(ch, "manager/integration", "", self.rules)
        good, _ = guard.check_changes(
            ch, "manager/integration", "INTEGRATION-EXCEPTION: " + path,
            self.rules)
        self.assertTrue(bad)
        self.assertEqual(good, [])

    def test_critical_deletion_never_passes_agent_review(self):
        target = "Assets/MIR/Scripts/MirPlayer.cs"
        errors, _ = guard.check_changes(
            [guard.Change("D", target)], "agent/unity-webgl",
            "DELETION-REVIEWED: " + target, self.rules)
        self.assertTrue(errors)
        manager, _ = guard.check_changes(
            [guard.Change("D", target)], "manager/integration",
            "INTEGRATION-EXCEPTION: " + target + "\nDELETION-REVIEWED: " + target,
            self.rules)
        self.assertEqual(manager, [])

    def test_rename_checks_both_paths(self):
        errors, warnings = guard.check_changes(
            [guard.Change("R100", "Assets/MIR/Scripts/MirHumanFactory.cs",
                          "Assets/MIR/Scripts/MirPlayer.cs")],
            "agent/graphics-ui", "", self.rules)
        self.assertTrue(errors)
        self.assertTrue(warnings)

    def test_external_assets_need_license_evidence(self):
        change = [guard.Change("A", "Assets/MIR/Art/People/person.fbx")]
        errors, _ = guard.check_changes(
            change, "agent/graphics-ui", "", self.rules)
        accepted, _ = guard.check_changes(
            change, "agent/graphics-ui",
            "ASSET-LICENSE-EVIDENCE: CC0 source/version/readback", self.rules)
        self.assertTrue(errors)
        self.assertEqual(accepted, [])

    def test_restricted_asset_names_are_denied_even_if_license_asserted(self):
        errors, _ = guard.check_changes(
            [guard.Change("A", "Assets/MIR/Art/Honda_Civic.fbx")],
            "agent/graphics-ui", "ASSET-LICENSE-EVIDENCE: unknown", self.rules)
        self.assertTrue(errors)

    def test_secret_pattern_is_scanned_only_in_added_lines(self):
        fake = "ghp_" + "X" * 40
        errors, _ = guard.check_changes(
            [], "manager/example", "", self.rules, "+" + fake)
        self.assertTrue(errors)
        none, _ = guard.check_changes(
            [], "manager/example", "", self.rules, "-" + fake)
        self.assertEqual(none, [])

    def test_method_return_type_change_is_not_public_api_deletion(self):
        old = "public int TotalKnownMoney() { return 0; }"
        new = "public long TotalKnownMoney() { return 0L; }"
        self.assertEqual(guard.missing_public_symbols(old, new), set())
        self.assertEqual(
            guard.missing_public_symbols(old, "public long Different() => 0L;"),
            {"TotalKnownMoney"})

    def test_name_status_zero_records_are_parsed_without_split_on_whitespace(self):
        items = guard.parse_name_status_zero(
            b"M\x00Assets/MIR/Scripts/MirEconomy.cs\x00"
            b"R100\x00old name.cs\x00new name.cs\x00"
        )
        self.assertEqual(items[0], guard.Change(
            "M", "Assets/MIR/Scripts/MirEconomy.cs"))
        self.assertEqual(items[1], guard.Change(
            "R100", "new name.cs", "old name.cs"))
        with self.assertRaises(ValueError):
            guard.parse_name_status_zero(b"R100\x00only-one\x00")

    def test_bad_policy_fails_closed(self):
        with self.assertRaises(ValueError):
            guard.parse_ownership("## no agent ownership table")


if __name__ == "__main__":
    unittest.main()
