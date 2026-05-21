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
        "- defaultCode MUST include all common/standard packages and libraries the candidate might need at the top.\n"
        "- Inside defaultCode, after the imports, it contains ONLY the Solution class — nothing else.\n"
        "- NO helper classes or extra code outside Solution.\n"
        "- The method body must contain ONLY the skeleton comment and return stub\n"
        "- CORRECT C++: \"#include <iostream>\\n#include <vector>\\n#include <string>\\n#include <algorithm>\\n#include <map>\\n#include <set>\\n#include <queue>\\n#include <stack>\\nusing namespace std;\\n\\nclass Solution {\\npublic:\\n    vector<double> method(vector<double>& data) {\\n        // Write your code here\\n        return {};\\n    }\\n};\"\n"
        "- CORRECT Java: \"import java.util.*;\\nimport java.io.*;\\nimport java.util.stream.*;\\n\\npublic class Solution {\\n    public List<Double> method(List<Double> data) {\\n        // Write your code here\\n        return new ArrayList<>();\\n    }\\n}\"\n\n"
        "RULES FOR driverCode (THE HARNESS THAT RUNS THE CANDIDATE CODE):\n"
        "- Layout: imports/headers → {{USER_CODE}} → main/runner block\n"
        "- Write the placeholder EXACTLY as {{USER_CODE}} — two opening braces, USER_CODE, two closing braces\n"
        "- {{USER_CODE}} must appear BEFORE the main/runner block\n"
        "- testCases input is a raw value (e.g. a list, a number) — NOT a JSON object with named keys\n"
        "- The driverCode reads stdin, parses it, calls Solution, and prints the result\n"
        "CORRECT C++ EXAMPLE (input is a list of numbers):\n"
        "  defaultCode: \"#include <iostream>\\n#include <vector>\\n#include <string>\\n#include <algorithm>\\nusing namespace std;\\n\\nclass Solution {\\npublic:\\n    vector<double> min_max_normalize(vector<double>& data) {\\n        // Write your code here\\n        return {};\\n    }\\n};\"\n"
        "  driverCode: \"#include <iostream>\\n#include <vector>\\n#include <string>\\n#include <sstream>\\n{{USER_CODE}}\\nint main() {\\n    string input;\\n    if (getline(cin, input)) {\\n        // parse input string to vector, call Solution, print output\\n    }\\n    return 0;\\n}\"\n"
        "  testCase input: \"[1, 2, 3, 4, 5]\"\n"
        "  testCase output: \"[0.0, 0.25, 0.5, 0.75, 1.0]\"\n\n"
        "CORRECT JAVA EXAMPLE (input is a list of numbers):\n"
        "  defaultCode: \"import java.util.*;\\nimport java.io.*;\\n\\npublic class Solution {\\n    public List<Double> minMaxNormalize(List<Double> data) {\\n        // Write your code here\\n        return new ArrayList<>();\\n    }\\n}\"\n"
        "  driverCode: \"import java.util.*;\\nimport java.io.*;\\n{{USER_CODE}}\\npublic class Program {\\n    public static void Main(string[] args) throws Exception {\\n        BufferedReader br = new BufferedReader(new InputStreamReader(System.in));\\n        String input = br.readLine();\\n        // parse input, call Solution, print output\\n    }\\n}\"\n"
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
        '          "languageId": 54,\n'
        '          "defaultCode": "Common C++ headers (#include) + Solution class skeleton — NO logic",\n'
        '          "driverCode": "headers + {{USER_CODE}} + int main() block that reads raw stdin"\n'
        '        },\n'
        '        {\n'
        '          "languageId": 62,\n'
        '          "defaultCode": "Common Java imports + public class Solution skeleton — NO logic",\n'
        '          "driverCode": "imports + {{USER_CODE}} + public class Program with main method that reads raw stdin"\n'
        '        }\n'
        '      ]\n'
        '    }\n'
        '  ]\n'
        '}\n\n'
        "CHECKLIST:\n"
        "[ ] codingQuestions has exactly 2 items\n"
        "[ ] defaultCode includes all common libraries/packages at the top, followed ONLY by the Solution class skeleton\n"        "[ ] driverCode contains {{USER_CODE}} with DOUBLE braces\n"
        "[ ] {{USER_CODE}} appears BEFORE the main/runner block\n"
        "[ ] testCase input/output are raw values as strings (not JSON objects with named keys)\n"
        "[ ] driverCode reads raw stdin directly (not json.loads of a named-key object)\n"
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
        "- defaultCode: MUST include all common/standard packages and libraries at the top, followed ONLY by the Solution class skeleton — no logic\n"
        "- driverCode: imports/headers → {{USER_CODE}} → main block that reads raw stdin\n"
        "- testCase input/output: raw values as strings (e.g. '[1,2,3]'), NOT JSON objects\n"
        "- {{USER_CODE}} must appear BEFORE the main block — use DOUBLE braces\n"
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
        '          "languageId": 54,\n'
        '          "defaultCode": "#include <iostream>\\n#include <vector>\\n#include <string>\\n#include <algorithm>\\nusing namespace std;\\n\\nclass Solution {\\npublic:\\n    vector<double> method(vector<double>& data) {\\n        // Write your code here\\n        return {};\\n    }\\n};",\n'
        '          "driverCode": "#include <iostream>\\n#include <vector>\\n#include <string>\\n{{USER_CODE}}\\nint main() {\\n    string inputLine = Console.ReadLine();\\n    // parse inputLine → call Solution → print result\\n    return 0;\\n}"\n'
        '        },\n'
        '        {\n'
        '          "languageId": 62,\n'
        '          "defaultCode": "import java.util.*;\\nimport java.io.*;\\n\\npublic class Solution {\\n    public List<Double> method(List<Double> data) {\\n        // Write your code here\\n        return new ArrayList<>();\\n    }\\n}",\n'
        '          "driverCode": "import java.util.*;\\nimport java.io.*;\\n{{USER_CODE}}\\npublic class Program {\\n    public static void main(String[] args) throws Exception {\\n        BufferedReader br = new BufferedReader(new InputStreamReader(System.in));\\n        String inputLine = br.readLine();\\n        // parse inputLine → call Solution → print result\\n    }\\n}"\n'
        '        }\\n'
        '      ]\n'
        '    }\n'
        '  ]\n'
        '}\n\n'
        "FINAL CHECKLIST:\n"
        "[ ] mcqQuestions has exactly 8 items\n"
        "[ ] codingQuestions has exactly 2 items\n"
        "[ ] Each MCQ has exactly 1 isCorrect=true option\n"
        "[ ] defaultCode includes all common libraries/packages at the top, followed ONLY by the Solution class skeleton\n"
        "[ ] driverCode contains {{USER_CODE}} with DOUBLE braces before main block\n""[ ] testCase input/output are raw values as strings\n"
        "[ ] testCase input/output are raw values as strings\n"
        "[ ] All code strings single-line with \\n for newlines\n"
        "[ ] Output is valid JSON — no markdown, no trailing commas\n"
    )
