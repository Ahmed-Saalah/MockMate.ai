import json


def _base_header(track: str, seniority_level: str) -> str:
    return (
        "You are a Senior Technical Interview Generator.\n\n"
        "STRICT RULES:\n"
        "- Output MUST be valid JSON only\n"
        "- Do NOT include markdown, explanation, or extra fields\n"
        "- Follow the structure EXACTLY\n\n"
        "CANDIDATE:\n"
        "- Track: " + track + "\n"
        "- Level: " + seniority_level + "\n\n"
    )


def build_mcq_prompt(cv_analysis: dict, job_description: str = None) -> str:
    track = cv_analysis.get("track_name", "Unknown Track")
    level = cv_analysis.get("level", "Mid-level")
    level_map = {"Junior": "Junior", "Mid-level": "MidLevel", "Senior": "Senior"}
    seniority_level = level_map.get(level, level.replace("-", ""))
    skills = cv_analysis.get("technical_skills", [])[:12]
    skills_json = json.dumps(skills, ensure_ascii=False)

    jd_line = ("JOB DESCRIPTION:\n" + job_description + "\n\n") if job_description else ""

    return (
        _base_header(track, seniority_level)
        + jd_line
        + "Generate exactly 8 MCQ questions relevant to the candidate's track and skills.\n"
        "Each MCQ must have between 3 and 5 options and exactly 1 correct answer.\n\n"
        "RETURN THIS EXACT JSON:\n"
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
        '        { "optionText": "string", "isCorrect": true },\n'
        '        { "optionText": "string", "isCorrect": false }\n'
        '      ]\n'
        '    }\n'
        '  ]\n'
        '}\n\n'
        "CHECKLIST:\n"
        "[ ] mcqQuestions has exactly 8 items\n"
        "[ ] Each MCQ has exactly 1 option with isCorrect=true\n"
        "[ ] Output is valid JSON — no markdown, no trailing commas\n"
    )


