// Geometry and timing helpers for the plant scene of the introduction (OnboardingStage.razor).
// All positions are measured in the viewport; the plant is moved with CSS variables and a transform transition.

const FLOOR_PLANT_CENTER_X = 74;
const FLOOR_SCALE = 1.3;
const root = document.documentElement;

let docked = false;
let dockedPlant = null;
let dockedFloor = null;

const nextFrame = () => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));

export function isReducedMotion() {
    return window.matchMedia("(prefers-reduced-motion: reduce)").matches;
}

function pose(plant, centerX, centerY, rotation, scale) {
    plant.style.setProperty("--x", `${centerX - plant.offsetWidth / 2}px`);
    plant.style.setProperty("--y", `${centerY - plant.offsetHeight / 2}px`);
    plant.style.setProperty("--r", `${rotation}deg`);
    plant.style.setProperty("--s", `${scale}`);
}

function poseWithoutTransition(plant, centerX, centerY, rotation, scale) {
    plant.classList.add("notrans");
    pose(plant, centerX, centerY, rotation, scale);
    void plant.offsetWidth;
    plant.classList.remove("notrans");
}

function targetCenter(plant, floor, target, scale) {
    if (target === "scope") {
        // Under the work/private switch of the demonstration: the plant jumps up to press it.
        const toggle = document.querySelector(".stage-demo [data-onboarding=\"scope-toggle\"]");
        if (toggle) {
            const rect = toggle.getBoundingClientRect();
            return { x: rect.left + rect.width / 2, y: rect.bottom + (plant.offsetHeight * scale) / 2 + 4 };
        }

        return { x: window.innerWidth / 2, y: 90 };
    }

    if (target === "menu") {
        const button = document.querySelector("[data-onboarding=\"nav-toggler\"]");
        if (button) {
            const rect = button.getBoundingClientRect();
            return { x: rect.left + rect.width / 2 - 8, y: rect.bottom + 10 };
        }

        return { x: window.innerWidth - 60, y: 70 };
    }

    const floorTop = floor.getBoundingClientRect().top;
    return { x: FLOOR_PLANT_CENTER_X, y: floorTop + 2 - (plant.offsetHeight * scale) / 2 };
}

/** Puts the standing-in plant exactly over the sleeping one in the header, hides the real one and keeps it hidden. */
export function anchorAtHeader(plant) {
    const button = document.querySelector("[data-onboarding=\"plant-button\"]");
    const plantHeight = plant.offsetHeight;
    let centerX;
    let centerY;

    if (button) {
        const rect = button.getBoundingClientRect();
        centerX = rect.left + rect.width / 2;
        // The header plant is clipped at its lower edge, which sits at the ceiling after the 180 degree turn.
        centerY = rect.top + plantHeight / 2 - (plantHeight - rect.height);
    } else {
        const topRow = document.querySelector("[data-onboarding=\"top-row\"]");
        const rect = topRow ? topRow.getBoundingClientRect() : { left: 0, top: 0, width: window.innerWidth };
        centerX = rect.left + rect.width * 0.64;
        centerY = rect.top + plantHeight / 2 - 4;
    }

    root.classList.add("onboarding-stage");
    poseWithoutTransition(plant, centerX, centerY, 180, 1);
    plant.style.visibility = "visible";
}

/** Moves the plant to "floor" or "menu" with a turn and a scale; resolves when it has arrived. */
export function moveTo(plant, floor, target, rotation, scale, duration, easing) {
    const center = targetCenter(plant, floor, target, scale);
    const instant = isReducedMotion() || duration <= 0;

    plant.style.setProperty("--dur", instant ? "0ms" : `${duration}ms`);
    plant.style.setProperty("--ease", easing);
    pose(plant, center.x, center.y, rotation, scale);

    if (target !== "floor") {
        docked = false;
    }

    if (instant) {
        return nextFrame();
    }

    return new Promise(resolve => {
        const finish = () => {
            plant.removeEventListener("transitionend", onEnd);
            clearTimeout(timer);
            resolve();
        };
        const onEnd = event => {
            if (event.target === plant && event.propertyName === "transform") {
                finish();
            }
        };
        const timer = setTimeout(finish, duration + 150);
        plant.addEventListener("transitionend", onEnd);
    });
}

