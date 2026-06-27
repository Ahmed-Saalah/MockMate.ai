import asyncio
import json
import logging
from fastapi import APIRouter, WebSocket, WebSocketDisconnect
from core.config import Config
from prompts.voice_interview import build_opening_prompt
from services.voice_interview_service import (
    create_chat_session,
    stream_ai_response,
    evaluate_and_close,
)

router = APIRouter()
logger = logging.getLogger(__name__)
_HANDSHAKE_TIMEOUT = 15   
_IDLE_TIMEOUT = 45       

async def _receive_json(websocket: WebSocket, timeout: float) -> dict:
    raw = await asyncio.wait_for(websocket.receive_text(), timeout=timeout)
    return json.loads(raw)

@router.websocket("/ws/voice-interview")
async def voice_interview_endpoint(websocket: WebSocket):
    await websocket.accept()
    logger.info("WebSocket connection accepted.")

    try:
        auth_data = await _receive_json(websocket, timeout=_HANDSHAKE_TIMEOUT)
        if "token" not in auth_data:
            logger.warning("No token in initial message.")
            await websocket.close(code=1008, reason="Missing token")
            return
        logger.info(f"Auth token received (len={len(auth_data['token'])}).")

        config_data = await _receive_json(websocket, timeout=_HANDSHAKE_TIMEOUT)
        if config_data.get("event") != "start_interview" or "track" not in config_data:
            logger.warning("Invalid configuration payload.")
            await websocket.close(code=1008, reason="Missing track configuration")
            return

        track = config_data["track"]
        logger.info(f"Interview track: {track}")
        chat_session, client = await create_chat_session(track)
        history: list[str] = []

        await stream_ai_response(
            build_opening_prompt(track),
            websocket, chat_session, history,
            record_user_turn=False,
        )

        while True:
            try:
                data = await _receive_json(websocket, timeout=_IDLE_TIMEOUT)
            except asyncio.TimeoutError:
                logger.warning("Idle timeout — closing connection.")
                await websocket.close(code=1001, reason="Idle timeout")
                break

            event = data.get("event")

            if event == "user_speech":
                text = data.get("text", "").strip()[:Config.VOICE_MAX_USER_INPUT]
                if not text:
                    logger.debug("Empty user_speech — skipping.")
                    continue
                logger.info(f"User: {text[:120]}")
                await stream_ai_response(text, websocket, chat_session, history)

            elif event == "end_interview":
                logger.info(f"End interview — {len(history)} turns recorded.")
                await evaluate_and_close(websocket, client, history)
                break

            else:
                logger.warning(f"Unknown event: {event!r}")

    except WebSocketDisconnect:
        logger.info("Client disconnected.")
    except asyncio.TimeoutError:
        logger.warning("Handshake timeout.")
        await websocket.close(code=1001, reason="Handshake timeout")
    except json.JSONDecodeError:
        logger.error("Invalid JSON received.")
        await websocket.close(code=1003, reason="Invalid JSON")
    except Exception as e:
        logger.error(f"Unexpected WebSocket error: {e}", exc_info=True)
        try:
            await websocket.close(code=1011, reason="Internal Server Error")
        except Exception:
            pass
