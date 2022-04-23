# Test application for city bank

## Known issues

When a duplicated file is encounter the application may break/act weird, no
specific way on how to handle was provided

if the application is stopped it will process the file from the beginning, 
that shouldn't be to hard to address, since it can already start to process 
a file given a row number, headers dont count, the second issue is that we
enumerating the files in memory, this is bad but no time to implement some
a function that process the file in a temp storage (a file), again it 
shouldn't be that hard to implement just enumerate and write into a file, 
in a temp folder, that would also help fix the first issue, since we can 
read those file an enqueue them with the starting pos being the number of rows
on the temp file

The application is written in a kinda functional way, i do have some curried functions
since i was playing with observables

** The "That shouldn't be that hard to address" is in theory, in practice that could
be a pain to implement but those were the solutions i came up with,

## Improvement

We can encapsulate some functionality in some services and maka the app more "imperative"
also the Either monad implementation is a bit vague but right is error and left is success
we could implement some call backs or something else to make it a bit more intuitive,

also the scheduler is pretty basic, process files, wait a minute, repeat

## Config
A root path to the folder is needed, i left an example on dev app settings