/** After the landing: resets the turn (360 degrees equal 0) and keeps the plant on the floor when the window changes. */
export function dockAtFloor(plant, floor) {
    const center = targetCenter(plant, floor, "floor", FLOOR_SCALE);
    poseWithoutTransition(plant, center.x, center.y, 0, FLOOR_SCALE);
    docked = true;
    dockedPlant = plant;
    dockedFloor = floor;
    window.removeEventListener("resize", onResize);
    window.addEventListener("resize", onResize);
}

function onResize() {
    if (docked && dockedPlant && dockedFloor && dockedPlant.isConnected && dockedFloor.isConnected) {
        const center = targetCenter(dockedPlant, dockedFloor, "floor", FLOOR_SCALE);
        poseWithoutTransition(dockedPlant, center.x, center.y, 0, FLOOR_SCALE);
    }
}

/** Plays a one-shot CSS animation class again. */
export function restartClass(element, className) {
    element.classList.remove(className);
    void element.offsetWidth;
    element.classList.add(className);
}

/** Locks the app below the scene: no pointer, no keyboard, no screen reader access. */
export function lockApp(locked) {
    const page = document.querySelector("[data-onboarding=\"app-page\"]");
    if (page) {
        page.toggleAttribute("inert", locked);
    }

    root.classList.toggle("onboarding-locked", locked);
}

export function pressMenu(pressed) {
    const button = document.querySelector("[data-onboarding=\"nav-toggler\"]");
    if (button) {
        button.toggleAttribute("data-onboarding-pressed", pressed);
    }
}

export function setBlur(blurred) {
    root.classList.toggle("onboarding-blur", blurred);
}

/** Resolves true as soon as an element matching the selector exists, false after the timeout. */
export function waitFor(selector, timeout) {
    if (document.querySelector(selector)) {
        return Promise.resolve(true);
    }

    return new Promise(resolve => {
        const finish = found => {
            observer.disconnect();
            clearTimeout(timer);
            resolve(found);
        };
        const observer = new MutationObserver(() => {
            if (document.querySelector(selector)) {
                finish(true);
            }
        });
        const timer = setTimeout(() => finish(false), timeout);
        observer.observe(document.body, { childList: true, subtree: true });
    });
}

/** Lays the ring replica exactly over the real timer clock and hides the real one while the replica is shown. */
export function placeRing(ring) {
    const clock = document.querySelector("[data-onboarding=\"timer-clock\"]");
    ring.style.visibility = "visible";

    if (!clock) {
        // No timer page found: the replica stays centered in the upper half.
        ring.style.removeProperty("--ring-size");
        ring.style.left = `${Math.max(0, (window.innerWidth - ring.offsetWidth) / 2)}px`;
        ring.style.top = "7rem";
        return false;
    }

    const rect = clock.getBoundingClientRect();
    ring.style.setProperty("--ring-size", `${rect.width}px`);
    ring.style.left = `${rect.left}px`;
    ring.style.top = `${rect.top}px`;
    root.classList.add("onboarding-ring-active");
    return true;
}

export function releaseRing() {
    root.classList.remove("onboarding-ring-active");
}

/** Starts the ring demonstration; the leaf first flies from the plant to the end of the ring. */
export async function startRingDemo(ring, leaf, plant) {
    ring.classList.remove("demo-run");
    ring.style.removeProperty("--lx");
    ring.style.removeProperty("--ly");

    if (isReducedMotion()) {
        return;
    }

    await nextFrame();
    const target = leaf.getBoundingClientRect();
    const plantRect = plant.getBoundingClientRect();
    ring.style.setProperty("--lx", `${plantRect.left + plantRect.width / 2 - (target.left + target.width / 2)}px`);
    ring.style.setProperty("--ly", `${plantRect.top + 8 - (target.top + target.height / 2)}px`);
    void ring.offsetWidth;
    ring.classList.add("demo-run");
}

/* ---------- Demonstration of "Your day": the pointer (the leaf) and the replicas over the real page ---------- */

