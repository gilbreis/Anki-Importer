import base64
import hashlib
import html
import json
import sys
import tkinter as tk
from io import BytesIO
from pathlib import Path
from tkinter import filedialog, messagebox
from urllib.error import URLError
from urllib.request import Request, urlopen

from gtts import gTTS

ANKI_URL = "http://127.0.0.1:8765"
APP_TITLE = "Anki Importer"


def anki(action, params=None):
    payload = json.dumps({"action": action, "version": 6, "params": params or {}}, ensure_ascii=False).encode("utf-8")
    request = Request(ANKI_URL, data=payload, headers={"Content-Type": "application/json; charset=utf-8"})
    try:
        with urlopen(request, timeout=10) as response:
            result = json.loads(response.read().decode("utf-8"))
    except (URLError, OSError) as exc:
        raise RuntimeError("Não consegui acessar o Anki. Abra o Anki Desktop e confirme que o AnkiConnect está instalado.") from exc

    if result.get("error"):
        raise RuntimeError(str(result["error"]))
    return result.get("result")


def load_package(path: Path):
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except Exception as exc:
        raise RuntimeError("O arquivo .ankiimport é inválido.") from exc

    deck = str(data.get("deck", "")).strip()
    cards = data.get("cards")
    if not deck or not isinstance(cards, list):
        raise RuntimeError("O arquivo .ankiimport não contém um deck e uma lista de cartões válidos.")

    return {
        "deck": deck,
        "model": str(data.get("model") or "Basic"),
        "frontField": str(data.get("frontField") or "Front"),
        "backField": str(data.get("backField") or "Back"),
        "tts": bool(data.get("tts", True)),
        "ttsLanguage": str(data.get("ttsLanguage") or "en"),
        "cards": cards,
    }


def normalize(value):
    return " ".join(str(value or "").split()).strip()


def existing_fronts(deck, front_field):
    note_ids = anki("findNotes", {"query": f'deck:"{deck.replace(chr(34), "")}"'}) or []
    if not note_ids:
        return set()

    notes = anki("notesInfo", {"notes": note_ids}) or []
    values = set()
    for note in notes:
        field = (note.get("fields") or {}).get(front_field)
        if isinstance(field, dict):
            value = normalize(field.get("value"))
            if value:
                values.add(value.casefold())
    return values


def create_tts_audio(text, language="en"):
    digest = hashlib.sha256(f"{language}\0{text}".encode("utf-8")).hexdigest()[:20]
    filename = f"anki_importer_tts_{digest}.mp3"

    audio = BytesIO()
    gTTS(text=text, lang=language, slow=False, lang_check=False).write_to_fp(audio)
    encoded = base64.b64encode(audio.getvalue()).decode("ascii")
    stored_name = anki("storeMediaFile", {"filename": filename, "data": encoded})
    if not stored_name:
        raise RuntimeError("O Anki não conseguiu armazenar o áudio TTS.")
    return stored_name


def import_package(package):
    deck = package["deck"]
    model = package["model"]
    front_field = package["frontField"]
    back_field = package["backField"]
    tts_enabled = package["tts"]
    tts_language = package["ttsLanguage"]

    anki("version")
    decks = set(anki("deckNames") or [])
    if deck not in decks:
        anki("createDeck", {"deck": deck})

    existing = existing_fronts(deck, front_field)
    seen = set()
    valid = []
    duplicates = 0
    invalid = 0

    for card in package["cards"]:
        if not isinstance(card, dict):
            invalid += 1
            continue

        front = normalize(card.get("front"))
        back = normalize(card.get("back"))
        if not front or not back:
            invalid += 1
            continue

        key = front.casefold()
        if key in seen or key in existing:
            duplicates += 1
            continue

        seen.add(key)
        valid.append((front, back, card.get("tags") or []))

    notes = []
    prepared = []
    audio_generated = 0
    audio_errors = []
    audio_cache = {}

    for front, back, tags in valid:
        back_value = html.escape(back)

        if tts_enabled:
            try:
                cache_key = (tts_language, back.casefold())
                audio_name = audio_cache.get(cache_key)
                if not audio_name:
                    audio_name = create_tts_audio(back, tts_language)
                    audio_cache[cache_key] = audio_name
                back_value = f"{back_value}<br>[sound:{audio_name}]"
                audio_generated += 1
            except Exception:
                audio_errors.append(front)

        notes.append({
            "deckName": deck,
            "modelName": model,
            "fields": {front_field: html.escape(front), back_field: back_value},
            "options": {"allowDuplicate": False},
            "tags": ["chatgpt-import", *[str(t) for t in tags if str(t).strip()]],
        })
        prepared.append((front, back))

    added = 0
    errors = []
    if notes:
        results = anki("addNotes", {"notes": notes}) or []
        for note, note_id in zip(prepared, results):
            if note_id is None:
                errors.append(note[0])
            else:
                added += 1

    return {
        "deck": deck,
        "found": len(package["cards"]),
        "added": added,
        "duplicates": duplicates,
        "invalid": invalid,
        "audioGenerated": audio_generated,
        "audioErrors": audio_errors,
        "errors": errors,
    }


def choose_file(root):
    filename = filedialog.askopenfilename(
        title="Abrir pacote do Anki Importer",
        filetypes=[("Pacote Anki Importer", "*.ankiimport")],
    )
    return Path(filename) if filename else None


def main():
    root = tk.Tk()
    root.withdraw()

    path = None
    if len(sys.argv) >= 2:
        candidate = Path(sys.argv[1])
        if candidate.exists():
            path = candidate
    if path is None:
        path = choose_file(root)
    if path is None:
        return

    try:
        package = load_package(path)
        report = import_package(package)
        error_count = len(report["errors"])
        audio_error_count = len(report["audioErrors"])
        messagebox.showinfo(
            APP_TITLE,
            f'Deck: {report["deck"]}\n\n'
            f'Encontradas: {report["found"]}\n'
            f'Adicionadas: {report["added"]}\n'
            f'Duplicadas: {report["duplicates"]}\n'
            f'Inválidas: {report["invalid"]}\n'
            f'Áudios TTS: {report["audioGenerated"]}\n'
            f'Erros de áudio: {audio_error_count}\n'
            f'Erros: {error_count}',
        )
    except Exception as exc:
        messagebox.showerror(APP_TITLE, str(exc))


if __name__ == "__main__":
    main()
