"""Cleans the gourds' on-screen name, which only the mod shows."""

from __future__ import annotations

import logging
import unicodedata

from . import data

MAX_LENGTH = 24  # Same as GourdNames.MaxLength in the mod.


# Punctuation kept besides letters, marks, digits and spaces; no `<`, the hub board is TextMeshPro.
ALLOWED_PUNCTUATION = frozenset("'-_.,!?&+:;()@")


def keep(character: str) -> bool:
    return (character == " " or character in ALLOWED_PUNCTUATION
            or unicodedata.category(character)[0] in ("L", "M", "N"))


def filtered(wanted: str) -> str:
    """NFKC first, so decorated letters (Ⓐⓑⓒ, full-width) become plain ones the fonts can draw."""
    wanted = unicodedata.normalize("NFKC", wanted)
    return "".join(character for character in " ".join(wanted.split()) if keep(character))


def clean(wanted: str, player_name: str) -> str:
    """Trims and cuts the name; "Gourd" if nothing with a letter is left."""
    name = filtered(wanted)
    if name != " ".join(wanted.split()):
        logging.warning(f"Big Walk ({player_name}): gourd_name lost the characters it may not hold.")
    name = " ".join(name.split())
    if len(name) > MAX_LENGTH:
        logging.warning(f"Big Walk ({player_name}): gourd_name cut to {MAX_LENGTH} characters.")
        name = name[:MAX_LENGTH].rstrip()
    if not any(unicodedata.category(character)[0] in ("L", "M") for character in name):
        # No letter: empty, or only digits and signs, like `{0}` once the braces are gone.
        if name:
            logging.warning(f"Big Walk ({player_name}): gourd_name has no letter, keeping Gourd.")
        return data.GOURD_ITEM_NAME
    if name.casefold() == data.GOURD_ITEM_NAME.casefold():
        return data.GOURD_ITEM_NAME
    return name