def build_coding_prompt(cv_analysis: dict, job_description: str = None) -> str:
    track = cv_analysis.get("track_name", "Unknown Track")
    level = cv_analysis.get("level", "Mid-level")
    level_map = {"Junior": "Junior", "Mid-level": "MidLevel", "Senior": "Senior"}
    seniority_level = level_map.get(level, level.replace("-", ""))

    jd_line = ("JOB DESCRIPTION:\n" + job_description + "\n\n") if job_description else ""

    return (
        _base_header(track, seniority_level)
        + jd_line
        + "RULES FOR defaultCode (THE SKELETON THE CANDIDATE SEES):\n"
        "- defaultCode contains ONLY the Solution class — nothing else\n"
        "- NO imports, NO helper classes, NO extra code outside Solution\n"
        "- The method body must contain ONLY the skeleton comment and return stub\n"
        "- CORRECT Python: \"class Solution:\\n    def method(self, data: list) -> list:\\n        # Write your code here\\n        pass\"\n"
        "- CORRECT C#: \"public class Solution {\\n    public List<double> Method(List<double> data) {\\n        // Write your code here\\n        return new List<double>();\\n    }\\n}\"\n\n"
        "RULES FOR driverCode (THE HARNESS THAT RUNS THE CANDIDATE CODE):\n"
        "- Layout: imports → {{USER_CODE}} → main/runner block\n"
        "- Write the placeholder EXACTLY as {{USER_CODE}} — two opening braces, USER_CODE, two closing braces\n"
        "- {{USER_CODE}} must appear BEFORE the main/runner block\n"
        "- testCases input is a raw value (e.g. a list, a number) — NOT a JSON object with named keys\n"
        "- The driverCode reads stdin, parses it, calls Solution, and prints the result\n"
        "- PYTHON ENTRY POINT must use DOUBLE underscores: if __name__ == \"__main__\":\n\n"
        "CORRECT PYTHON EXAMPLE (input is a list of numbers):\n"
        "  defaultCode: \"class Solution:\\n    def min_max_normalize(self, data: list[float]) -> list[float]:\\n        # Write your code here\\n        pass\"\n"
        "  driverCode: \"import json\\nimport sys\\n{{USER_CODE}}\\nif __name__ == \\\"__main__\\\":\\n    input_data = json.loads(sys.stdin.read().strip())\\n    result = Solution().min_max_normalize(input_data)\\n    print(json.dumps(result))\"\n"
        "  testCase input: \"[1, 2, 3, 4, 5]\"\n"
        "  testCase output: \"[0.0, 0.25, 0.5, 0.75, 1.0]\"\n\n"
        "CORRECT C# EXAMPLE (input is a list of numbers):\n"
        "  defaultCode: \"public class Solution {\\n    public List<double> MinMaxNormalize(List<double> data) {\\n        // Write your code here\\n        return new List<double>();\\n    }\\n}\"\n"
        "  driverCode: \"using System;\\nusing System.Collections.Generic;\\nusing System.Linq;\\n{{USER_CODE}}\\npublic class Program {\\n    public static void Main(string[] args) {\\n        string inputLine = Console.ReadLine();\\n        List<double> data = inputLine.Trim('[', ']').Split(',').Select(double.Parse).ToList();\\n        List<double> result = new Solution().MinMaxNormalize(data);\\n        Console.WriteLine(\\\"[\\\" + string.Join(\\\", \\\", result) + \\\"]\\\");\\n    }\\n}\"\n"
        "  testCase input: \"[1, 2, 3, 4, 5]\"\n"
        "  testCase output: \"[0.0, 0.25, 0.5, 0.75, 1.0]\"\n\n"
        "Generate exactly 2 coding questions relevant to the candidate's track and level.\n\n"
        "RETURN THIS EXACT JSON:\n"
        "{\n"
        '  "codingQuestions": [\n'
        '    {\n'
        '      "title": "string",\n'
        '      "text": "string — problem statement with Example and Constraints",\n'
        '      "testCases": [\n'
        '        { "input": "raw value as string e.g. [1,2,3]", "output": "raw value as string", "isHidden": false },\n'
        '        { "input": "raw value as string", "output": "raw value as string", "isHidden": true }\n'
        '      ],\n'
        '      "templates": [\n'
        '        {\n'
        '          "languageId": 51,\n'
        '          "defaultCode": "Solution class skeleton — NO imports — NO logic",\n'
        '          "driverCode": "imports + {{USER_CODE}} + Program.Main that reads raw stdin"\n'
        '        },\n'
        '        {\n'
        '          "languageId": 71,\n'
        '          "defaultCode": "Solution class skeleton — NO imports — NO logic",\n'
        '          "driverCode": "import json\\nimport sys\\n{{USER_CODE}}\\nif __name__ == \\"__main__\\": block that reads raw stdin"\n'
        '        }\n'
        '      ]\n'
        '    }\n'
        '  ]\n'
        '}\n\n'
        "CHECKLIST:\n"
        "[ ] codingQuestions has exactly 2 items\n"
        "[ ] defaultCode is skeleton only — ONLY Solution class, no imports, no logic\n"
        "[ ] driverCode contains {{USER_CODE}} with DOUBLE braces\n"
        "[ ] {{USER_CODE}} appears BEFORE the main/runner block\n"
        "[ ] testCase input/output are raw values as strings (not JSON objects with named keys)\n"
        "[ ] driverCode reads raw stdin directly (not json.loads of a named-key object)\n"
        "[ ] Python driverCode uses if __name__ == \"__main__\": with DOUBLE underscores\n"
        "[ ] All code strings are single-line — newlines encoded as \\n\n"
        "[ ] Output is valid JSON — no markdown, no trailing commas\n"
    )


