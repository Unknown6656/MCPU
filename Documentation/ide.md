# IDE (Integrated Development Environment) Manual

The MCPU IDE is a lightweight, multilingual integrated development environment for the MCPU Assembly language.

(((TODO)))

### User Interface

This is the IDE's main window, which will be presented to the user upon launching the application:
<br/>
![][ide01]
<br/>
As can be seen in the image, the IDE's main component is the central editor window with its bottom information status bar, and the top menu bar. The section on the right-hand side is a scrollable code-map, which provides an overview of the entire code file.

The IDE's settings window can be accessed by pressing the key <kbd>F10</kbd>:
<br/>
![][ide02]
<br/>
It provides a selection of (internally stored) language files and various compiler and processor options.

### Code Editor

The IDE's main editor is rich in features. It does include a complete set of syntax-highlighting, auto-completion, hinting and refractoring, along side the "usual" text editor features.
Many keyboard shortcuts are available to the developer, e.g.:

Key | Function
:--:|----------
<kbd>Ctrl</kbd>+<kbd>Z</kbd> | Undo
<kbd>Ctrl</kbd>+<kbd>Y</kbd> | Redo
<kbd>Ctrl</kbd>+<kbd>C</kbd> | Copy
<kbd>Ctrl</kbd>+<kbd>X</kbd> | Cut
<kbd>Ctrl</kbd>+<kbd>V</kbd> | Paste
<kbd>Ctrl</kbd>+<kbd>A</kbd> | Select All
<kbd>Ctrl</kbd>+<kbd>F</kbd> | Search
<kbd>Ctrl</kbd>+<kbd>H</kbd> | Replace
<kbd>Ctrl</kbd>+<kbd>B</kbd> | Create Bookmark
<kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>B</kbd> | Delete Bookmark
<kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>N</kbd> | Previous Bookmark
<kbd>Ctrl</kbd>+<kbd>N</kbd> | Next Bookmark
<kbd>Ctrl</kbd>+<kbd>+</kbd> | Zoom in (also possible using <kbd>Ctrl</kbd>+`scroll wheel`)
<kbd>Ctrl</kbd>+<kbd>-</kbd> | Zoom out (also possible using <kbd>Ctrl</kbd>+`scroll wheel`)
<kbd>Ctrl</kbd>+<kbd>0</kbd> | Reset Zoom
<kbd>Alt</kbd>+<kbd>F</kbd> | Fold all regions
<kbd>Alt</kbd>+<kbd>Shift</kbd>+<kbd>F</kbd> | Unfold all regions
etc.

The editor's autocompletion-menu can be accessed by pressing the keyboard combination <kbd>Ctrl</kbd>+<kbd>Space</kbd> or by inserting a space or line-break after any token:
<br/>
![][ide04]
<br/>
The context menu contains a list of all known instructions, defined functions or labels and a few other useful token. The selected item can be inserted by the press of the key <kbd>Tab</kbd> or <kbd>Enter</kbd>.  
The menu can be closed by pressing the <kbd>Esc</kbd>-key.

(((TODO)))

### Compiler

A compiler error will be shown an error message during the compilation processes and a tool tip, when hovering over the faulty line:
<br/>
![][ide05]
<br/>
If the developer has not yet pressed the `Compile`-button (or the key <kbd>F5</kbd>), only a warning will be shown:
<br/>
![][ide07]
<br/>

The MCPU compiler also has a build-in optimization algorithm, which can be activated inside the settings window. The optimizer removes all empty or unused instructions, labels and functions during compilation and indicates an expression which can be optimized as follows:
<br/>
![][ide06]
<br/>

(((TODO)))

### Debugger

![][ide03]

(((TODO)))




[ide01]: ./ide-01.png "IDE Screenshot"
[ide02]: ./ide-02.png "IDE Screenshot"
[ide03]: ./ide-03.png "IDE Screenshot"
[ide04]: ./ide-04.png "IDE Screenshot"
[ide05]: ./ide-05.png "IDE Screenshot"
[ide06]: ./ide-06.png "IDE Screenshot"
[ide07]: ./ide-07.png "IDE Screenshot"
[ide08]: ./ide-08.png "IDE Screenshot"
[ide09]: ./ide-09.png "IDE Screenshot"