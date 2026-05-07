from fastapi import APIRouter, WebSocket, WebSocketDisconnect
import json
import logging
import os
import random
from dotenv import load_dotenv
from google import genai
from google.genai import types

load_dotenv()
router = APIRouter()
logger = logging.getLogger(__name__)

API_KEYS = [value for key, value in os.environ.items() if key.startswith("GEMINI_API_KEY_")]

if not API_KEYS:
    logger.error("No Gemini API keys found in .env!")

async def stream_ai_response(user_text: str, websocket: WebSocket, chat_session):
    current_sentence = ""
    try:
        # Use the chat session to send the message, retaining memory
        response_stream = await chat_session.send_message_stream(user_text)
        
        async for chunk in response_stream:
            if chunk.text:
                current_sentence += chunk.text
                # Check for punctuation indicating the end of a sentence
                if current_sentence.rstrip().endswith(('.', '?', '!', '\n')):
                    if current_sentence.strip():
                        await websocket.send_json({
                            "event": "ai_sentence", 
                            "text": current_sentence.strip()
                        })
                    current_sentence = ""
                    
        if current_sentence.strip():
            await websocket.send_json({
                "event": "ai_sentence", 
                "text": current_sentence.strip()
            })
            
        # AI has completely finished generating for this turn
        await websocket.send_json({
            "event": "ai_turn_complete"
        })

    except Exception as e:
        logger.error(f"Error streaming from Gemini: {e}")
        await websocket.send_json({
            "event": "ai_error",
            "text": "I'm sorry, I encountered an error processing your response."
        })

@router.websocket("/ws/voice-interview")
async def voice_interview_endpoint(websocket: WebSocket):
    await websocket.accept()
    logger.info("WebSocket connection accepted.")

    try:
        auth_message = await websocket.receive_text()
        auth_data = json.loads(auth_message)
        if "token" not in auth_data:
            logger.warning("No token provided in the initial message.")
            await websocket.close(code=1008, reason="Missing token")
            return
            
        token = auth_data["token"]
        logger.info(f"Authentication token received (len: {len(token)}).")
        
        config_message = await websocket.receive_text()
        config_data = json.loads(config_message)
        
        if config_data.get("event") != "start_interview" or "track" not in config_data:
            logger.warning("Invalid configuration payload received.")
            await websocket.close(code=1008, reason="Missing track configuration")
            return
            
        track = config_data["track"]
        logger.info(f"Interview track received: {track}")
        
        dynamic_system_prompt = f"""You are a rigorous, senior technical interviewer for a top-tier tech company, conducting a {track} engineering interview.
            Your tone is highly professional, objective, and analytical. Do not be overly chatty, overly enthusiastic, or act like a tutor.
            Follow these strict rules:
            1. Ask authentic, real-world technical questions commonly found in FAANG-level interviews. Focus on practical engineering challenges relevant to {track} (e.g., for backend: database indexing, caching strategies, message queues, race conditions; for frontend: state management, DOM performance, closures).
            2. Ask ONE clear, specific, and advanced technical question at a time. Wait for the candidate's response.
            3. If the candidate gives a surface-level answer, probe deeper into the "why" and "how" and uncover edge cases.
            4. Challenge their assumptions gracefully but firmly. Ask them how their proposed solution scales under heavy load or mitigates potential failures.
            5. NEVER give away the direct answer if they struggle; instead, pivot to a related fundamental concept.
            6. Keep your spoken responses EXTREMELY short and punchy (maximum 20-30 words per response). Do not provide long setups, examples, or context. Ask your question as directly and concisely as possible.
        """
        
        selected_key = random.choice(API_KEYS)
        client = genai.Client(api_key=selected_key)
        chat_session = client.aio.chats.create(
            model="gemini-2.5-flash",
            config=types.GenerateContentConfig(
                system_instruction=dynamic_system_prompt,
            )
        )

        logger.info("Gemini Chat Session created successfully.")
        
        hidden_prompt = f"""Initiate the interview. Introduce yourself formally as Alex, a Senior Engineering Manager for the {track} team. Ask the candidate to briefly introduce their background to begin. Keep it to one short sentence."""
        logger.info("Triggering AI's first move.")
        await stream_ai_response(hidden_prompt, websocket, chat_session)
        
        while True:
            message = await websocket.receive_text()
            data = json.loads(message)
            
            if data.get("event") == "user_speech":
                text = data.get("text", "")
                logger.info(f"User Speech Received: {text}")
                print("=" * 50)
                print("User Speech Received: ", text)
                print("=" * 50)
                await stream_ai_response(text, websocket, chat_session)
            elif data.get("event") == "end_interview":
                logger.info("End interview event received. Evaluating performance...")
                prompt = """The interview is now over. Based on our full conversation history, evaluate the candidate's performance.
CRITICAL INSTRUCTION: Write all text fields addressing the candidate directly in the second person (use "you" and "your"). Do NOT use third-person phrasing like "the candidate" or "they". For example, write "You showed strong knowledge of..." instead of "The candidate showed...".
Return ONLY a raw JSON object — no markdown, no ```json``` fences, no extra text — with this EXACT structure:
{
  "score": <integer 0-100>,
  "feedback": {
    "overallSummary": "<2-3 sentence summary of the performance, addressing the user directly (e.g., 'You did a great job at...')>",
    "strengths": ["<strength 1>", "<strength 2>"],
    "weaknesses": ["<area to improve 1>", "<area to improve 2>"],
    "detailedFeedback": [
      {
        "questionTitle": "<the question or topic that was answered poorly>",
        "feedback": "<what was wrong or missing in your answer>",
        "suggestion": "<what your correct or ideal answer should have included>"
      }
    ]
  }
}

IMPORTANT rules for detailedFeedback:
- Include ONLY questions or topics where the candidate answered incorrectly, incompletely, showed a misconception, or needed significant prompting.
- Do NOT include questions the candidate answered well or correctly.
- If the candidate answered everything correctly, return an empty array: "detailedFeedback": []"""

                try:
                    response = await chat_session.send_message(prompt)
                    raw_text = response.text.strip()
                    if raw_text.startswith("```"):
                        raw_text = raw_text.split("```")[1]
                        if raw_text.startswith("json"):
                            raw_text = raw_text[4:]
                        raw_text = raw_text.strip()

                    evaluation = json.loads(raw_text)
                    await websocket.send_json({
                        "event": "interview_result",
                        "score": evaluation.get("score", 0),
                        "feedback": evaluation.get("feedback", {})
                    })
                except Exception as e:
                    logger.error(f"Error evaluating interview: {e}")
                    await websocket.send_json({
                        "event": "interview_result",
                        "score": 0,
                        "feedback": {
                            "overallSummary": "Failed to generate evaluation due to an error.",
                            "strengths": [],
                            "weaknesses": [],
                            "detailedFeedback": []
                        }
                    })
                finally:
                    await websocket.close(code=1000, reason="Interview Ended")
                    break
            else:
                logger.warning(f"Unexpected event received: {data.get('event')}")
                
    except WebSocketDisconnect:
        logger.info("WebSocket client disconnected.")
    except json.JSONDecodeError:
        logger.error("Invalid JSON received.")
        await websocket.close(code=1003, reason="Invalid JSON")
    except Exception as e:
        logger.error(f"Unexpected error in websocket: {e}")
        await websocket.close(code=1011, reason="Internal Server Error")