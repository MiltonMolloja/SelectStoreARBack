"""
SelectStoreAR — Telegram Group Reader

Lee mensajes de un grupo/canal de Telegram usando tu cuenta personal
(no necesitás ser admin) y los envía al backend para importar productos.

Uso:
    1. Copiá .env.example → .env y completá los valores
    2. pip install -r requirements.txt
    3. python reader.py

La primera vez te va a pedir tu número de teléfono y un código de
verificación. Después queda guardada la sesión en 'session.session'.
"""

import asyncio
import os
import sys
import re
import logging
from datetime import datetime, timedelta, timezone

from dotenv import load_dotenv
from telethon import TelegramClient
from telethon.tl.types import Message
import httpx

# ── Configuración ────────────────────────────────────────────────────────────

load_dotenv()

API_ID = int(os.getenv("TELEGRAM_API_ID", "0"))
API_HASH = os.getenv("TELEGRAM_API_HASH", "")
GROUP_NAME = os.getenv("TELEGRAM_GROUP", "")
API_BASE_URL = os.getenv("API_BASE_URL", "http://localhost:5012")
API_ADMIN_TOKEN = os.getenv("API_ADMIN_TOKEN", "")
CHECK_INTERVAL = int(os.getenv("CHECK_INTERVAL_HOURS", "4"))
LOOKBACK_HOURS = int(os.getenv("LOOKBACK_HOURS", "24"))

SESSION_FILE = os.path.join(os.path.dirname(__file__), "session")

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
    datefmt="%Y-%m-%d %H:%M:%S",
)
log = logging.getLogger("telegram-reader")

# ── Funciones ────────────────────────────────────────────────────────────────


def is_price_list(text: str) -> bool:
    """Detecta si un mensaje es una lista de precios (5+ líneas con u$número)."""
    count = sum(
        1 for line in text.split("\n") if re.search(r"u\s*\$\s*\d", line, re.IGNORECASE)
    )
    return count >= 5


async def sync_to_backend(
    text: str, message_id: int, message_date: datetime
) -> dict | None:
    """Envía el texto al endpoint de sync del backend."""
    headers = {
        "Content-Type": "application/json",
        "Authorization": f"Bearer {API_ADMIN_TOKEN}",
    }

    payload = {"text": text}

    try:
        async with httpx.AsyncClient(timeout=30) as client:
            # Primero preview para ver qué detecta
            preview_resp = await client.post(
                f"{API_BASE_URL}/api/admin/telegram/preview-prices",
                json=payload,
                headers=headers,
            )

            if preview_resp.status_code != 200:
                log.error(
                    "Preview failed (HTTP %d): %s",
                    preview_resp.status_code,
                    preview_resp.text[:200],
                )
                return None

            preview = preview_resp.json()
            parsed_count = preview.get("parsedCount", 0)

            if parsed_count == 0:
                log.info("  → Preview: 0 products parsed, skipping sync")
                return None

            log.info("  → Preview: %d products detected, syncing...", parsed_count)

            # Sync real
            sync_resp = await client.post(
                f"{API_BASE_URL}/api/admin/telegram/sync-prices",
                json=payload,
                headers=headers,
            )

            if sync_resp.status_code != 200:
                log.error(
                    "Sync failed (HTTP %d): %s",
                    sync_resp.status_code,
                    sync_resp.text[:200],
                )
                return None

            result = sync_resp.json()
            log.info(
                "  → Sync OK: created=%d updated=%d skipped=%d errors=%d",
                result.get("created", 0),
                result.get("updated", 0),
                result.get("skipped", 0),
                result.get("errors", 0),
            )
            return result

    except httpx.ConnectError:
        log.error("Cannot connect to backend at %s", API_BASE_URL)
        return None
    except Exception as ex:
        log.error("Unexpected error during sync: %s", ex)
        return None


async def read_group_messages(client: TelegramClient):
    """Lee los mensajes recientes del grupo y envía las listas de precios al backend."""

    log.info("Looking for group/channel: '%s'", GROUP_NAME)

    # Buscar el grupo por nombre o ID
    target = None
    async for dialog in client.iter_dialogs():
        if GROUP_NAME.lstrip("-").isdigit():
            if str(dialog.id) == GROUP_NAME or str(
                dialog.entity.id
            ) == GROUP_NAME.lstrip("-"):
                target = dialog.entity
                break
        elif dialog.name and GROUP_NAME.lower() in dialog.name.lower():
            target = dialog.entity
            break

    if target is None:
        log.error("Group '%s' not found. Available groups:", GROUP_NAME)
        async for dialog in client.iter_dialogs():
            if hasattr(dialog.entity, "megagroup") or hasattr(
                dialog.entity, "broadcast"
            ):
                log.info("  - %s (ID: %s)", dialog.name, dialog.id)
        return

    log.info("Found: %s (ID: %s)", getattr(target, "title", GROUP_NAME), target.id)

    # Leer mensajes de las últimas LOOKBACK_HOURS horas
    cutoff = datetime.now(timezone.utc) - timedelta(hours=LOOKBACK_HOURS)
    messages_processed = 0
    price_lists_found = 0

    async for message in client.iter_messages(target, offset_date=cutoff, reverse=True):
        if not isinstance(message, Message):
            continue

        text = message.text or message.message or ""
        if not text or len(text) < 20:
            continue

        messages_processed += 1

        if is_price_list(text):
            price_lists_found += 1
            msg_date = message.date.strftime("%Y-%m-%d %H:%M")
            log.info(
                "Price list found (msg #%d, %s, %d chars)",
                message.id,
                msg_date,
                len(text),
            )

            await sync_to_backend(text, message.id, message.date)

    log.info(
        "Done: %d messages scanned, %d price lists found",
        messages_processed,
        price_lists_found,
    )


async def main():
    """Entry point."""
    if not API_ID or not API_HASH:
        log.error("TELEGRAM_API_ID and TELEGRAM_API_HASH are required.")
        log.error("Get them at https://my.telegram.org → API development tools")
        sys.exit(1)

    if not GROUP_NAME:
        log.error("TELEGRAM_GROUP is required (group name or ID)")
        sys.exit(1)

    if not API_ADMIN_TOKEN:
        log.warning("API_ADMIN_TOKEN not set — sync will fail (401)")

    log.info("Starting Telegram reader...")
    log.info("  Group: %s", GROUP_NAME)
    log.info("  Backend: %s", API_BASE_URL)
    log.info("  Lookback: %d hours", LOOKBACK_HOURS)
    log.info(
        "  Interval: %s",
        f"every {CHECK_INTERVAL}h" if CHECK_INTERVAL > 0 else "one-shot",
    )

    client = TelegramClient(SESSION_FILE, API_ID, API_HASH)
    await client.start()

    log.info("Logged in as: %s", (await client.get_me()).first_name)

    while True:
        try:
            await read_group_messages(client)
        except Exception as ex:
            log.error("Error reading messages: %s", ex)

        if CHECK_INTERVAL <= 0:
            break

        log.info("Sleeping %d hours until next check...", CHECK_INTERVAL)
        await asyncio.sleep(CHECK_INTERVAL * 3600)

    await client.disconnect()
    log.info("Done.")


if __name__ == "__main__":
    asyncio.run(main())
