// unity-dialog-ax PID              the dialogs and sheets a process (the Unity Editor) shows: title, text, buttons
// unity-dialog-ax PID press NAME   press one of their buttons
//
// Built once by unity-dialog into ~/.diceload/Unity Dialog.app and launched as an app of its own, so it
// alone is allowed in System Settings ▸ Privacy & Security ▸ Accessibility — not a terminal or Claude. Reads windows
// through the macOS Accessibility API; it doesn't send keys or touch any other app.

import ApplicationServices
import Foundation

let args = CommandLine.arguments
guard args.count >= 2, let pid = pid_t(args[1]) else {
    print("usage: unity-dialog-ax PID [press NAME]")
    exit(64)
}

if !AXIsProcessTrusted() {
    // Shows macOS's own prompt, which adds this binary to the Accessibility list.
    let prompt = [kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: true] as CFDictionary
    _ = AXIsProcessTrustedWithOptions(prompt)
    print("not allowed: System Settings ▸ Privacy & Security ▸ Accessibility ▸ Unity Dialog")
    exit(5)
}

func value(_ e: AXUIElement, _ name: String) -> AnyObject? {
    var v: AnyObject?
    return AXUIElementCopyAttributeValue(e, name as CFString, &v) == .success ? v : nil
}

func text(_ e: AXUIElement, _ name: String) -> String { (value(e, name) as? String) ?? "" }

func children(_ e: AXUIElement) -> [AXUIElement] { (value(e, kAXChildrenAttribute) as? [AXUIElement]) ?? [] }

/// Texts and buttons anywhere inside an element (a few levels deep: an alert, a save panel).
func contents(_ e: AXUIElement, depth: Int = 0, texts: inout [String], buttons: inout [(String, AXUIElement)]) {
    if depth > 8 { return }
    for c in children(e) {
        switch text(c, kAXRoleAttribute) {
        case kAXStaticTextRole:
            let v = text(c, kAXValueAttribute)
            if !v.isEmpty { texts.append(v) }
        case kAXButtonRole:
            let t = text(c, kAXTitleAttribute)
            if !t.isEmpty { buttons.append((t, c)) }
        default: break
        }
        contents(c, depth: depth + 1, texts: &texts, buttons: &buttons)
    }
}

let app = AXUIElementCreateApplication(pid)
var dialogs: [(kind: String, element: AXUIElement)] = []
for w in (value(app, kAXWindowsAttribute) as? [AXUIElement]) ?? [] {
    let subrole = text(w, kAXSubroleAttribute)
    if subrole != kAXStandardWindowSubrole && subrole != kAXFloatingWindowSubrole { dialogs.append(("dialog", w)) }
    for c in children(w) where text(c, kAXRoleAttribute) == kAXSheetRole { dialogs.append(("sheet", c)) }
}

if dialogs.isEmpty {
    print("no dialog: the Editor isn't waiting on one (it may be busy compiling or importing — wait, then try again)")
    exit(0)
}

let pressing = args.count >= 4 && args[2] == "press" ? args[3] : nil
var report = ""
for (kind, d) in dialogs {
    var texts: [String] = []
    var buttons: [(String, AXUIElement)] = []
    contents(d, texts: &texts, buttons: &buttons)
    if let name = pressing, let b = buttons.first(where: { $0.0 == name }) {
        let result = AXUIElementPerformAction(b.1, kAXPressAction as CFString)
        print(result == .success ? "pressed \"\(name)\"" : "couldn't press \"\(name)\" (\(result.rawValue))")
        exit(result == .success ? 0 : 1)
    }
    report += "\(kind): \(text(d, kAXTitleAttribute))\n"
    for t in texts { report += "  text: \(t)\n" }
    report += "  buttons: " + buttons.map { "\"\($0.0)\"" }.joined(separator: " ") + "\n"
}
if let name = pressing { print("no button \"\(name)\" — the dialogs are:") }
print(report, terminator: "")
exit(pressing == nil ? 0 : 1)
