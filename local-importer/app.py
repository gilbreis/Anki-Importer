import json
import sys
import tkinter as tk
from pathlib import Path
from tkinter import filedialog, messagebox
from urllib.request import Request, urlopen
from urllib.error import URLError

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


def import_package(package):
    deck = package["deck"]
    model = package["model"]
    front_field = package["frontField"]
    back_field = package["backField"]

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
    for front, back, tags in valid:
        notes.append({
            "deckName": deck,
            "modelName": model,
            "fields": {front_field: front, back_field: back},
            "options": {"allowDuplicate": False},
            "tags": ["chatgpt-import", *[str(t) for t in tags if str(t).strip()]],
        })

    added = 0
    errors = []
    if notes:
        results = anki("addNotes", {"notes": notes}) or []
        for note, note_id in zip(valid, results):
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
        messagebox.showinfo(
            APP_TITLE,
            f'Deck: {report["deck"]}\n\n'
            f'Encontradas: {report["found"]}\n'
            f'Adicionadas: {report["added"]}\n'
            f'Duplicadas: {report["duplicates"]}\n'
            f'Inválidas: {report["invalid"]}\n'
            f'Erros: {error_count}',
        )
    except Exception as exc:
        messagebox.showerror(APP_TITLE, str(exc))


if __name__ == "__main__":
    main()
