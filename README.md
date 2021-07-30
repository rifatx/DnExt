# A managed WinDbg extension for .NET debugging and dump analysis

## Implemented functions

### Clr
- `!clrversions`: List CLR versions
- `!setclrversion`: Set active CLR version for current session

### Modules
- `!getmodules`: List and save loaded modules
- `!savemanagedmodule`: Save a single module

### Managed Heap
- `!heapstat`: Prints simple statistics about objects on heap

### System.DataSet
- `!dumpdataset`: Dump the DataSet at given address to list its tables 
- `!dumpdatatable`:  Dump the DataTable at given address

## Manual
You can run `!command -?` to see detailed help for the given `command`.


