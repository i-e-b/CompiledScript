# Compiled Script
Parse and compile a prefix expression (source code) to a simple byte code.

## TODO

* [x] Add custom subroutines
* [x] Add value returns (I want to catch failure to return early, without having to define too much)
* [x] More inequality/comparisons (`<`, `>` `<>` etc)
* [x] Add rough language specs to readme
* [x] Import files (in compiler, compile and include generated, like functions. Have an include chain, warn and skip if file has been seen before)
* [x] Uniary minus (negation), list math.
* [x] List inequality (like `<(0 get(x) 10)` for 'x is between 1 and 9')
* [x] Assert function
* [x] Switch-like statement (for if-else chains)
* [x] Treat `variableName` as `get(variableName)`

* [x] Split into library and apps
* [x] Add a REPL.

* [x] Change assm definition to be less stringy
      - Tagged basic type (NaN-tagging)
      - All non-numbers are pointers to type

* [x] Underscores in numbers
* [x] String interning / deduplication (compiler)
* [ ] Remove hash->string->switch step in functions
* [ ] Circular buffer in the repl console (steal from WebJony?)
* [ ] Single step in repl gui
* [ ] Runtime interpreter exceptions to give position

* [ ] Basic types
* [ ] Memory model (heap, arrays)
* [ ] Built-ins for hash-tables, 1D arrays, 2D arrays.
* [ ] Move value definitions into memory

* [ ] Reference counting / basic GC / etc (follow scope? Maybe copy-arenas for simplicity)
* [ ] String interning / deduplication (runtime)

* [ ] Skip and EndWhile for loops (like `continue` and `break` in C)
* [ ] More math: list support, abs, sign, pow, floor, ceil, round, random, sin, cos, tan, ...etc
* [ ] More math: useful constants (pi, e, inf, neg-inf)
* [ ] Map files for byte code, to help with error messages and diagnosis.
* [ ] Make sure (de)serialisation is available
* [ ] Object-literal form? Functions-as-data, or special syntax?
* [ ] Built-ins to handle variadic functions (expose naming? Limit '__' prefixed names to local scope only?)
* [ ] Infix blocks? (like `set(x [y + (4 * z)])`?)
* [ ] Iterators/Generators?

### Data types

* String
* Int
* Float/Number
* Set/flags
* Dictionary/map/hashtable
* Array (of type)
* List (heterogenous)
* Grid (2+D array, sparse)

### Ideas

Extend get/set over many types? Like `set(myDict "keyStr" "valueStr")` and `get(myGrid 3 5)`

# Language definitions

## Core language

A lisp-look-alike language. Atoms are treated as variable reads, except in the direct parameters of `get`, `set`, `isset`, `unset`.
Delimited strings are treated as a single value.
Values go on the value-stack. Variables and functions are case sensitive.
Function and variable names can contain any non-whitespace characters, but must not start with numbers.

Strings are quoted with either `"` or `'`. String escapes with `\`

Whitespace is significant as an atom separator, but all whitespace is treated equally (newlines, tabs, any number of spaces).
The `,` character is considered whitespace.
Everything else is function calls. Function calls start with the function name, then have parenthesis surrounding parameters.
Function calls may be parameters.

Language built-in functions are tried first, then custom defined functions.
The `def` function defines custom functions

### Keywords:

`true`, `false` (technically these are just string atoms at the moment)

### Reserved functions

```
=    equals    >    <    <>    not-equal
assert    random    eval    call    not    or
and    readkey    readline    print    substring
length    replace    concat    return    +    -
*    /    %
()
```

### get (1 param)
Get the value of a variable by name. If a variable is used *outside* of the params of a `get` or `set`, it is treated as a get.
```javascript
print("My value is " get(val))
print("My value is "     val )  // this is the same as above

set(dst get(src)) // copy src into dst (can't use implicit get here)
```

### set (2 params)
Set the value of a variable
```javascript
set(myVar "hello")
set(var2 "world")
print(myVar ", " var2) // "hello, world"
```

### `=` or `equals` (2+ params)
Returns true if values are all the same

### `<>` (2+ params)
Returns true if each value is different to the last
```javascript
<>("Hello"  "world") // true
<>(1 1 1)            // false
<>(0 1 0)            // true
```

### `>` (2+ params)
Returns true if each number is less than the one before
```javascript
>(100 0)  // true
>(3 2 1)  // true -- like the infix  3 > 2 > 1
>(50 x 0) // is 'x' between 49 and 1
```

### `<` (2+ params)
Returns true if each number is bigger than the one before

### `isset` (1 param)
Returns true if the name has a value assigned

### `unset` (1 param)
Remove a name definition

```javascript
print(isset(x)) // false
set(x false)
print(isset(x)) // true
unset(x)
print(isset(x)) // false
```

### `def` (2 params)
Define a subroutine / custom function. First param is the new function's parameter list (may be empty). Second param is the function definition.
```javascript
def (
    timesTwo (x) (
        return( *(2 x) )
    )
)
def (
    meaning? () (
        print("42")
    )
)

