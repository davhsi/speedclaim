import unicodedata


class QuestionValidationError(ValueError):
    pass


def normalize_question(value: str, *, max_characters: int) -> str:
    normalized = unicodedata.normalize("NFC", value)
    for character in normalized:
        category = unicodedata.category(character)
        if category.startswith("C") and character not in {"\t", "\n", "\r"}:
            raise QuestionValidationError("question contains unsupported control characters")
    normalized = " ".join(normalized.split())
    if not normalized:
        raise QuestionValidationError("question must not be empty")
    if len(normalized) > max_characters:
        raise QuestionValidationError(
            f"question must not exceed {max_characters} characters"
        )
    return normalized
