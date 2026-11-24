"""gourd_name: a name only the mod shows; Archipelago keeps "Gourd"."""

from unittest import TestCase

import worlds

from .. import data, gourd_name
from .bases import BigWalkTestBase

NAME = "Plumbus du Destin"


class TestDefault(BigWalkTestBase):
    run_default_tests = False

    def test_nothing_is_renamed(self) -> None:
        self.assertEqual(self.world.gourd_name, data.GOURD_ITEM_NAME)
        self.assertEqual(self.world.fill_slot_data()["gourd_name"], data.GOURD_ITEM_NAME)


class TestRenamed(BigWalkTestBase):
    options = {"gourd_name": NAME}
    run_default_tests = False

    def test_only_slot_data_carries_the_name(self) -> None:
        self.assertEqual(self.world.gourd_name, NAME)
        self.assertEqual(self.world.fill_slot_data()["gourd_name"], NAME)
        self.assertEqual(len(self.get_items_by_name(data.GOURD_ITEM_NAME)), data.MAX_MONUMENT_SLOTS)

    def test_the_data_package_keeps_gourd(self) -> None:
        package = worlds.network_data_package["games"]["Big Walk"]
        self.assertIn(data.GOURD_ITEM_NAME, package["item_name_to_id"])
        self.assertNotIn(NAME, package["item_name_to_id"])
        self.assertIs(type(self.world).modify_multidata, worlds.AutoWorld.World.modify_multidata)


class TestCleaning(TestCase):
    def clean(self, wanted: str) -> str:
        return gourd_name.clean(wanted, "Tester")

    def test_trimmed(self) -> None:
        self.assertEqual(self.clean("  Big   Boid "), "Big Boid")

    def test_cut_to_the_mods_limit(self) -> None:
        self.assertEqual(self.clean("x" * 40), "x" * gourd_name.MAX_LENGTH)

    def test_empty_or_gourd_is_gourd(self) -> None:
        for wanted in ("", "   ", "gourd", "GOURD"):
            self.assertEqual(self.clean(wanted), data.GOURD_ITEM_NAME, wanted)

    def test_any_other_name_is_free(self) -> None:
        for wanted in ("Boid", "PLUMBUS", "Jelly Baby", "Backpack", "Victory"):
            self.assertEqual(self.clean(wanted), wanted)


class TestCharactersAreFiltered(TestCase):
    """What a name may not hold is dropped, and what it may hold is left alone."""

    def clean(self, wanted: str) -> str:
        return gourd_name.clean(wanted, "Tester")

    def test_letters_digits_accents_and_the_signs_are_kept(self) -> None:
        for name in ("Plumbus du Destin", "Gourde éàç", "Prout's", "Prout-2", "Prout, Prout", "Prout!", "R2.D2 (ok)", "南瓜"):
            self.assertEqual(name, self.clean(name))

    def test_tags_braces_and_separators_are_dropped(self) -> None:
        self.assertEqual("bProutb", self.clean("<b>Prout</b>").replace("/", ""))
        for bad in ("<", ">", "{", "}", "|", "#", "%", "\\", "/", '"', "[", "]", "^", "~"):
            self.assertEqual("PROUT", self.clean(f"PR{bad}OUT"))

    def test_control_and_invisible_characters_are_dropped(self) -> None:
        self.assertEqual("ProutProut", self.clean("Prout​Prout"))
        self.assertEqual("ProutProut", self.clean("‮ProutProut"))
        self.assertEqual("Prout Prout", self.clean("Prout\tProut"))
        self.assertEqual("Prout Prout", self.clean("Prout\nProut"))
        self.assertEqual("ProutProut", self.clean("Prout\x00Prout"))

    def test_emoji_are_dropped(self) -> None:
        self.assertEqual("Prout", self.clean("Prout 🌽"))

    def test_nothing_left_is_gourd(self) -> None:
        for name in ("<>", "🌽", "###", "​​", "   "):
            self.assertEqual(data.GOURD_ITEM_NAME, self.clean(name))

    def test_a_name_without_a_letter_is_gourd(self) -> None:
        for name in ("{0}", "123", "42 !", "- 7 -"):
            self.assertEqual(data.GOURD_ITEM_NAME, self.clean(name))

    def test_decorated_letters_become_plain(self) -> None:
        self.assertEqual("Fraktur", self.clean("𝕱𝖗𝖆𝖐𝖙𝖚𝖗"))
        self.assertEqual("abc", self.clean("ⓐⓑⓒ"))
        self.assertEqual("ABC", self.clean("ＡＢＣ"))

    def test_the_cut_comes_after_the_filter(self) -> None:
        wanted = "<<<" + "x" * 30
        self.assertEqual("x" * 24, self.clean(wanted))
