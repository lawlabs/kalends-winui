# Month drag correction

The previous compact preview used a fixed width of 210 logical pixels and offsets
of +12 / -35 pixels from the pointer. At 200% scaling, its rectangle was wider
than a Month event card and the original grab point was lost.

Compact previews now retain the source card's measured width and height and use
the pointer's offset within that card. An opaque surface prevents date labels
from showing through. A separate outline identifies the destination date, and
the overlay is clipped to the calendar. Adjacent cells use half-open hit bounds.

The date under the initial press anchors the move. Grabbing the second day of a
multi-day event therefore shifts the whole event relative to that day, preserving
the start time and duration. The release location is checked again; releasing
outside the grid cancels the move. Deferred redraws are applied after capture ends
without rebuilding cards after ordinary clicks.

## Validation

- 21 unit tests passed, including timed moves, later all-day segments, overnight
  events across a month boundary and same-day drops.
- Native UI checks at 200% scaling passed: moving to another week while preserving
  hours, releasing outside the grid, single click and double-click editing, and
  moving the second all-day segment to the intended date.
- Recorded frames were visually checked for card size, pointer anchoring, opaque
  backgrounds and the destination outline.
- Escape cancellation while holding the mouse was confirmed manually by the user.
  WinApp serializes separate mouse and keyboard commands, so a concurrent command
  attempt did not deliver Escape until after release and was not a valid test of
  cancellation during capture.
- LawMatic B2 and the standalone Windows calendar built with the updated library.

The checks used disposable demo events in LawMatic Calendar; no production tasks
were moved during testing.