print(timesTwo(4)) // "8"
meaning?()         // "42"
```

### `import` (1 param)
Includes another source file at compile time.
This function can only be called from the root level of files (not inside functions, parameters or evals).
Recursive imports will be ignored.
Root level statements will be run, and defined functions will be available after the import call.
```javascript
import("./my-other-file.ecs")

OtherFileFunction("hello")
```

### `while` (1+ params)
Loops through a set of calls while the first parameter returns true
```javascript
set(i 10)
while ( not(=(i 0))
    print(i)
    set(i -(i 1))
) // 10 9 8 7 6 5 4 3 2 1
```

### `if` (1+ params)
Run a set of calls once if the first parameter returns true
```javascript
set(i 10)
while ( not(=(i 0))
    if ( %(i 2)
        print(i)
    )
    set(i -(i 1))
) // 9 7 5 3 1
```

### `pick` (special)
Run the first `if` statement that matches, ignore all the others
```javascript
pick (
    if ( a
        print("A was truthy")
    )

    if ( b
        print("B was truthy, and A was not")
    )

    if ( true // this is how to do a 'default' case
        print("Neither A nor B were truthy")
    )
)
```
This gets converted to a new function definition by the compiler, so `return` will only escape from the `pick` and not any surrounding function.

### `not` (1 param)
Invert boolean. Given 0, "0", "false" or false, returns true. Otherwise returns true.

### `or` (0+ params)
Returns true if any of the parameters are not false or zero.

### `and` (0+ params)
Returns true only if all the parameters are not false or zero.

### `readkey` (0 params)
Pause and wait for a key to be pressed. Returns a string representation of the character pressed

### `readline` (0 params)
Pause and wait for `return` key to be pressed. Returns a string of everything typed until enter.

### `print` (0+ params)
Write all params to the console, then start a new line. If the last param is empty, new line is suppressed

### `substring` (2 params)
Return a shortened string
```javascript
set(long "hello, world")
print(substring(long 7)) // "world"
```

### `substring` (3 params)
Return a shortened string
```javascript
set(long "hello, world")
print(substring(long 3 2)) // "lo"
```

### `length` (1 param)
Return number of characters in a string

### `replace` (3 params)
Replace substrings
```javascript
print(
    replace(
        "this is the source" // string to change
        "th"                 // substring to search for
        "d"                  // replacement string
        ) // "dis is de source"
)
```

### `concat` (0+ params)
Join strings together
```javascript
set(str
    concat(
        "hello"
        ", "
        "world"
        ) // "hello, world"
)
```

### `return` (0+ params)
Exit a subroutine and add values to the value stack.
If *any* path in a subroutine uses `return`, they all must.

### `+` (1 param)
Return a number as-is (uniary plus)

### `+` (2+ params)
Add numbers

### `-` (1 param)
Negate a number (uniary minus)

### `-` (2+ params)
Subtract numbers. If more than two params given, a running subtraction will be made (e.g. `-(12 2 4)` is equivalent to the infix: `(12 - 2) - 4`)

### `*` (2+ params)
Multiply numbers

### `/` (2+ params)
Divide numbers. If more than two params given, a running division will be made (e.g. `/(12 2 4)` is equivalent to the infix: `(12 / 2) / 4`)

### `%` (2+ params)
Get remainder from integer division

### `random` (0+ params)
Get a random value

* 0 params - any size
* 1 param  - max size
* 2 params - range

## Advanced built-in functions

### `eval` (1 param)
(Advanced) Compile and run a program string.
```javascript
eval("print('what')")
```
The evaluated program is run with a copy of current variable scope. Programs run in `eval` can read values, but `set` will have no effect
outside of the `eval`. The last value on the value-stack will be returned from the call to `eval`.

### `call` (1+ params)
(Advanced) Execute a function by name. Used to switch function by a variable.
```javascript
set(i "print")
call(i "Hello, world")
```


## Byte code

** THIS SECTION IS OUT OF DATE **
*The system now uses a NaN-tagging format*

The byte code is white-space delimited and string-based. Each operation has an op-code prefix, and potentially some values.
The runtime has a value-stack, from where parameters are pulled, and return values added.
There is also a return-stack for tracking function calls.
The byte code may be commented using line comments (`//`)
In the op-code descriptions, `[]` are used to make the parts obvious. They are not included in bytecode.

