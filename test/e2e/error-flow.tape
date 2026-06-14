# End-to-end recording of an error path: `skillz add` on a missing local source.
#
# Validates the failure UX end to end — the logo, the resolved Source line, the
# error message (written to stdout via the interaction service), and the non-zero
# exit code (asserted by echoing $? into the recording). Hermetic: the path simply
# does not exist. Produces error-flow.gif + error-flow.txt.

Output error-flow.gif
Output error-flow.txt

Set Shell bash
Set FontSize 18
Set Width 1100
Set Height 850
Set Padding 14
Set TypingSpeed 50ms
Set CursorBlink false

# --- setup (hidden) ---
Hide
Type `export PS1="$ "` Enter
Type `export PATH=/src/test/e2e/bin:$PATH` Enter
Type `cd /tmp && rm -rf work && mkdir work && cd work` Enter
Type `clear` Enter
Show

# --- the demo ---
Type `skillz add ./missing` Enter

# Assert the failure UX *and* the non-zero exit: echo $? straight after the
# command (no command runs in between, so $? is still skillz's) so the golden
# pins "exit: 1". A regression that prints the error but exits 0 would change
# this line — caught by the diff, and by the marker ([skillz exit: 1]) too.
Wait+Screen@30s /Local path does not exist/
Sleep 800ms
Type `echo "[skillz exit: $?]"` Enter
Wait+Screen@30s /skillz exit: 1/
Sleep 1000ms
Screenshot error-flow-final.png
Sleep 500ms
