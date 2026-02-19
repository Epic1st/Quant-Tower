import os
import threading
from datetime import datetime, timezone
from typing import Any

from fastapi import FastAPI, Header, HTTPException, Query

app = FastAPI(docs_url=None, redoc_url=None)

RELAY_SECRET = os.getenv("RELAY_SECRET", "")
if not RELAY_SECRET:
    raise RuntimeError("RELAY_SECRET environment variable is required")

_state_lock = threading.Lock()
_state: dict[str, Any] = {
    "id": 0,
    "ts": None,
    "payload": None,
}


def _utc_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def _authorize(provided: str | None) -> None:
    if not provided or provided != RELAY_SECRET:
        raise HTTPException(status_code=401, detail="unauthorized")


@app.get("/health")
def health() -> dict[str, Any]:
    return {"ok": True, "ts": _utc_iso()}


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
        snapshot = dict(_state)

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
        snapshot = dict(_state)

    if snapshot["id"] <= id:
        return {
            "ok": True,
            "updated": False,
            "id": snapshot["id"],
            "ts": snapshot["ts"],
            "payload": None,
        }

    return {
        "ok": True,
        "updated": True,
        "id": snapshot["id"],
        "ts": snapshot["ts"],
        "payload": snapshot["payload"],
    }
