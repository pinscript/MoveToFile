# MoveToFile
MoveToFile is a Visual Studio extension that offers a code fix to move classes/types to a file corresponding to its name. It's available for all types.

The refactoring is shown for types where the current file does not match its name. For example:

![Visual Studio Move class to File](http://i.imgur.com/jwR9We6.png)

*Note that the original document is removed if it contains no types after the refactor (essentially a rename).*

## Todo

- [ ] Remove unused usings from both files.
- [ ] Allow a new file name to be specified manually (is this even possible?)
