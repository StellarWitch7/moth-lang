# Moth

[Home Page](https://stellarwitch7.github.io)

### About [![Build](https://github.com/StellarWitch7/moth-lang/actions/workflows/build.yml/badge.svg)](https://github.com/StellarWitch7/moth-lang/actions/workflows/build.yml) [![Test](https://github.com/StellarWitch7/moth-lang/actions/workflows/test.yml/badge.svg)](https://github.com/StellarWitch7/moth-lang/actions/workflows/test.yml)

Moth's official compiler, written in C#. It takes Moth code and converts it to LLVM IR, which is then passed to the Clang C compiler. Currently only compatible with the Clang compiler. Read the [wiki](https://github.com/StellarWitch7/Moth/wiki) for Moth's documentation. Please report any bugs to the [issue tracker](https://github.com/StellarWitch7/Moth/issues), as it helps to improve Moth's compiler. 

### Dependencies
1. The Clang compiler. (Tested with version 16.)

### Arguments
```
-v, --verbose => Logs extra info to console. 
-o, --output => The name of the output file. Please forego the extension.
-i, --input => The files to compile. 
```

### Tools
Currently the only aid for coding in Moth is the official [VS Code extension](https://github.com/StellarWitch7/moth-dev). It serves only to provide syntax highlighting. 

# !!WARNING!!
Moth is currently **unfinished**. Many things are unlikely to function well. 
