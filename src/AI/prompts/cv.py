def build_prompt(cv_text: str, job_description: str) -> str:
    allowed_tracks = [
        "Backend Development",
        "Frontend Development",
        "Full-Stack Development",
        "Mobile Development",
        "AI & Machine Learning Engineering",
        "General Software Engineer",
    ]
    allowed_tracks_str = ", ".join(f'"{t}"' for t in allowed_tracks)

    cv_empty = not cv_text or not cv_text.strip()
    jd_empty = not job_description or not job_description.strip() or job_description.strip().lower() in ("no", "none", "-", "n/a")

    if cv_empty and not jd_empty:
        scenario = "SCENARIO 2: CV is empty, JD is provided.\n- Determine track, level, and skills ENTIRELY from the JD.\n- Ignore CV completely."
    elif not cv_empty and not jd_empty:
        scenario = "SCENARIO 3: CV has skills, JD is provided.\n- Determine track and level from the JD (JD takes priority).\n- Skills = CV skills that are relevant to the JD role + any important skills from the JD not already in CV.\n- DISCARD CV skills unrelated to the JD role."
    elif not cv_empty and jd_empty:
        scenario = "SCENARIO 1: CV has skills, JD is missing or unclear.\n- Determine track, level, and skills ENTIRELY from the CV."
    else:
        scenario = "SCENARIO 4: CV is empty and JD is missing or unclear.\n- Set track_name to 'General Software Engineer'.\n- Set level to 'Junior'.\n- Set technical_skills to core software engineering skills: Python, Git, SQL, REST APIs, Docker, Data Structures, Algorithms, Linux."

    return f"""You are a senior technical recruiter and expert AI resume analyst.

Analyze the inputs and return ONLY valid JSON — no markdown, no explanation.

OUTPUT FORMAT:
{{
  "track_name": "Exactly one of: [{allowed_tracks_str}]",
  "level": "Junior | Mid-level | Senior",
  "technical_skills": ["Skill1", "Skill2", ...]
}}

ACTIVE SCENARIO:
{scenario}

TRACK MAPPING (use when determining track):
- .NET / Java / Node.js / Go / Python backend → "Backend Development"
- React / Angular / Vue / JavaScript / CSS / HTML / Frontend Developer → "Frontend Development"
- Both frontend + backend → "Full-Stack Development"
- Flutter / iOS / Android / React Native → "Mobile Development"
- ML / AI / Data Science / LLMs / Computer Vision / NLP / Data Scientist → "AI & Machine Learning Engineering"
- Cannot be determined → "General Software Engineer"
- Must be character-for-character exact match.

LEVEL MAPPING (use when determining level):
- Junior: student, fresh grad, < 1 year real experience
- Mid-level: 1–3 years professional experience
- Senior: 4+ years, or JD says "Senior" / "Lead" / "4+ years"

SKILLS QUALITY RULES:
- Official names only: "React" not "ReactJS", "Node.js" not "NodeJS"
- No soft skills (no "Communication", "Teamwork")
- No duplicates, sorted by relevance
- Max 15 skills

CV TEXT:
{cv_text if not cv_empty else "(empty)"}

JOB DESCRIPTION:
{job_description if not jd_empty else "(not provided)"}

Return JSON only.
"""
