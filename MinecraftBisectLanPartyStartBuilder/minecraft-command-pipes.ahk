lines := [
	; LINES FROM OUTPUT GOES HERE!
]

global idx := 1

; Create GUI
myGui := Gui("+AlwaysOnTop", "Line Sender")
myGui.SetFont("s10", "Segoe UI")
myGui.Add("Text",, "F8 = Send Next")
btnNext := myGui.Add("Button", "w200", "Send Next")
btnReset := myGui.Add("Button", "x+m w90", "Reset")
myGui.Add("Text", "xm y+8", "Status:")
status := myGui.Add("Text", "x+m w260", "Ready")

btnNext.OnEvent("Click", (*) => SendNext())
btnReset.OnEvent("Click", (*) => ResetQueue())
myGui.Show("AutoSize")

; Hotkey to send next without clicking
F8::SendNext()

SendNext() {
    global idx, lines, status
    if (idx > lines.Length) {
        status.Text := "All lines sent."
        SoundBeep(1000, 100)
        return
    }

    text := lines[idx]
    idx++

    status.Text := "Sending: " . text

    ; Send as normal input + Enter
    SetKeyDelay(50, 50)   ; gentle pacing (50 ms down, 50 ms up)
    SendInput("{Text}" . text)
    Sleep(100)            ; short pause
    SendInput("{Enter}")
}

ResetQueue() {
    global idx, status
    idx := 1
    status.Text := "Queue reset."
    SoundBeep(800, 80)
}
