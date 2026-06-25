"""
Voice Interview Service
Handles Gemini chat session lifecycle, streaming, and evaluation.
"""
import asyncio
import json
import logging
import re

from fastapi import WebSocket
from google import genai
from google.genai import types
from google.api_core.exceptions import ResourceExhausted, TooManyRequests

from core.config import Config
from prompts.voice_interview import (
    build_system_prompt,
    EVALUATION_PROMPT,
)

logger = logging.getLogger(__name__)

# Matches the position right after . ? ! — used to split sentences.
_SENTENCE_END = re.compile(r'(?<=[.?!])(?:\s+|$)')


# ── Helpers ────────────────────────────────────────────────────────────────────

def _flush_sentence(buffer: str) -> tuple[str, list[str]]:
    """
    Return (leftover, sentences[]).
    Splits on sentence boundaries and returns each sentence individually
    so the TTS engine receives one clean sentence at a time with punctuation intact.
    """
    sentences = []
    while True:
        m = _SENTENCE_END.search(buffer)
        if not m:
            break
        sentence = buffer[:m.start() + 1].strip()
        if sentence:
            sentences.append(sentence)
        buffer = buffer[m.end():]
    return buffer, sentences


def _strip_markdown_fences(text: str) -> str:
    text = re.sub(r"^```(?:json)?\s*", "", text)
    text = re.sub(r"\s*```$", "", text)
    return text.strip()


# def _clean_for_tts(text: str) -> str:
#     """Strip markdown characters that might trip up the TTS engine."""
#     text = text.replace("*", "").replace("_", "").replace("#", "")
#     return text.strip()


def _make_config(raw: dict, **extra) -> types.GenerateContentConfig:
    """Convert a Config dict to GenerateContentConfig, merging any extra kwargs."""
    kwargs = {k: v for k, v in raw.items() if k != "thinking_config"}
    thinking_budget = raw.get("thinking_config", {}).get("thinking_budget")
    if thinking_budget is not None:
        kwargs["thinking_config"] = types.ThinkingConfig(thinking_budget=thinking_budget)
    kwargs.update(extra)
    return types.GenerateContentConfig(**kwargs)


# ── Session factory ────────────────────────────────────────────────────────────

async def create_chat_session(track: str) -> tuple:
    """
    Create a Gemini async chat session for the given track.
    Returns (session, client).
    """
    client = genai.Client(api_key=Config.GEMINI_API_KEY)
    session = client.aio.chats.create(
        model=Config.VOICE_MODEL,
        config=_make_config(
            Config.VOICE_GENERATION_CONFIG,
            system_instruction=build_system_prompt(track),
        ),
    )
    logger.info("Gemini chat session created.")
    return session, client


# ── Streaming ──────────────────────────────────────────────────────────────────

async def stream_ai_response(
    user_text: str,
    websocket: WebSocket,
    chat_session,
    history: list,
    record_user_turn: bool = True,
) -> None:
    """
    Stream the AI response sentence-by-sentence for smooth TTS playback.
    Set record_user_turn=False for the opening prompt (it's not a real user message).
    """
    ai_response = ""
    buffer = ""
    try:
        response_stream = await chat_session.send_message_stream(user_text)

        async for chunk in response_stream:
            text = getattr(chunk, "text", None)
            if not text:
                continue
            buffer += text
            ai_response += text
            buffer, sentences = _flush_sentence(buffer)
            for sentence in sentences:
                await websocket.send_json({"event": "ai_sentence", "text": _clean_for_tts(sentence)})

        # Flush any trailing text after the stream ends
        if buffer.strip():
            await websocket.send_json({"event": "ai_sentence", "text": _clean_for_tts(buffer.strip())})
            ai_response += buffer

        await websocket.send_json({"event": "ai_turn_complete"})

        if record_user_turn:
            history.append(f"USER: {user_text}")
        history.append(f"ALEX: {ai_response.strip()}")

    except (ResourceExhausted, TooManyRequests) as e:
        logger.warning(f"Rate limit during streaming: {e}")
        await websocket.send_json({
            "event": "ai_error",
            "text": "I'm temporarily unavailable. Please wait a moment and try again.",
        })
    except Exception as e:
        logger.error(f"Streaming error: {e}", exc_info=True)
        await websocket.send_json({
            "event": "ai_error",
            "text": "I encountered an error. Please repeat your last answer.",
        })


# ── Evaluation ─────────────────────────────────────────────────────────────────

_EMPTY_FEEDBACK = {
    "overallSummary": "Evaluation could not be generated. Please try again.",
    "strengths": [],
    "weaknesses": [],
    "detailedFeedback": [],
}


async def evaluate_and_close(
    websocket: WebSocket,
    client: genai.Client,
    history: list,
) -> None:
    """
    Evaluate the full interview using an independent generate_content call
    with the manually tracked history.
    """
    try:
        history_text = "\n".join(history)
        logger.info(f"Evaluating interview — {len(history)} turns in history.")
        full_prompt = f"INTERVIEW TRANSCRIPT:\n{history_text}\n\n{EVALUATION_PROMPT}"

        response = await client.aio.models.generate_content(
            model=Config.VOICE_EVAL_MODEL,
            contents=full_prompt,
            config=_make_config(Config.VOICE_EVALUATION_CONFIG),
        )
        raw = _strip_markdown_fences(response.text.strip())
        evaluation = json.loads(raw)

        await websocket.send_json({
            "event": "interview_result",
            "score": evaluation.get("score", 0),
            "feedback": evaluation.get("feedback", {}),
        })

    except json.JSONDecodeError as e:
        logger.error(f"Failed to parse evaluation JSON: {e}")
        await websocket.send_json({
            "event": "interview_result",
            "score": 0,
            "feedback": {**_EMPTY_FEEDBACK},
        })

    except (ResourceExhausted, TooManyRequests) as e:
        logger.warning(f"Rate limit during evaluation: {e}")
        await websocket.send_json({
            "event": "interview_result",
            "score": 0,
            "feedback": {**_EMPTY_FEEDBACK, "overallSummary": "Service is busy. Please try again shortly."},
        })

    except Exception as e:
        logger.error(f"Evaluation error: {e}", exc_info=True)
        await websocket.send_json({
            "event": "interview_result",
            "score": 0,
            "feedback": {**_EMPTY_FEEDBACK, "overallSummary": "An error occurred during evaluation."},
        })

    finally:
        try:
            await websocket.close(code=1000, reason="Interview ended")
        except Exception:
            pass
