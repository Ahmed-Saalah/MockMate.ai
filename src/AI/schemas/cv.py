from pydantic import BaseModel
from typing import List

class ResumeAnalysis(BaseModel):
    track_name: str
    level: str
    technical_skills: List[str]


VALID_TRACKS = [
    "Backend Development",
    "Frontend Development",
    "Full-Stack Development",
    "Mobile Development",
    "AI & Machine Learning Engineering",
    "General Software Engineer",
]
VALID_LEVELS = ["Junior", "Mid-level", "Senior"]

def validate_cv_output(data: dict) -> dict:
    if not isinstance(data, dict):
        raise ValueError("Output is not a dictionary")

    if data.get("track_name") not in VALID_TRACKS:
        data["track_name"] = "General Software Engineer"

    if data.get("level") not in VALID_LEVELS:
        data["level"] = "Mid-level"

    if not isinstance(data.get("technical_skills"), list):
        data["technical_skills"] = []

    # Remove duplicates while preserving order
    data["technical_skills"] = list(dict.fromkeys(data["technical_skills"]))

    return data
