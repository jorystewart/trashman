# Trashman is a cross-platform CLI tool for managing the Recycle Bin on Windows and Trash on Linux.

---
Trashman requires the .NET 8 runtime, but can be compiled into a standalone executable instead. 

Trashman conforms to the FreeDesktop trash standard, found here: https://specifications.freedesktop.org/trash-spec/trashspec-latest.html

---

`trashman delete` moves files and/or directories to Recycle Bin/Trash.
It accepts both absolute and relative paths and supports globbing.
It is recursive by default.

`trashman restore` restores files and/or directories from Recycle Bin/Trash to their original locations.
It supports the wildcard character `*`

`trashman list` lists all files and directories currently in Recycle Bin/Trash.

`trashman purge` permanently deletes files and/or directories in Recycle Bin/Trash. 
It supports the wildcard character `*`

`trashman empty` permanently deletes all files and directories in Recycle Bin/Trash.

---

<details>
<summary>Todo</summary>


* Windows implementation:
  - [ ] Tab completion for trasher restore and trasher purge  

* Linux implementation:
  - [ ]  Exception detection and handling
  - [ ]  Tab completion

* General:
  - [ ]  Write unit tests
  - [ ]  GitHub Actions?


</details>