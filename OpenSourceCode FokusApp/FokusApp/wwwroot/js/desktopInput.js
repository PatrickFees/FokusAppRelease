// Desktop-Eingaben (Maus und Tastatur): Escape, Maus-Zurück-Taste und Alt+Pfeil-links schließen
// das oberste Popup bzw. das geöffnete Navigationsmenü, so wie die Zurück-Geste auf Android.
const MOUSE_BACK_BUTTON = 3;

const clickTopmostOverlay = () => {
    const overlays = document.querySelectorAll(".popup-overlay");
    const overlay = overlays[overlays.length - 1];
    if (!overlay) {
        return false;
    }

    (overlay.querySelector("[data-escape-close]") ?? overlay).click();
    return true;
};

const closeNavigationMenu = () => {
    const backdrop = document.querySelector(".nav-backdrop");
    if (!backdrop) {
        return false;
    }

    backdrop.click();
    return true;
};

export function attach(dotNetRef) {
    const requestBack = async () => {
        if (await dotNetRef.invokeMethodAsync("TryHandleBackRequest")) {
            return;
        }

        if (!closeNavigationMenu()) {
            clickTopmostOverlay();
        }
    };

    const onKeyDown = (e) => {
        const isEscape = e.key === "Escape" && !e.defaultPrevented;
        const isAltLeft = e.key === "ArrowLeft" && e.altKey && !e.ctrlKey && !e.shiftKey;
        if (!isEscape && !isAltLeft) {
            return;
        }

        // Das Aufgaben-Fenster verarbeitet Escape selbst, solange der Fokus darin liegt.
        if (isEscape && e.target instanceof Element && e.target.closest(".modal-content")) {
            return;
        }

        e.preventDefault();
        requestBack();
    };

    const onMouseBack = (e) => {
        if (e.button !== MOUSE_BACK_BUTTON) {
            return;
        }

        e.preventDefault();
        if (e.type === "mouseup") {
            requestBack();
        }
    };

    document.addEventListener("keydown", onKeyDown);
    document.addEventListener("mouseup", onMouseBack);
    document.addEventListener("auxclick", onMouseBack);

    return {
        dispose() {
            document.removeEventListener("keydown", onKeyDown);
            document.removeEventListener("mouseup", onMouseBack);
            document.removeEventListener("auxclick", onMouseBack);
        }
    };
}
