from pydantic import BaseModel
from typing import List

class MCQOption(BaseModel):
    optionText: str
    isCorrect: bool

class MCQQuestion(BaseModel):
    title: str
    text: str
    options: List[MCQOption]

class TestCase(BaseModel):
    input: str
    output: str
    isHidden: bool

class CodeTemplate(BaseModel):
    languageId: int
    defaultCode: str
    driverCode: str

class CodingQuestion(BaseModel):
    title: str
    text: str
    testCases: List[TestCase]
    templates: List[CodeTemplate]

class InterviewQuestions(BaseModel):
    trackName: str
    seniorityLevel: str
    detectedSkills: List[str]
    mcqQuestions: List[MCQQuestion]
    codingQuestions: List[CodingQuestion]