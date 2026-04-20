def build_questions_prompt(cv_analysis: dict, job_description: str = None) -> str:
    import json

    track = cv_analysis.get("track_name", "Unknown Track")
    level = cv_analysis.get("level", "Mid-level")

    level_map = {"Junior": "Junior", "Mid-level": "MidLevel", "Senior": "Senior"}
    seniority_level = level_map.get(level, level.replace("-", ""))

    skills = cv_analysis.get("technical_skills", [])[:12]
    skills_json = json.dumps(skills, ensure_ascii=False)

    # IMPORTANT: Do NOT use .format() or f-strings with this prompt.
    # The prompt contains {{USER_CODE}}, JSON braces, and user-supplied strings
    # that may themselves contain { or } characters. Use concatenation only.

    prompt = (
        "You are a Senior Technical Interview Generator.\n\n"
        "STRICT RULES:\n"
        "- Output MUST be valid JSON only\n"
        "- Do NOT include markdown\n"
        "- Do NOT include explanation\n"
        "- Do NOT add extra fields\n"
        "- Do NOT remove fields\n"
        "- Follow structure EXACTLY\n\n"
        "CANDIDATE:\n"
        "- Track: " + track + "\n"
        "- Level: " + seniority_level + "\n\n"

        # ── defaultCode rules ─────────────────────────────────────────────────
        "RULES FOR defaultCode (THE SKELETON THE CANDIDATE SEES):\n"
        "- defaultCode contains ONLY the Solution class — nothing else\n"
        "- NO imports, NO helper classes, NO extra code outside Solution\n"
        "- If the problem needs a helper class (e.g. Product, Node, TreeNode), "
        "define it ONLY in driverCode, NOT in defaultCode\n"
        "- The method body must contain ONLY the skeleton comment and return stub — zero logic\n"
        "- Do NOT include any implementation or algorithm in defaultCode\n"
        "- CORRECT Python defaultCode:\n"
        '  "class Solution:\\n    def method_name(self, param: type) -> type:\\n        # Write your code here\\n        pass"\n'
        "- CORRECT C# defaultCode:\n"
        '  "public class Solution {\\n    public ReturnType MethodName(ParamType param) {\\n        // Write your code here\\n        return default;\\n    }\\n}"\n'
        "- WRONG C# defaultCode — do NOT put helper classes here:\n"
        '  "public class Product { ... }\\npublic class Solution { ... }"  ← FORBIDDEN\n\n'

        # ── driverCode rules ──────────────────────────────────────────────────
        "RULES FOR driverCode (THE HARNESS THAT RUNS THE CANDIDATE CODE):\n"
        "- driverCode layout: imports → optional helper classes → {{USER_CODE}} → main/runner block\n"
        "- If the problem needs helper classes (Product, Node, etc.), define them in driverCode BEFORE {{USER_CODE}}\n"
        "- Write the placeholder EXACTLY as {{USER_CODE}} — two opening braces, USER_CODE, two closing braces\n"
        "- Place {{USER_CODE}} BEFORE the Program/main block so Solution is defined before it is called\n"
        "\n"
        "PYTHON ENTRY POINT — copy this exactly, underscores are DOUBLE on every side:\n"
        '  if __name__ == "__main__":\n'
        "  (that is: underscore underscore name underscore underscore)\n"
        "  WRONG: if _name_ == \"_main_\":  ← single underscores — this will NEVER execute\n"
        "\n"
        "CORRECT Python driverCode pattern:\n"
        '  "import json\\nimport sys\\n{{USER_CODE}}\\nif __name__ == \\"__main__\\":\\n    input_data = json.loads(sys.stdin.read().strip())\\n    result = Solution().method_name(input_data)\\n    print(json.dumps(result))"\n'
        "\n"
        "CORRECT C# driverCode pattern:\n"
        '  "using System;\\nusing System.Collections.Generic;\\nusing System.Linq;\\n{{USER_CODE}}\\npublic class Program {\\n    public static void Main(string[] args) {\\n        string line = Console.ReadLine();\\n        // parse → call new Solution().Method() → print\\n    }\\n}"\n\n'

        "YOU MUST RETURN THIS EXACT JSON STRUCTURE:\n\n"
        "{\n"
        '  "trackName": "' + track + '",\n'
        '  "seniorityLevel": "' + seniority_level + '",\n'
        '  "detectedSkills": ' + skills_json + ',\n'
        '  "mcqQuestions": [\n'
        '    {\n'
        '      "title": "string",\n'
        '      "text": "string",\n'
        '      "options": [\n'
        '        { "optionText": "string", "isCorrect": false },\n'
        '        { "optionText": "string", "isCorrect": false },\n'
        '        { "optionText": "string", "isCorrect": true },\n'
        '        { "optionText": "string", "isCorrect": false }\n'
        '      ]\n'
        '    }\n'
        '  ],\n'
        '  "codingQuestions": [\n'
        '    {\n'
        '      "title": "string",\n'
        '      "text": "string (problem statement with Example and Constraints sections)",\n'
        '      "testCases": [\n'
        '        { "input": "string", "output": "string", "isHidden": false },\n'
        '        { "input": "string", "output": "string", "isHidden": true }\n'
        '      ],\n'
        '      "templates": [\n'
        '        {\n'
        '          "languageId": 51,\n'
        '          "defaultCode": "Solution class skeleton only — single line with \\\\n — NO helper classes — NO implementation",\n'
        '          "driverCode": "imports + optional helpers + {{USER_CODE}} + Program.Main — single line with \\\\n"\n'
        '        },\n'
        '        {\n'
        '          "languageId": 71,\n'
        '          "defaultCode": "Solution class skeleton only — single line with \\\\n — NO imports — NO implementation",\n'
        '          "driverCode": "imports + {{USER_CODE}} + if __name__ == \\"__main__\\": block — single line with \\\\n"\n'
        '        }\n'
        '      ]\n'
        '    }\n'
        '  ]\n'
        '}\n\n'

        "FINAL CHECKLIST — verify every item before outputting:\n"
        "[ ] mcqQuestions has exactly 8 items\n"
        "[ ] codingQuestions has exactly 2 items\n"
        "[ ] defaultCode for EVERY template is a skeleton with ONLY Solution class — no helper classes, no logic\n"
        "[ ] driverCode for EVERY template contains {{USER_CODE}} with DOUBLE braces\n"
        "[ ] {{USER_CODE}} appears BEFORE the main/runner block in every driverCode\n"
        "[ ] Python driverCode uses if __name__ == \"__main__\": with DOUBLE underscores — NOT _name_ or _main_\n"
        "[ ] Helper classes (if any) are defined in driverCode only, NEVER in defaultCode\n"
        "[ ] All code strings are single-line — newlines encoded as \\n, no real line breaks inside JSON strings\n"
        "[ ] Output is valid JSON — no markdown fences, no trailing commas\n"
    )

    return prompt
