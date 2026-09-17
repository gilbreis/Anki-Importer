import importlib
import json
import os
import traceback

from aqt import gui_hooks, mw

ANKICONNECT_ID = "2055492159"
AWESOMETTS_ID = "1436550454"
ACTION_NAME = "awesomeTtsGenerate"
INFO_ACTION = "awesomeTtsBridgeInfo"
BRIDGE_VERSION = "0.3.2"
_last_register_error = None


def _load_addon(addon_id):
    try:
        return importlib.import_module(addon_id)
    except Exception as exc:
        raise RuntimeError(f"Add-on obrigatório não encontrado ou não carregado: {addon_id}: {exc}") from exc


def _awesome_module():
    module = _load_addon(AWESOMETTS_ID)
    addon = getattr(module, "awesometts", None)
    if addon is None:
        raise RuntimeError("Módulo interno 'awesometts' não encontrado no AwesomeTTS.")
    if not hasattr(addon, "router"):
        raise RuntimeError("AwesomeTTS carregado, mas o router interno não está disponível.")
    return addon


def _copy_audio_to_collection(path):
    if not path or not os.path.exists(path):
        raise RuntimeError(f"AwesomeTTS não retornou um arquivo de áudio válido: {path!r}")
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
        state["error"] = f"{type(exception).__name__}: {exception}"

    addon.router(
        svc_id="google",
        text=str(text),
        options={"voice": voice, "speed": float(speed)},
        callbacks={"okay": okay, "fail": fail},
        async_variable=False,
    )

    if state["error"]:
        raise RuntimeError(f"AwesomeTTS/Google Translate falhou: {state['error']}")
    if not state["path"]:
        raise RuntimeError("AwesomeTTS não gerou o áudio e não retornou erro detalhado.")

    filename = _copy_audio_to_collection(state["path"])
    sound_tag = _append_sound(note_id, field_name, filename)
    return {
        "ok": True,
        "filename": filename,
        "soundTag": sound_tag,
        "service": "google",
        "voice": voice,
        "speed": float(speed),
    }


def _bridge_info():
    awesome_ready = False
    awesome_error = None
    try:
        addon = _awesome_module()
        awesome_ready = True
        services = dict(addon.router.get_services()) if hasattr(addon.router, "get_services") else {}
        google_available = "google" in services or any(name == "Google Translate" for name in services.values())
    except Exception as exc:
        google_available = False
        awesome_error = f"{type(exc).__name__}: {exc}"

    return {
        "bridge": True,
        "bridgeVersion": BRIDGE_VERSION,
        "registered": _last_register_error is None,
        "registerError": _last_register_error,
        "awesomeTts": awesome_ready,
        "awesomeTtsError": awesome_error,
        "googleServiceAvailable": google_available,
        "serviceId": "google",
        "serviceName": "Google Translate",
        "voice": "en-US",
        "speed": 1.0,
    }


def _write_status(ok, detail=None):
    try:
        path = os.path.join(os.path.dirname(__file__), "bridge-status.json")
        with open(path, "w", encoding="utf-8") as handle:
            json.dump({
                "ok": bool(ok),
                "bridgeVersion": BRIDGE_VERSION,
                "detail": detail,
            }, handle, ensure_ascii=False, indent=2)
    except Exception:
        pass


def _register_bridge():
    global _last_register_error
    try:
        ankiconnect = _load_addon(ANKICONNECT_ID)
        api = ankiconnect.util.api

        # Sempre usa a classe da instância real que está atendendo em 127.0.0.1:8765.
        live_instance = getattr(ankiconnect, "ac", None)
        cls = type(live_instance) if live_instance is not None else getattr(ankiconnect, "AnkiConnect", None)
        if cls is None:
            raise RuntimeError("Classe AnkiConnect não encontrada no add-on 2055492159.")

        if not hasattr(cls, INFO_ACTION):
            @api()
            def awesomeTtsBridgeInfo(self):
                return _bridge_info()
            setattr(cls, INFO_ACTION, awesomeTtsBridgeInfo)

        if not hasattr(cls, ACTION_NAME):
            @api()
            def awesomeTtsGenerate(self, text, noteId, fieldName="Back", voice="en-US", speed=1.0):
                return _generate_with_awesometts(text, noteId, fieldName, voice, speed)
            setattr(cls, ACTION_NAME, awesomeTtsGenerate)

        # Valida contra o mesmo mecanismo que o AnkiConnect usa para refletir ações.
        if live_instance is not None and hasattr(live_instance, "apiReflect"):
            reflected = live_instance.apiReflect(
                scopes=["actions"],
                actions=[INFO_ACTION, ACTION_NAME],
            ) or {}
            exposed = set(reflected.get("actions") or [])
            missing = [name for name in (INFO_ACTION, ACTION_NAME) if name not in exposed]
            if missing:
                raise RuntimeError("Ações não expostas pelo AnkiConnect após registro: " + ", ".join(missing))

        _last_register_error = None
        _write_status(True, "bridge registrado no AnkiConnect")
        return True
    except Exception as exc:
        _last_register_error = f"{type(exc).__name__}: {exc}\n{traceback.format_exc(limit=4)}"
        _write_status(False, _last_register_error)
        return False


# Registra imediatamente quando o add-on é carregado.
_register_bridge()

# Tenta novamente após o perfil abrir, caso a ordem de carga dos add-ons
# tenha feito o AnkiConnect ainda não estar disponível na primeira tentativa.
gui_hooks.profile_did_open.append(_register_bridge)
