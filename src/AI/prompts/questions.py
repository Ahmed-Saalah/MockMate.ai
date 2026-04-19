def build_questions_prompt(cv_analysis: dict, job_description: str = None) -> str:
    import json

    track = cv_analysis.get("track_name", "Unknown Track")
    level = cv_analysis.get("level", "Mid-level")

    level_map = {"Junior": "Junior", "Mid-level": "MidLevel", "Senior": "Senior"}
    seniority_level = level_map.get(level, level.replace("-", ""))

    skills = cv_analysis.get("technical_skills", [])[:12]
    skills_json = json.dumps(skills)

    return """
You are a Senior Technical Interview Generator.

STRICT RULES:
- Output MUST be valid JSON only
- Do NOT include markdown
- Do NOT include explanation
- Do NOT add extra fields
- Do NOT remove fields
- Follow structure EXACTLY

CANDIDATE:
- Track: {track}
- Level: {seniority_level}

YOU MUST RETURN THIS EXACT JSON STRUCTURE:

{{
  "trackName": "{track}",
  "seniorityLevel": "{seniority_level}",
  "detectedSkills": {skills_json},
  "mcqQuestions": [
    {{
      "title": "string",
      "text": "string",
      "options": [
        {{ "optionText": "string", "isCorrect": false }},
        {{ "optionText": "string", "isCorrect": false }},
        {{ "optionText": "string", "isCorrect": true }},
        {{ "optionText": "string", "isCorrect": false }}
      ]
    }}
  ],
  "codingQuestions": [
    {{
      "title": "string",
      "text": "string (must include example + constraints)",
      "testCases": [
        {{
          "input": "string",
          "output": "string",
          "isHidden": false
        }},
        {{
          "input": "string",
          "output": "string",
          "isHidden": true
        }}
      ],
      "templates": [
        {{
          "languageId": 51,
          "defaultCode": "C# code as SINGLE LINE with \\n",
          "driverCode": "C# driver code as SINGLE LINE with \\n and {{USER_CODE}} placeholder"
        }},
        {{
          "languageId": 71,
          "defaultCode": "Python code as SINGLE LINE with \\n",
          "driverCode": "Python driver code as SINGLE LINE with \\n and {{USER_CODE}} placeholder"
        }}
      ]
    }}
  ]
}}


CRITICAL RULES:
- All strings must be valid JSON strings
- Escape all quotes properly
- All code must be single-line strings using \\n
- MUST NOT break JSON under any condition
- Return ONLY JSON
- mcqQuestions must contain exactly 8 questions
- codingQuestions must contain exactly 2 questions
- Each codingQuestion must have title, text, testCases array, and templates array
- You MUST always include "{{USER_CODE}}" inside driverCode
- NEVER remove or rename {{USER_CODE}}
- IMPORTANT: In driverCode, use {{USER_CODE}} exactly with double curly braces, not single

""".format(
    track=track,
    seniority_level=seniority_level,
    skills_str=", ".join(skills),
    skills_json=skills_json
)