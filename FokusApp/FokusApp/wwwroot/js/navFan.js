const HOLD_DELAY_MS = 350;
const MOVE_TOLERANCE_PX = 12;

export function attach(button, dotNetRef) {
    let holdTimer = null;
    let holding = false;
    let startX = 0;
    let startY = 0;
    let hovered = null;

    const setHovered = (item) => {
        if (hovered === item) {
            return;
        }

        hovered?.removeAttribute("data-hover");
        hovered = item;
        hovered?.setAttribute("data-hover", "true");
    };

    const itemAt = (x, y) => document.elementFromPoint(x, y)?.closest("[data-nav-route]") ?? null;

    const clearTimer = () => {
        if (holdTimer !== null) {
            clearTimeout(holdTimer);
            holdTimer = null;
        }
    };

    const suppressNextClick = () => {
        const handler = (e) => {
            e.stopImmediatePropagation();
            e.preventDefault();
        };
        button.addEventListener("click", handler, { capture: true, once: true });
        setTimeout(() => button.removeEventListener("click", handler, { capture: true }), 150);
    };

    const finish = (route) => {
        clearTimer();
        setHovered(null);

        if (!holding) {
            return;
        }

        holding = false;
        suppressNextClick();
        dotNetRef.invokeMethodAsync("OnHoldReleased", route);
    };

    const onDown = (e) => {
        if (e.pointerType === "mouse" && e.button !== 0) {
            return;
        }

        startX = e.clientX;
        startY = e.clientY;
        holding = false;
        clearTimer();
        button.setPointerCapture?.(e.pointerId);
        holdTimer = setTimeout(() => {
            holdTimer = null;
            holding = true;
            dotNetRef.invokeMethodAsync("OnHoldStarted");
        }, HOLD_DELAY_MS);
    };

    const onMove = (e) => {
        if (holding) {
            setHovered(itemAt(e.clientX, e.clientY));
            return;
        }

        if (holdTimer !== null &&
            (Math.abs(e.clientX - startX) > MOVE_TOLERANCE_PX || Math.abs(e.clientY - startY) > MOVE_TOLERANCE_PX)) {
            clearTimer();
        }
    };

    const onUp = (e) => finish(itemAt(e.clientX, e.clientY)?.dataset.navRoute ?? null);
    const onCancel = () => finish(null);
    const onContextMenu = (e) => e.preventDefault();

    button.addEventListener("pointerdown", onDown);
    button.addEventListener("pointermove", onMove);
    button.addEventListener("pointerup", onUp);
    button.addEventListener("pointercancel", onCancel);
    button.addEventListener("contextmenu", onContextMenu);

    return {
        dispose() {
            clearTimer();
            setHovered(null);
            button.removeEventListener("pointerdown", onDown);
            button.removeEventListener("pointermove", onMove);
            button.removeEventListener("pointerup", onUp);
            button.removeEventListener("pointercancel", onCancel);
            button.removeEventListener("contextmenu", onContextMenu);
        }
    };
}
