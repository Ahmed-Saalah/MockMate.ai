<div align="center">

# MockMate.ai

### *AI-Powered Technical Interview Practice Platform*

*Analyze your CV. Master your skills. Ace your interview.*

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Python](https://img.shields.io/badge/Python-3.12-3776AB?logo=python&logoColor=white)](https://www.python.org/)
[![FastAPI](https://img.shields.io/badge/FastAPI-0.133-009688?logo=fastapi&logoColor=white)](https://fastapi.tiangolo.com/)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED)

</div>

---

## Work in Progress

> **This project is currently under active development.**
> Features, APIs, and documentation are subject to change at any time. See the [Roadmap](#roadmap) section for the full plan and current progress.

---

## Overview

**MockMate.ai** is a full-stack, AI-powered platform designed to help software engineers prepare for technical interviews. It analyzes a candidate's uploaded CV and/or job description to intelligently extract required technical skills, then generates a timed, personalized interview session consisting of:

- **Multiple-Choice Questions (MCQs)** — assessing theoretical and conceptual knowledge
- **Coding Challenges** — live code execution with automated test-case evaluation via Judge0

At the end of each session, MockMate.ai automatically scores performance and maintains a full interview history so candidates can track improvement over time.

---

## Features

| Feature | Description |
|---|---|
| **CV and JD Analysis** | Upload a PDF resume and/or paste a job description; NLP extracts your track, seniority level, and technical skills automatically |
| **Personalized Sessions** | Interview questions are matched to your specific skill set, track, and experience level |
| **MCQ Assessment** | Timed multiple-choice questions with instant automated grading |
| **Live Code Execution** | Write and run code against real test cases, powered by the Judge0 sandbox |
| **Auto-Scoring** | Sessions are evaluated and scored automatically upon submission |
| **Interview History** | Every session is persisted, giving candidates a full performance timeline |
| **Secure Authentication** | JWT-based auth with access and refresh token rotation |
| **Cloud Asset Storage** | CVs and assets are securely stored via Cloudinary |

---

## Architecture

MockMate.ai is a **microservices monorepo** composed of two independent, loosely-coupled services:

```
MockMate.ai/
└── src/
    ├── Backend/          # .NET 9 REST API  (C#)
    │   └── MockMate.Api/
    └── AI/               # Python AI Service (FastAPI)
```

### Backend Service — .NET 9 / C#

The core business logic service, responsible for:

- **User authentication and identity management** via ASP.NET Core Identity and JWT Bearer tokens
- **Interview session lifecycle** — creation, question serving, answer recording, timer tracking, and auto-scoring
- **Code execution routing** — forwarding candidate code submissions to the **Judge0** API and evaluating results against test cases
- **Data persistence** — all entities (users, sessions, questions, skills, tracks) are stored in **SQL Server** via **Entity Framework Core** with code-first migrations
- **AI Service integration** — calling the Python service with an uploaded CV to receive structured skill data before assembling a session

The backend is organized using **vertical slice architecture**, with one MediatR handler per feature endpoint, and **FluentValidation** for all incoming request validation.


### AI Service — Python / FastAPI

A lightweight, stateless microservice responsible for CV intelligence:

1. Accepts a PDF CV upload (with an optional plain-text job description)
2. Extracts raw text using **pdfplumber** and **pdfminer.six**
3. Builds a structured prompt and calls **Google Gemini 2.5 Flash** via the Generative AI SDK
4. Parses and validates the LLM JSON response using **Pydantic v2** into a clean schema:
   ```json
   {
     "track_name": "Backend Development",
     "level": "Mid-Level",
     "technical_skills": ["C#", ".NET", "SQL Server", "REST APIs"]
   }
   ```
5. Returns the structured payload to the Backend service for session assembly

---

## Tech Stack

### Backend

| Technology | Version | Purpose |
|---|---|---|
| .NET / C# | 9.0 | Core API framework |
| ASP.NET Core Identity | 9.0 | User management and password hashing |
| Entity Framework Core | 9.0 | ORM and database migrations |
| SQL Server | — | Primary relational data store |
| MediatR | 14.0 | CQRS-style request/handler pipeline |
| FluentValidation | 12.1 | Request model validation |
| JWT Bearer Auth | — | Stateless token-based authentication |
| Cloudinary SDK | 1.28 | Cloud storage for CVs and assets |
| Judge0 API | — | Remote sandbox for code execution |
| Swashbuckle / Swagger | 6.4 | Interactive API documentation |

### AI Service

| Technology | Version | Purpose |
|---|---|---|
| Python | 3.12 | Runtime |
| FastAPI | 0.133 | Async REST framework |
| Uvicorn | 0.41 | ASGI production server |
| pdfplumber | 0.11 | Primary PDF text extraction |
| pdfminer.six | 20251230 | Fallback PDF parsing layer |
| Google Gemini 2.5 Flash | — | LLM for CV analysis and skill extraction |
| google-generativeai SDK | 0.8 | Gemini API client |
| Pydantic | v2 | Response schema validation |
| python-dotenv | 1.2 | Environment variable management |

---

## Roadmap

### Phase 1 — Database-Driven Assessment *(Current)*

The platform is fully operational using a **curated question bank** approach:

1. The AI Service parses the uploaded CV and/or job description and returns `track_name`, `seniority level`, and `technical_skills`.
2. The Backend queries SQL Server to select relevant MCQ and coding questions matching the extracted track, seniority, and skills.
3. The interview session is assembled, timed, served to the candidate, executed via Judge0, auto-graded, and persisted.

### Phase 2 — AI-Generated Assessment *(Planned)*

In the next major phase, LLMs will be deeply integrated into the assessment itself:

- **Dynamic Question Generation** — Questions and coding problems generated on-the-fly by Gemini, tailored to each candidate's unique skill profile
- **Alternative Solution Evaluation** — The LLM will review submitted code for correctness, efficiency, and code quality beyond test-case pass/fail
- **Adaptive Difficulty** — Session difficulty adjusts in real-time based on the candidate's live performance
- **Natural Language Feedback** — AI-generated explanations and improvement suggestions delivered at the end of each session

---

## Getting Started

### Prerequisites

Ensure the following are installed on your machine:

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Python 3.12+](https://www.python.org/downloads/)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Express edition is sufficient for local development)
- [Git](https://git-scm.com/)

---

### Step 1 — Clone the Repository

```bash
git clone https://github.com/your-org/MockMate.ai.git
cd MockMate.ai
```

---

### Step 2 — Backend Setup (.NET API)

Navigate to the API project:

```bash
cd src/Backend/MockMate.Api
```

**Configure your settings:**

Open `appsettings.json` or create a local override file `appsettings.Development.json` and fill in your credentials:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS; Database=MockMateDb; Trusted_Connection=True; TrustServerCertificate=True"
  },
  "JwtSettings": {
    "Secret": "<a-random-secret-key-at-least-32-characters-long>",
    "Issuer": "MockMate.Auth",
    "Audience": "MockMate.Clients",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "Cloudinary": {
    "CloudName": "<your-cloudinary-cloud-name>",
    "ApiKey": "<your-cloudinary-api-key>",
    "ApiSecret": "<your-cloudinary-api-secret>"
  },
  "AiService": {
    "BaseUrl": "http://localhost:8000"
  },
  "Judge0": {
    "BaseUrl": "https://judge029.p.rapidapi.com",
    "ApiKey": "<your-rapidapi-judge0-key>",
    "ApiHost": "judge029.p.rapidapi.com"
  }
}
```

**Restore packages and run:**

```bash
dotnet restore
dotnet run
```

---

### Step 3 — AI Service Setup (Python / FastAPI)

Navigate to the AI service:

```bash
cd src/AI
```

**Create and activate a virtual environment:**

```bash
# Windows (PowerShell)
python -m venv .venv
.venv\Scripts\Activate.ps1

# macOS / Linux
python -m venv .venv
source .venv/bin/activate
```

**Install dependencies:**

```bash
pip install -r requirements.txt
```

**Start the development server:**

```bash
uvicorn api:app --reload --host 0.0.0.0 --port 8000
```

---

### Step 4 — Verify Both Services

Once both services are running, confirm they are healthy:

```bash
# Backend health check
curl http://localhost:5143/health
# Expected: "Healthy"

# AI Service docs
# Open in browser: http://localhost:8000/docs
```

---

<div align="center">

*Built with passion as a graduation project.*

</div>


