# Moth

[Home Page](https://stellarwitch7.github.io)

# About [![Build](https://github.com/StellarWitch7/Moth/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/StellarWitch7/Moth/actions/workflows/build-and-test.yml)

Moth's official compiler, written in C#. It takes Moth code and converts it to LLVM IR, which is then passed to a C compiler. Currently only compatible with the Clang compiler. Read the [wiki](https://github.com/StellarWitch7/Moth/wiki) for Moth's documentation. Please report any bugs to the [issue tracker](https://github.com/StellarWitch7/Moth/issues), as it helps to improve Moth's compiler. 

### Arguments
```
-v, --verbose => Logs extra info to console. 
-o, --output => The name of the output file. Please forego the extension.
-i, --input => The files to compile. 
```

# !!WARNING!!
Moth is currently **unfinished**. Many things are unlikely to function well, and for the time being, only Windows is supported. 
