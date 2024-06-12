# Moth

[Home Page](https://stellarwitch7.github.io)

### About [![Build](https://github.com/StellarWitch7/moth-lang/actions/workflows/build.yml/badge.svg)](https://github.com/StellarWitch7/moth-lang/actions/workflows/build.yml) [![Test](https://github.com/StellarWitch7/moth-lang/actions/workflows/test.yml/badge.svg)](https://github.com/StellarWitch7/moth-lang/actions/workflows/test.yml)

Moth's official compiler, written in C#. It takes Moth code and converts it to LLVM IR, which is then passed to the Clang C compiler. Currently only compatible with the Clang compiler. Read the [wiki](https://github.com/StellarWitch7/Moth/wiki) for Moth's documentation. Please report any bugs to the [issue tracker](https://github.com/StellarWitch7/Moth/issues), as it helps to improve Moth's compiler. 

### Dependencies
1. [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
2. [Clang 16](https://clang.llvm.org/get_started.html)
3. [Git](https://git-scm.com/downloads)
4. [Git Extras](https://github.com/tj/git-extras)

### Arguments

#### luna
```
Usage:
luna build [-v] [-n] [-c] [--no-advanced-ir-opt] [-p <path>] => Builds the project at the path provided or in the current directory if no project file is passed. 
luna run [-v] [-n] [-c] [--no-advanced-ir-opt] [-p <path>] [--run-args <args>] [--run-dir <path>] => Builds and runs the project at the path provided or in the current directory if no project file is passed. 
luna init [--lib] [--name <project-name>] => Initialises a new project in the current directory. 

-v, --verbose => Logs extra info to console. 
-d, --do-not-compress => Tell mothc to not compress embedded metadata. 
-n, --no-meta => Strips metadata from the output file. WARNING: disables reflection! 
-c, --clear-cache => Whether to clear dependency cache prior to build. 
--no-advanced-ir-opt => Whether to skip IR optimization passes. 
-p, --project => The project file to use. 
--name => When initializing a new project, pass this option with the name to use. 
--lib => When initializing a new project, pass this option to create a static library instead of an executable project. 
--run-args => When running a project, pass this option with the arguments to use. 
--run-dir => When running a project, pass this option with the working directory to use. 
```

#### mothc
```
Usage:
mothc [-v] [-n] [--no-advanced-ir-opt] [--moth-libs <paths>] [--c-libs <paths>] -t exe|lib -o <output-name> -i <paths>
-v, --verbose => Logs extra info to console. 
-n, --no-meta => Strips metadata from the output file. WARNING: disables reflection! 
--no-advanced-ir-opt => Whether to skip IR optimization passes. 
-t, --output-type => The type of file to output. Options are "exe" and "lib". 
-o, --output => The name of the output file. Please forego the extension. 
-V, --module-version => The version of the compiled module. 
-i, --input => The files to compile. 
-m, --moth-libs => External Moth library files to include in the compiled program. 
-c, --c-libs => External C library files to include in the compiled program. 
-e, --export-for => Languages to @Export functions for. Use the file extension for the language. 
-g, --compression-level => The type of compression to use for mothlib embedded metadata. Only really matters for huge projects. Options are: "none", "low", "mid", and "high". 
```

### Hello World
The following is a minimal example that prints "Hello World!" to the terminal. 
```
// HelloWorld.moth
namespace hello; // all files must belong to a namespace

with core; // we are using the core namespace in this file, so we declare our usage here

// the main function is the entrypoint of an executable program
// it must return a value of type i32
fn main() #i32 {
    WriteLine("Hello World!"); // we use the core namespace here to call the WriteLine(#u8*) function
    // we end the program by returning from the main function with an exit code of zero
    ret 0 // note that unlike other lines, return lines do not have semicolons
}
```
To learn how to run this code, [continue reading](https://github.com/StellarWitch7/moth-lang/wiki/Hello-World). 

### Tools
Currently the only aid for coding in Moth is the official [VS Code extension](https://github.com/StellarWitch7/moth-dev). It serves only to provide syntax highlighting. 

# !!WARNING!!
Moth is currently **unfinished**. Many things are unlikely to function well. 
