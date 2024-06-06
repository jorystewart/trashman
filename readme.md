# TrashMan is a cross-platform CLI tool for managing the Recycle Bin on Windows and Trash on Linux.

`trashman delete` moves files and/or directories to Recycle Bin/Trash.
It accepts both absolute and relative paths and supports globbing.
It is recursive by default.

`trashman restore` restores files and/or directories from Recycle Bin/Trash to their original locations.
It supports the wildcard character `*`

`trashman list` lists all files and directories currently in Recycle Bin/Trash.

`trashman purge` permanently deletes files and/or directories in Recycle Bin/Trash. 
It supports the wildcard character `*`

`trashman empty` permanently deletes all files and directories in Recycle Bin/Trash.




Todo:

* Windows implementation:
  * Tab completion for trasher restore and trasher purge  

* Linux implementation:
  * implement restore and purge methods in Trash.cs for Linux implementation
  * Exception detection and handling
  * Tab completion

* General:
  * Write unit tests
  * GitHub Actions?