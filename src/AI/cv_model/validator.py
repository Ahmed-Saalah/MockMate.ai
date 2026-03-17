VALID_LEVELS = ["Junior", "Mid-level", "Senior"]

def validate_output(data: dict) -> dict:
    if not isinstance(data, dict):
        raise ValueError("Output is not a dictionary")

    if "track_name" not in data or not data["track_name"]:
        data["track_name"] = "Unknown"

    if data.get("level") not in VALID_LEVELS:
        data["level"] = "Unknown"

    if not isinstance(data.get("technical_skills"), list):
        data["technical_skills"] = []

    # Remove duplicates
    data["technical_skills"] = list(set(data["technical_skills"]))

    return data