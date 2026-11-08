// Scroll-Rad für Zahlenwerte (Stunden, Minuten): Die Liste rastet per CSS (scroll-snap) auf einen Eintrag ein.
// Das Skript meldet den eingerasteten Eintrag an .NET und stellt ihn bei Änderungen von außen ein.
const SETTLE_DELAY_MS = 90;

export function attach(scroller, dotNetRef, selectedIndex) {
    let settleTimer = null;
    let reportedIndex = selectedIndex;

    const itemHeight = () => scroller.querySelector(".time-wheel-item")?.offsetHeight || 1;
    const itemCount = () => scroller.querySelectorAll(".time-wheel-item").length;
    const clampIndex = (index) => Math.max(0, Math.min(itemCount() - 1, index));

    const settle = () => {
        settleTimer = null;
        moveReported = false;
        const index = clampIndex(Math.round(scroller.scrollTop / itemHeight()));
        if (index === reportedIndex) {
            return;
        }

        reportedIndex = index;
        dotNetRef.invokeMethodAsync("OnWheelSettled", index);
    };

    const onScroll = () => {
        if (settleTimer !== null) {
            clearTimeout(settleTimer);
        }

        settleTimer = setTimeout(settle, SETTLE_DELAY_MS);
    };

    const setIndex = (index, smooth) => {
        const target = clampIndex(index);
        reportedIndex = target;
        scroller.scrollTo({ top: target * itemHeight(), behavior: smooth ? "smooth" : "auto" });
    };

    // Nur Eingaben des Nutzers (Finger, Mausrad) melden den Beginn der Bewegung; programmatisches Scrollen nicht.
    // So reagiert die Oberfläche sofort, nicht erst wenn der Finger den Bildschirm verlässt.
    let moveReported = false;
    const onUserMove = () => {
        if (moveReported) {
            return;
        }

        moveReported = true;
        dotNetRef.invokeMethodAsync("OnWheelMoved");
    };
    const onUserMoveEnd = () => {
        moveReported = false;
    };

    scroller.addEventListener("scroll", onScroll, { passive: true });
    scroller.addEventListener("touchmove", onUserMove, { passive: true });
    scroller.addEventListener("wheel", onUserMove, { passive: true });
    scroller.addEventListener("touchend", onUserMoveEnd, { passive: true });
    scroller.addEventListener("touchcancel", onUserMoveEnd, { passive: true });
    setIndex(selectedIndex, false);

    return {
        setIndex,
        dispose() {
            if (settleTimer !== null) {
                clearTimeout(settleTimer);
            }

            scroller.removeEventListener("scroll", onScroll);
            scroller.removeEventListener("touchmove", onUserMove);
            scroller.removeEventListener("wheel", onUserMove);
            scroller.removeEventListener("touchend", onUserMoveEnd);
            scroller.removeEventListener("touchcancel", onUserMoveEnd);
        }
    };
}
