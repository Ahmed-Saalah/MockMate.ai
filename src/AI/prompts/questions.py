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
        "- Layout: {{USER_CODE}} → main/runner block\n"
        "- Write the placeholder EXACTLY as {{USER_CODE}} — two opening braces, USER_CODE, two closing braces\n"
        "- {{USER_CODE}} must appear BEFORE the main/runner block\n"
        "- testCases input MUST be raw, space-separated values (e.g. '1 2 3 4 5') — NEVER use brackets, commas, or JSON arrays.\n"
        "- The driverCode reads standard space-separated stdin directly (using cin or Scanner), parses it, calls Solution, and prints space-separated output.\n"
        "- DO NOT import regex or use complex string splitting in the driverCode.\n\n"
        "CORRECT C++ EXAMPLE (input is a list of space-separated numbers):\n"
        "  defaultCode: \"#include <iostream>\\n#include <vector>\\n#include <string>\\n#include <algorithm>\\nusing namespace std;\\n\\nclass Solution {\\npublic:\\n    vector<double> min_max_normalize(vector<double>& data) {\\n        // Write your code here\\n        return {};\\n    }\\n};\"\n"
        "  driverCode: \"{{USER_CODE}}\\nint main() {\\n    vector<double> data;\\n    double val;\\n    while (cin >> val) data.push_back(val);\\n    vector<double> result = Solution().min_max_normalize(data);\\n    for (size_t i = 0; i < result.size(); ++i) cout << result[i] << (i == result.size() - 1 ? \\\"\\\" : \\\" \\\");\\n    cout << endl;\\n    return 0;\\n}\"\n"
        "  testCase input: \"1 2 3 4 5\"\n"
        "  testCase output: \"0.0 0.25 0.5 0.75 1.0\"\n\n"
        "CORRECT JAVA EXAMPLE (input is a list of space-separated numbers):\n"
        "  defaultCode: \"import java.util.*;\\nimport java.io.*;\\n\\npublic class Solution {\\n    public List<Double> minMaxNormalize(List<Double> data) {\\n        // Write your code here\\n        return new ArrayList<>();\\n    }\\n}\"\n"
        "  driverCode: \"{{USER_CODE}}\\npublic class Main {\\n    public static void main(String[] args) {\\n        Scanner sc = new Scanner(System.in);\\n        List<Double> data = new ArrayList<>();\\n        while (sc.hasNextDouble()) data.add(sc.nextDouble());\\n        List<Double> result = new Solution().minMaxNormalize(data);\\n        for (int i = 0; i < result.size(); i++) System.out.print(result.get(i) + (i == result.size() - 1 ? \\\"\\\" : \\\" \\\"));\\n        System.out.println();\\n    }\\n}\"\n"
        "  testCase input: \"1 2 3 4 5\"\n"
        "  testCase output: \"0.0 0.25 0.5 0.75 1.0\"\n\n"
        "Generate exactly 2 coding questions relevant to the candidate's track and level.\n\n"
        "RETURN THIS EXACT JSON:\n"
        "{\n"
        '  "codingQuestions": [\n'
        '    {\n'
        '      "title": "string",\n'
        '      "text": "string — problem statement with Example and Constraints",\n'
        '      "testCases": [\n'
        '        { "input": "space-separated string e.g. 1 2 3", "output": "space-separated string", "isHidden": false },\n'
        '        { "input": "space-separated string", "output": "space-separated string", "isHidden": true }\n'
        '      ],\n'
        '      "templates": [\n'
        '        {\n'
        '          "languageId": 54,\n'
        '          "defaultCode": "#include <iostream>\\n#include <vector>\\nusing namespace std;\\n\\nclass Solution {\\npublic:\\n    vector<double> Method(vector<double>& data) {\\n        // Write your code here\\n        return {};\\n    }\\n};",\n'
        '          "driverCode": "{{USER_CODE}}\\nint main() {\\n    vector<double> data; double val; while(cin >> val) data.push_back(val);\\n    vector<double> result = Solution().Method(data);\\n    for(size_t i=0; i<result.size(); ++i) cout << result[i] << (i==result.size()-1 ? \\\"\\\" : \\\" \\\"); cout << endl; return 0;\\n}"\n'
        '        },\n'
        '        {\n'
        '          "languageId": 62,\n'
        '          "defaultCode": "import java.util.*;\\nimport java.io.*;\\n\\npublic class Solution {\\n    public List<Double> method(List<Double> data) {\\n        // Write your code here\\n        return new ArrayList<>();\\n    }\\n}",\n'
        '          "driverCode": "{{USER_CODE}}\\npublic class Main {\\n    public static void main(String[] args) {\\n        Scanner sc = new Scanner(System.in); List<Double> data = new ArrayList<>();\\n        while(sc.hasNextDouble()) data.add(sc.nextDouble());\\n        List<Double> result = new Solution().method(data);\\n        for(int i=0; i<result.size(); i++) System.out.print(result.get(i) + (i==result.size()-1 ? \\\"\\\" : \\\" \\\")); System.out.println();\\n    }\\n}"\n'
        '        }\n'
        '      ]\n'
        '    }\n'
        '  ]\n'
        '}\n\n'
        "CHECKLIST:\n"
        "[ ] codingQuestions has exactly 2 items\n"
        "[ ] defaultCode includes all common libraries/packages at the top, followed ONLY by the Solution class skeleton\n"
        "[ ] driverCode contains {{USER_CODE}} with DOUBLE braces BEFORE the main block\n"
        "[ ] testCase input/output are space-separated strings ONLY (NO brackets, NO commas)\n"
        "[ ] driverCode reads raw space-separated stdin directly using cin or Scanner\n"
        "[ ] NO regex or complex string parsing used in the driverCode\n"
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
        "- driverCode: {{USER_CODE}} → main block that reads raw space-separated stdin\n"
        "- testCase input/output: space-separated strings ONLY (e.g. '1 2 3'), NEVER use brackets or JSON arrays.\n"
        "- {{USER_CODE}} must appear BEFORE the main block — use DOUBLE braces\n"
        "- C++ uses cin, Java uses Scanner. NO regex logic.\n\n"
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
        '        { "input": "1 2 3", "output": "0.0 0.5 1.0", "isHidden": false },\n'
        '        { "input": "5 5 5", "output": "0.5 0.5 0.5", "isHidden": true }\n'
        '      ],\n'
        '      "templates": [\n'
        '        {\n'
        '          "languageId": 54,\n'
        '          "defaultCode": "#include <iostream>\\n#include <vector>\\n#include <string>\\n#include <algorithm>\\nusing namespace std;\\n\\nclass Solution {\\npublic:\\n    vector<double> method(vector<double>& data) {\\n        // Write your code here\\n        return {};\\n    }\\n};",\n'
        '          "driverCode": "{{USER_CODE}}\\nint main() {\\n    vector<double> data; double val; while(cin >> val) data.push_back(val);\\n    vector<double> result = Solution().method(data);\\n    for(size_t i=0; i<result.size(); ++i) cout << result[i] << (i==result.size()-1 ? \\\"\\\" : \\\" \\\"); cout << endl; return 0;\\n}"\n'
        '        },\n'
        '        {\n'
        '          "languageId": 62,\n'
        '          "defaultCode": "import java.util.*;\\nimport java.io.*;\\n\\npublic class Solution {\\n    public List<Double> method(List<Double> data) {\\n        // Write your code here\\n        return new ArrayList<>();\\n    }\\n}",\n'
        '          "driverCode": "{{USER_CODE}}\\npublic class Main {\\n    public static void main(String[] args) {\\n        Scanner sc = new Scanner(System.in); List<Double> data = new ArrayList<>();\\n        while(sc.hasNextDouble()) data.add(sc.nextDouble());\\n        List<Double> result = new Solution().method(data);\\n        for(int i=0; i<result.size(); i++) System.out.print(result.get(i) + (i==result.size()-1 ? \\\"\\\" : \\\" \\\")); System.out.println();\\n    }\\n}"\n'
        '        }\n'
        '      ]\n'
        '    }\n'
        '  ]\n'
        '}\n\n'
        "FINAL CHECKLIST:\n"
        "[ ] mcqQuestions has exactly 8 items\n"
        "[ ] codingQuestions has exactly 2 items\n"
        "[ ] Each MCQ has exactly 1 isCorrect=true option\n"
        "[ ] defaultCode includes all common libraries/packages at the top, followed ONLY by the Solution class skeleton\n"
        "[ ] driverCode contains {{USER_CODE}} with DOUBLE braces before main block\n"
        "[ ] testCase input/output are space-separated strings ONLY (NO brackets, NO commas)\n"
        "[ ] driverCode reads raw space-separated stdin directly using cin or Scanner\n"
        "[ ] All code strings single-line with \\n for newlines\n"
        "[ ] Output is valid JSON — no markdown, no trailing commas\n"
    )