function elementAt(selector, index) {
    return document.querySelectorAll(selector)[index] ?? null;
}

function centerOf(element) {
    const rect = element.getBoundingClientRect();
    return { x: rect.left + rect.width / 2, y: rect.top + rect.height / 2 };
}

const pause = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));

/** The pointer appears at the top of the plant, as if the leaf grew out of it. */
export function pointerAppear(pointer, plant) {
    const rect = plant.getBoundingClientRect();
    pointer.style.transition = "none";
    pointer.style.transform = `translate(${rect.left + rect.width / 2}px, ${rect.top + 6}px) rotate(-18deg)`;
    void pointer.getBoundingClientRect();
    pointer.style.opacity = "1";
}

/** Moves the pointer onto an element and resolves when it has arrived. */
export async function pointerMove(pointer, selector, index, duration) {
    const target = elementAt(selector, index);
    if (!target) {
        return;
    }

    const center = centerOf(target);
    pointer.style.transition = duration > 0 ? `transform ${duration}ms ease-in-out` : "none";
    pointer.style.transform = `translate(${center.x}px, ${center.y}px) rotate(-18deg)`;
    await pause(duration);
}

/** Moves onto an element and presses it: a ring spreads out and the element dips. */
export async function pointerClick(pointer, selector, index, duration) {
    await pointerMove(pointer, selector, index, duration);
    const target = elementAt(selector, index);
    if (!target) {
        return;
    }

    const center = centerOf(target);
    const ripple = document.createElement("div");
    ripple.className = "stage-ripple";
    ripple.style.left = `${center.x}px`;
    ripple.style.top = `${center.y}px`;
    document.body.appendChild(ripple);
    target.setAttribute("data-onboarding-pressed", "");

    // A quick tap (short way) is also a short press.
    const quick = duration > 0 && duration < 400;
    await pause(duration > 0 ? (quick ? 70 : 200) : 0);
    target.removeAttribute("data-onboarding-pressed");
    setTimeout(() => ripple.remove(), 600);
    await pause(duration > 0 ? (quick ? 40 : 160) : 0);
}

/** The leaf goes away to the right. */
export async function pointerLeave(pointer) {
    pointer.style.transition = "transform 800ms ease-in, opacity 800ms ease-in";
    const match = /translate\(([-\d.]+)px, ([-\d.]+)px\)/.exec(pointer.style.transform);
    const y = match ? Number(match[2]) - 30 : 100;
    pointer.style.transform = `translate(${window.innerWidth + 60}px, ${y}px) rotate(-18deg)`;
    pointer.style.opacity = "0";
    await pause(850);
}

/** Hides the real elements the replicas stand for: "lists", "timer" or "scope". */
export function setDemoMode(name, on) {
    root.classList.toggle(`onboarding-demo-${name}`, on);
}

/** Lays a replica over the real element (same position and width, and the height if asked) and shows it. */
export function placeOver(element, selector, matchHeight) {
    const target = document.querySelector(selector);
    if (!target) {
        return false;
    }

    const rect = target.getBoundingClientRect();
    element.style.left = `${rect.left}px`;
    element.style.top = `${rect.top}px`;
    element.style.width = `${rect.width}px`;
    if (matchHeight) {
        element.style.height = `${rect.height}px`;
    }

    element.style.visibility = "visible";
    return true;
}

/** Takes the replicas away: the real elements show again and the pointer is gone. */
export function resetDemo(pointer) {
    for (const name of ["lists", "timer", "scope"]) {
        root.classList.remove(`onboarding-demo-${name}`);
    }

    if (pointer) {
        pointer.style.opacity = "0";
    }

    document.querySelectorAll(".stage-ripple").forEach(ripple => ripple.remove());
    releaseRing();
}

/** Gives the app back: removes every class, lock and hiding that the scene set. */
export function teardown() {
    docked = false;
    dockedPlant = null;
    dockedFloor = null;
    window.removeEventListener("resize", onResize);
    lockApp(false);
    pressMenu(false);
    setBlur(false);
    resetDemo(null);
    root.classList.remove("onboarding-stage");
}