def build_questions_prompt(cv_analysis: dict, job_description: str = None) -> str:
    """Legacy single-prompt builder — kept for the questions-only endpoint."""
    track = cv_analysis.get("track_name", "Unknown Track")
    level = cv_analysis.get("level", "Mid-level")
    level_map = {"Junior": "Junior", "Mid-level": "MidLevel", "Senior": "Senior"}
    seniority_level = level_map.get(level, level.replace("-", ""))
    skills = cv_analysis.get("technical_skills", [])[:12]
    skills_json = json.dumps(skills, ensure_ascii=False)

    jd_line = ("JOB DESCRIPTION:\n" + job_description + "\n\n") if job_description else ""

    return (
        _base_header(track, seniority_level)
        + jd_line
        + "Generate exactly 8 MCQ questions and exactly 2 coding questions.\n\n"
        "MCQ RULES:\n"
        "- Each MCQ has between 3 and 5 options and exactly 1 correct answer\n\n"
        "CODING RULES:\n"
        "- defaultCode: ONLY the Solution class skeleton — no imports, no logic\n"
        "- driverCode: imports → {{USER_CODE}} → main block that reads raw stdin\n"
        "- testCase input/output: raw values as strings (e.g. '[1,2,3]'), NOT JSON objects\n"
        "- {{USER_CODE}} must appear BEFORE the main block — use DOUBLE braces\n"
        "- Python entry point: if __name__ == \"__main__\": with DOUBLE underscores\n\n"
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
        '        { "optionText": "string", "isCorrect": true },\n'
        '        { "optionText": "string", "isCorrect": false }\n'
        '      ]\n'
        '    }\n'
        '  ],\n'
        '  "codingQuestions": [\n'
        '    {\n'
        '      "title": "string",\n'
        '      "text": "string",\n'
        '      "testCases": [\n'
        '        { "input": "[1, 2, 3]", "output": "[0.0, 0.5, 1.0]", "isHidden": false },\n'
        '        { "input": "[5, 5, 5]", "output": "[0.5, 0.5, 0.5]", "isHidden": true }\n'
        '      ],\n'
        '      "templates": [\n'
        '        {\n'
        '          "languageId": 51,\n'
        '          "defaultCode": "public class Solution {\\n    public List<double> Method(List<double> data) {\\n        // Write your code here\\n        return new List<double>();\\n    }\\n}",\n'
        '          "driverCode": "using System;\\nusing System.Collections.Generic;\\nusing System.Linq;\\n{{USER_CODE}}\\npublic class Program {\\n    public static void Main(string[] args) {\\n        string inputLine = Console.ReadLine();\\n        // parse inputLine → call Solution → print result\\n    }\\n}"\n'
        '        },\n'
        '        {\n'
        '          "languageId": 71,\n'
        '          "defaultCode": "class Solution:\\n    def method(self, data: list) -> list:\\n        # Write your code here\\n        pass",\n'
        '          "driverCode": "import json\\nimport sys\\n{{USER_CODE}}\\nif __name__ == \\"__main__\\":\\n    input_data = json.loads(sys.stdin.read().strip())\\n    result = Solution().method(input_data)\\n    print(json.dumps(result))"\n'
        '        }\n'
        '      ]\n'
        '    }\n'
        '  ]\n'
        '}\n\n'
        "FINAL CHECKLIST:\n"
        "[ ] mcqQuestions has exactly 8 items\n"
        "[ ] codingQuestions has exactly 2 items\n"
        "[ ] Each MCQ has exactly 1 isCorrect=true option\n"
        "[ ] defaultCode is skeleton only — ONLY Solution class\n"
        "[ ] driverCode contains {{USER_CODE}} with DOUBLE braces before main block\n"
        "[ ] testCase input/output are raw values as strings\n"
        "[ ] Python uses if __name__ == \"__main__\": with DOUBLE underscores\n"
        "[ ] All code strings single-line with \\n for newlines\n"
        "[ ] Output is valid JSON — no markdown, no trailing commas\n"
    )
