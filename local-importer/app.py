import html
import json
import re
import sys
import tkinter as tk
import webbrowser
from pathlib import Path
from tkinter import filedialog, messagebox
from urllib.error import URLError
from urllib.request import Request, urlopen

ANKI_URL = "http://127.0.0.1:8765"
APP_TITLE = "Anki Importer"
APP_VERSION = "0.3.0"
VERSION_URL = "https://raw.githubusercontent.com/gilbreis/Anki-Importer/main/local-importer/version.txt"
DOWNLOAD_URL = "https://github.com/gilbreis/Anki-Importer/releases/download/latest/AnkiImporterSetup.exe"
SOUND_RE = re.compile(r"\[sound:[^\]]+\]", re.IGNORECASE)


def version_tuple(value):
    try:
        return tuple(int(part) for part in str(value).strip().split("."))
    except ValueError:
        return ()


def check_for_update():
    try:
        request = Request(VERSION_URL, headers={"User-Agent": f"AnkiImporter/{APP_VERSION}"})
        with urlopen(request, timeout=3) as response:
            latest = response.read().decode("utf-8").strip()
        if version_tuple(latest) > version_tuple(APP_VERSION):
            if messagebox.askyesno(
                "Atualização disponível",
                f"Versão instalada: {APP_VERSION}\nNova versão: {latest}\n\nDeseja baixar agora?",
            ):
                webbrowser.open(DOWNLOAD_URL)
    except Exception:
        pass


def anki(action, params=None):
    payload = json.dumps({"action": action, "version": 6, "params": params or {}}, ensure_ascii=False).encode("utf-8")
    request = Request(ANKI_URL, data=payload, headers={"Content-Type": "application/json; charset=utf-8"})
    try:
        with urlopen(request, timeout=30) as response:
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
        "cards": cards,
    }


def normalize(value):
    return " ".join(str(value or "").split()).strip()


def ensure_awesometts_ready():
    try:
        info = anki("awesomeTtsBridgeInfo") or {}
    except Exception as exc:
        raise RuntimeError(
            "Integração AwesomeTTS não está disponível.\n\n"
            "Confirme que o AnkiConnect (2055492159) e o AwesomeTTS (1436550454) estão instalados, "
            "reinstale o Anki Importer e reinicie o Anki."
        ) from exc
    if not info.get("awesomeTts"):
        raise RuntimeError(
            "AwesomeTTS não foi encontrado.\n\n"
            "Instale o complemento 1436550454 no Anki e reinicie o Anki."
        )


def generate_audio(note_id, text, back_field):
    return anki("awesomeTtsGenerate", {
        "text": text,
        "noteId": note_id,
        "fieldName": back_field,
        "voice": "en-US",
        "speed": 1.0,
    })


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
        back_value = str(back_data.get("value") or "") if isinstance(back_data, dict) else ""
        values.setdefault(front.casefold(), {
            "noteId": note.get("noteId"),
            "back": back_value,
            "hasAudio": bool(SOUND_RE.search(back_value)),
        })
    return values


def import_package(package):
    deck = package["deck"]
    model = package["model"]
    front_field = package["frontField"]
    back_field = package["backField"]
    tts_enabled = package["tts"]

    anki("version")
    if tts_enabled:
        ensure_awesometts_ready()

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
                    generate_audio(existing_note["noteId"], back, back_field)
                    existing_audio_added += 1
                except Exception:
                    existing_audio_errors.append(front)
            else:
                duplicates += 1
            continue
        new_cards.append((front, back, card.get("tags") or []))

    notes = []
    for front, back, tags in new_cards:
        notes.append({
            "deckName": deck,
            "modelName": model,
            "fields": {front_field: html.escape(front), back_field: html.escape(back)},
            "options": {"allowDuplicate": False},
            "tags": ["chatgpt-import", *[str(t) for t in tags if str(t).strip()]],
        })

    added = 0
    audio_generated = 0
    audio_errors = []
    errors = []
    if notes:
        results = anki("addNotes", {"notes": notes}) or []
        for (front, back, _tags), note_id in zip(new_cards, results):
            if note_id is None:
                errors.append(front)
                continue
            added += 1
            if tts_enabled:
                try:
                    generate_audio(note_id, back, back_field)
                    audio_generated += 1
                except Exception:
                    audio_errors.append(front)

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
    filename = filedialog.askopenfilename(title="Abrir pacote do Anki Importer", filetypes=[("Pacote Anki Importer", "*.ankiimport")])
    return Path(filename) if filename else None


def main():
    root = tk.Tk()
    root.withdraw()
    check_for_update()

    path = Path(sys.argv[1]) if len(sys.argv) >= 2 and Path(sys.argv[1]).exists() else choose_file(root)
    if path is None:
        return

    try:
        report = import_package(load_package(path))
        messagebox.showinfo(
            f"{APP_TITLE} {APP_VERSION}",
            f'Deck: {report["deck"]}\n\n'
            f'Encontradas: {report["found"]}\n'
            f'Novos cartões: {report["added"]}\n'
            f'Duplicadas ignoradas: {report["duplicates"]}\n'
            f'Áudio AwesomeTTS em existentes: {report["existingAudioAdded"]}\n'
            f'Áudio AwesomeTTS em novos: {report["audioGenerated"]}\n'
            f'Inválidas: {report["invalid"]}\n'
            f'Erros de áudio em existentes: {len(report["existingAudioErrors"])}\n'
            f'Erros de áudio em novos: {len(report["audioErrors"])}\n'
            f'Erros: {len(report["errors"])}',
        )
    except Exception as exc:
        messagebox.showerror(f"{APP_TITLE} {APP_VERSION}", str(exc))


if __name__ == "__main__":
    main()
