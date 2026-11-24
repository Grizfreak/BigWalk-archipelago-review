# tools/ghidra/

Ghidra scripts that answer a question about the game binary. They need an analysed Ghidra project of
`GameAssembly.dll`, which is not in this repository. They are in Java because they run without PyGhidra.

- `DecompileAt.java <address>...`: decompiles the functions at these addresses.
- `FindCallers.java <name fragment>`: the functions whose name holds it, and what calls each.
- `FindSymbolRefs.java <name fragment>`: what refers to the symbols whose name holds it.

```powershell
& "<ghidra>\support\analyzeHeadless.bat" "<project folder>" "Big Walk" `
     -process "GameAssembly.dll" -noanalysis -readOnly -scriptPath "<repo>\tools\ghidra" `
     -postScript FindCallers.java "HeldInformation"
```

Functions are named `Class_Method`. IL2CPP dispatches indirectly, so the caller lists are often empty.

The binary analysed must be the one being played: addresses and code differ between builds.
