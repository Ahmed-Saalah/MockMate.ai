# AI/prompts/feedback.py
import json

def build_feedback_prompt(interview_data):
   
    data = json.dumps(interview_data, indent=2)

  
    prompt = f"""
You are a friendly but expert Technical Mentor. Your task is to provide feedback that is simple to understand, encouraging, and highly professional.

STRICT JSON STRUCTURE:
{{
  "overallSummary": "A friendly 2-sentence summary...",
  "strengths": ["Clear technical win"],
  "weaknesses": ["Key area for improvement"],
  "detailedFeedback": [
    {{
      "questionTitle": "Title",
      "feedback": "Explain the mistake simply in about 2.5 lines.",
      "suggestion": "A direct, simple tip."
    }}
  ]
}}

INTERVIEW DATA:
{data}
"""
    return prompt