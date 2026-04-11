def validate_feedback_structure(data: dict):
    required_keys = [
        "overallSummary",
        "strengths",
        "weaknesses",
        "detailedFeedback"
    ]

    for key in required_keys:
        if key not in data:
            raise ValueError(f"Missing key: {key}")

    if not isinstance(data["strengths"], list) or not data["strengths"]:
        raise ValueError("strengths must be a non-empty list")

    if not isinstance(data["weaknesses"], list) or not data["weaknesses"]:
        raise ValueError("weaknesses must be a non-empty list")

    if not isinstance(data["detailedFeedback"], list):
        raise ValueError("detailedFeedback must be a list")

    for item in data["detailedFeedback"]:
        if not all(k in item for k in ["questionTitle", "feedback", "suggestion"]):
            raise ValueError("Invalid detailedFeedback item structure")

    return True
