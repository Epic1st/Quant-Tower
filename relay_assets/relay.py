import os
import threading
from collections import deque
from datetime import datetime, timezone
from typing import Any

from fastapi import FastAPI, Header, HTTPException, Query

app = FastAPI(docs_url=None, redoc_url=None)

RELAY_SECRET = os.getenv("RELAY_SECRET", "")
if not RELAY_SECRET:
    raise RuntimeError("RELAY_SECRET environment variable is required")

RELAY_MAX_HISTORY = int(os.getenv("RELAY_MAX_HISTORY", "500"))
if RELAY_MAX_HISTORY < 10:
    RELAY_MAX_HISTORY = 10

_state_lock = threading.Lock()
_state: dict[str, Any] = {
    "id": 0,
    "ts": None,
    "payload": None,
    "history": deque(maxlen=RELAY_MAX_HISTORY),
    "poll_count": 0,
    "last_poll_id": None,
    "last_poll_ts": None,
}


def _utc_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def _authorize(provided: str | None) -> None:
    if not provided or provided != RELAY_SECRET:
        raise HTTPException(status_code=401, detail="unauthorized")


@app.get("/health")
def health() -> dict[str, Any]:
    with _state_lock:
        return {
            "ok": True,
            "ts": _utc_iso(),
            "latest_id": int(_state["id"]),
            "history_size": len(_state["history"]),
            "poll_count": int(_state["poll_count"]),
            "last_poll_id": _state["last_poll_id"],
            "last_poll_ts": _state["last_poll_ts"],
        }


@app.post("/signal")
def post_signal(
    payload: dict[str, Any],
    x_auth: str | None = Header(default=None, alias="X-Auth"),
) -> dict[str, Any]:
    _authorize(x_auth)

    with _state_lock:
        _state["id"] += 1
        _state["ts"] = _utc_iso()
        _state["payload"] = payload
        record = {
            "id": _state["id"],
            "ts": _state["ts"],
            "payload": payload,
        }
        _state["history"].append(record)
        snapshot = {
            "id": record["id"],
            "ts": record["ts"],
        }

    return {
        "ok": True,
        "id": snapshot["id"],
        "ts": snapshot["ts"],
    }


@app.get("/signal")
def get_signal(
    id: int = Query(default=0, ge=0),
    auth: str | None = Query(default=None),
) -> dict[str, Any]:
    _authorize(auth)

    with _state_lock:
        _state["poll_count"] += 1
        _state["last_poll_id"] = id
        _state["last_poll_ts"] = _utc_iso()

        latest_id = int(_state["id"])
        latest_ts = _state["ts"]
        latest_payload = _state["payload"]
        history = list(_state["history"])

    if latest_id <= id:
        return {
            "ok": True,
            "updated": False,
            "id": latest_id,
            "ts": latest_ts,
            "payload": None,
        }

    # Return the earliest unseen signal so polling clients can consume ids sequentially.
    for record in history:
        if int(record["id"]) > id:
            return {
                "ok": True,
                "updated": True,
                "id": int(record["id"]),
                "ts": record["ts"],
                "payload": record["payload"],
            }

    # Fallback if history rolled over: return latest snapshot.
    return {
        "ok": True,
        "updated": True,
        "id": latest_id,
        "ts": latest_ts,
        "payload": latest_payload,
    }