### `[v][value]`
Adds a value to the value-stack. Strings MUST be URL-encoded if they contain any whitespace, and may be URL-encoded in any case.
```
v0              // push a zero to the value-stack
vHello%20world  // push the string "Hello world" to the value-stack
```

### `[f][function name]  [param count]`
Call a function. The param count MUST be given, but can be zero.
Parameters will be popped from the value-stack in order (so the last pushed value will be the first parameter)
Any values returned by the function will be pushed to the value-stack
If this is a custom function (not reserved by the runtime) the exit position will be pushed to the return-stack.
```
vWorld         // push 'World' to value-stack
vHello%20      // push 'Hello ' to value-stack
fprint 2       // call 'print' with params ("Hello ", "World")
```

### `[ccmp]  [token count]`
Pops a value from the value-stack. Makes a relative jump **forward** if that value is zero or false.
Token count is the number of white-space delimited tokens in the byte code to skip.
```
. . .       // calculate some result
ccmp 1      // was the result true?
vWorld      // if it was true, put "World" on the value-stack
vHello%20   // in either case, put "Hello " on the value-stack
fprint 2    // print...
```

### `[cjmp]  [token count]`
Make an unconditional jump **backward**.
```
...         // loop body
ccmp 1      // if false, skip the jump-back and exit the loop
cjmp 10     // else jump back 10 tokens
...         // outside of loop
```

### `[cskip]  [token count]`
Make an unconditional jump **forward**.
```
cskip 50    // skip some binary or scratch data
... 50 chunks ...   
[next code]
```

### `[cret]`
Return from a function call. A absolute token position value is popped from the return-stack and program flow continues from there.
```
dmyProc 0 4         // define a function
vHello%20world      //     push a string value
fprint 2            //     print it
cret                // return from the function
...                 // rest of the code
```

### `[ct][function name]`
Runtime error: Function exit without value.
This is emitted by the compiler to trap functions where some but not all paths in a function return values
```
dmyFunc 2 8         // define a function
v__p0 mg            //     get first parameter
v__p1 mg            //     get second parameter
f%2B  2             //     add two numbers (result goes on stack)
freturn 1           //     return from function, taking top 1 stack values
ctmyFunc            // if flow gets here, the function was bad, and the runtime will error
```

### `[mg]` or `[mget]`
Memory get. Pops a name from the value-stack. Looks up a variable with that name, pushes its value to the value-stack
```
vi            // push "i"
mg            // find value of variable named "i"
fprint 1      // print the value of variable named "i"
```

### `[ms]` or `[mset]`
Memory set. Pops two values from the value-stack. First is the name of a variable, second is a value. That value is stored against the variable name
```
v123        // push 123
vx          // push "x"
ms          // set x = 123. Value-stack no longer contains either.
```

### `[mi]` or `[misset]`
Test varible scope. Pops a name from the value-stack.
If there is a variable in scope with that name,
`true` is pushed to the value-stack (regardless of the *actual* value of the variable)
```
v0 vx ms    // set x = 0
vx          // push "x"
mi          // is "x" set? Pushes "true"
vy          // push "y"
mi          // is "y" set? Pushes "false"
```

### `[mu]` or `[munset]`
Remove a variable from scope. Pops a name from the value-stack. If there is a variable in scope with that name, it is removed.
This only works for the most local scope, or the global scope.
```
v0 vx ms    // set x = 0
vx mi       // is "x" set? Pushes "true"
vx          // push "x"
mu          // unset "x"
vx mi       // is "x" set? Pushes "false"
```

### `[d][FunctionName]  [parameter count]  [skip length]`
Define a function.
Adds a function name to the function table, at the present location. Sets the expected number of parameters and the number of tokens of the function.
```
dmyFunc 2 8         // define a function. Expects 2 parameters (renamed as __p0 and __p1). Contains 8 opcodes (not including `cret` or `ct`)
v__p0 mg            //     get first parameter
v__p1 mg            //     get second parameter
f%2B  2             //     add two numbers (result goes on stack)
fprint 1            //     print the result
cret                // return from function.
```
