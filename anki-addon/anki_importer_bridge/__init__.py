import importlib
import os

from aqt import gui_hooks, mw

ANKICONNECT_ID = "2055492159"
AWESOMETTS_ID = "1436550454"
ACTION_NAME = "awesomeTtsGenerate"
INFO_ACTION = "awesomeTtsBridgeInfo"


def _load_addon(addon_id):
    try:
        return importlib.import_module(addon_id)
    except Exception as exc:
        raise RuntimeError(f"Add-on obrigatório não encontrado: {addon_id}") from exc


def _awesome_module():
    module = _load_addon(AWESOMETTS_ID)
    addon = getattr(module, "awesometts", None)
    if addon is None or not hasattr(addon, "router"):
        raise RuntimeError("AwesomeTTS não está carregado corretamente.")
    return addon


def _copy_audio_to_collection(path):
    if not path or not os.path.exists(path):
        raise RuntimeError("AwesomeTTS não retornou um arquivo de áudio válido.")
    return mw.col.media.add_file(path)


def _append_sound(note_id, field_name, filename):
    note = mw.col.get_note(int(note_id))
    if field_name not in note:
        raise RuntimeError(f"Campo não encontrado na nota: {field_name}")

    sound_tag = f"[sound:{filename}]"
    current = str(note[field_name] or "")
    if sound_tag in current:
        return sound_tag

    note[field_name] = f"{current.rstrip()}<br>{sound_tag}" if current.strip() else sound_tag
    mw.col.update_note(note)
    return sound_tag


def _generate_with_awesometts(text, note_id, field_name="Back", voice="en-US", speed=1.0):
    addon = _awesome_module()
    state = {"path": None, "error": None}

    def okay(path):
        state["path"] = path

    def fail(exception, _text):
        state["error"] = str(exception)

    addon.router(
        svc_id="google",
        text=str(text),
        options={"voice": voice, "speed": float(speed)},
        callbacks={"okay": okay, "fail": fail},
        async_variable=False,
    )

    if state["error"]:
        raise RuntimeError(state["error"])
    if not state["path"]:
        raise RuntimeError("AwesomeTTS não gerou o áudio.")

    filename = _copy_audio_to_collection(state["path"])
    sound_tag = _append_sound(note_id, field_name, filename)
    return {"ok": True, "filename": filename, "soundTag": sound_tag}


def _register_bridge():
    try:
        ankiconnect = _load_addon(ANKICONNECT_ID)
        api = ankiconnect.util.api
        cls = ankiconnect.AnkiConnect

        if not hasattr(cls, INFO_ACTION):
            @api()
            def awesomeTtsBridgeInfo(self):
                try:
                    _awesome_module()
                    awesome_ready = True
                except Exception:
                    awesome_ready = False
                return {
                    "bridge": True,
                    "awesomeTts": awesome_ready,
                    "service": "Google Translate",
                    "voice": "en-US",
                    "speed": 1.0,
                }

            setattr(cls, INFO_ACTION, awesomeTtsBridgeInfo)

        if not hasattr(cls, ACTION_NAME):
            @api()
            def awesomeTtsGenerate(self, text, noteId, fieldName="Back", voice="en-US", speed=1.0):
                return _generate_with_awesometts(text, noteId, fieldName, voice, speed)

            setattr(cls, ACTION_NAME, awesomeTtsGenerate)

    except Exception:
        pass


gui_hooks.profile_did_open.append(_register_bridge)
