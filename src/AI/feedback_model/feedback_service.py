from feedback_prompt import build_feedback_prompt
from llm_utils import analyze_content


def generate_feedback(interview_data: dict) -> dict:
    prompt = build_feedback_prompt(interview_data)

    result = analyze_content(prompt)

    return result