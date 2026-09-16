import base64
import hashlib
import html
import json
import re
import sys
import tkinter as tk
import webbrowser
from io import BytesIO
from pathlib import Path
from tkinter import filedialog, messagebox
from urllib.error import URLError
from urllib.request import Request, urlopen

from gtts import gTTS

ANKI_URL = "http://127.0.0.1:8765"
APP_TITLE = "Anki Importer"
APP_VERSION = "0.2.0"
VERSION_URL = "https://raw.githubusercontent.com/gilbreis/Anki-Importer/main/local-importer/version.txt"
DOWNLOAD_URL = "https://github.com/gilbreis/Anki-Importer/releases/download/latest/AnkiImporterSetup.exe"
SOUND_RE = re.compile(r"\[sound:[^\]]+\]", re.IGNORECASE)


def version_tuple(value):
    parts = []
    for part in str(value).strip().split("."):
        try:
            parts.append(int(part))
        except ValueError:
            return ()
    return tuple(parts)


def check_for_update():
    try:
        request = Request(VERSION_URL, headers={"User-Agent": f"AnkiImporter/{APP_VERSION}"})
        with urlopen(request, timeout=3) as response:
            latest = response.read().decode("utf-8").strip()

        current_version = version_tuple(APP_VERSION)
        latest_version = version_tuple(latest)
        if not current_version or not latest_version or latest_version <= current_version:
            return

        download = messagebox.askyesno(
            "Atualização disponível",
            f"Uma nova versão do Anki Importer está disponível.\n\n"
            f"Versão instalada: {APP_VERSION}\n"
            f"Nova versão: {latest}\n\n"
            "Deseja abrir o download da atualização agora?\n\n"
            "Você pode escolher Não e continuar a importação normalmente.",
        )
        if download:
            webbrowser.open(DOWNLOAD_URL)
    except Exception:
        # A falta de internet ou indisponibilidade do GitHub nunca impede a importação.
        pass


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


def existing_notes_by_front(deck, front_field, back_field):
    note_ids = anki("findNotes", {"query": f'deck:"{deck.replace(chr(34), "")}"'}) or []
    if not note_ids:
        return {}

    notes = anki("notesInfo", {"notes": note_ids}) or []
    values = {}
    for note in notes:
        fields = note.get("fields") or {}
        front_data = fields.get(front_field)
        back_data = fields.get(back_field)
        if not isinstance(front_data, dict):
            continue

        front = normalize(front_data.get("value"))
        if not front:
            continue

        back_value = ""
        if isinstance(back_data, dict):
            back_value = str(back_data.get("value") or "")

        values.setdefault(front.casefold(), {
            "noteId": note.get("noteId"),
            "front": front,
            "back": back_value,
            "hasAudio": bool(SOUND_RE.search(back_value)),
        })
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


def append_audio_to_existing_note(note_id, back_field, current_back, audio_name):
    if not note_id:
        raise RuntimeError("Não foi possível identificar o cartão existente no Anki.")

    current_back = str(current_back or "").rstrip()
    separator = "<br>" if current_back else ""
    updated_back = f"{current_back}{separator}[sound:{audio_name}]"

    anki("updateNoteFields", {
        "note": {
            "id": note_id,
            "fields": {back_field: updated_back},
        }
    })


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

    existing = existing_notes_by_front(deck, front_field, back_field)
    seen = set()
    new_cards = []
    duplicates = 0
    invalid = 0
    existing_audio_added = 0
    existing_audio_errors = []
    audio_cache = {}

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
        if key in seen:
            duplicates += 1
            continue
        seen.add(key)

        existing_note = existing.get(key)
        if existing_note:
            if tts_enabled and not existing_note["hasAudio"]:
                try:
                    cache_key = (tts_language, back.casefold())
                    audio_name = audio_cache.get(cache_key)
                    if not audio_name:
                        audio_name = create_tts_audio(back, tts_language)
                        audio_cache[cache_key] = audio_name

                    append_audio_to_existing_note(
                        existing_note["noteId"],
                        back_field,
                        existing_note["back"],
                        audio_name,
                    )
                    existing_note["hasAudio"] = True
                    existing_note["back"] = f'{existing_note["back"]}<br>[sound:{audio_name}]'
                    existing_audio_added += 1
                except Exception:
                    existing_audio_errors.append(front)
            else:
                duplicates += 1
            continue

        new_cards.append((front, back, card.get("tags") or []))

    notes = []
    prepared = []
    audio_generated = 0
    audio_errors = []

    for front, back, tags in new_cards:
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
        "existingAudioAdded": existing_audio_added,
        "audioErrors": audio_errors,
        "existingAudioErrors": existing_audio_errors,
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

    check_for_update()

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
        existing_audio_error_count = len(report["existingAudioErrors"])
        messagebox.showinfo(
            f"{APP_TITLE} {APP_VERSION}",
            f'Deck: {report["deck"]}\n\n'
            f'Encontradas: {report["found"]}\n'
            f'Novos cartões: {report["added"]}\n'
            f'Duplicadas ignoradas: {report["duplicates"]}\n'
            f'Áudio adicionado em existentes: {report["existingAudioAdded"]}\n'
            f'Inválidas: {report["invalid"]}\n'
            f'Áudios em novos cartões: {report["audioGenerated"]}\n'
            f'Erros de áudio em novos: {audio_error_count}\n'
            f'Erros de áudio em existentes: {existing_audio_error_count}\n'
            f'Erros: {error_count}',
        )
    except Exception as exc:
        messagebox.showerror(f"{APP_TITLE} {APP_VERSION}", str(exc))


if __name__ == "__main__":
    main()
