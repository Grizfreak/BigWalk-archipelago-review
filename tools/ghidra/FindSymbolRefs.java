// The symbols whose name holds the argument, and the functions that refer to them.
import ghidra.app.script.GhidraScript;
import ghidra.program.model.listing.Function;
import ghidra.program.model.symbol.Reference;
import ghidra.program.model.symbol.Symbol;
import ghidra.program.model.symbol.SymbolIterator;
import ghidra.util.task.ConsoleTaskMonitor;

import java.util.LinkedHashSet;
import java.util.Set;

public class FindSymbolRefs extends GhidraScript {
    @Override
    public void run() throws Exception {
        String[] args = getScriptArgs();
        if (args.length == 0) {
            println("Usage: FindSymbolRefs <fragment de nom de symbole>");
            return;
        }

        String needle = args[0].toLowerCase();
        ConsoleTaskMonitor monitor = new ConsoleTaskMonitor();
        SymbolIterator it = currentProgram.getSymbolTable().getAllSymbols(true);
        int symbols = 0;

        while (it.hasNext() && !monitor.isCancelled()) {
            Symbol symbol = it.next();
            if (!symbol.getName().toLowerCase().contains(needle)) {
                continue;
            }
            if (symbol.getSymbolType().toString().equals("Function")) {
                continue;
            }

            symbols++;
            println("=== SYMBOLE " + symbol.getName() + " @ " + symbol.getAddress());
            Set<String> seen = new LinkedHashSet<>();
            for (Reference ref : getReferencesTo(symbol.getAddress())) {
                Function f = getFunctionContaining(ref.getFromAddress());
                String line = f != null ? f.getName() + " @ " + f.getEntryPoint() : "<hors fonction> @ " + ref.getFromAddress();
                if (seen.add(line)) {
                    println("      <- " + line);
                }
            }
        }

        println("=== TOTAL: " + symbols + " symbole(s) correspondant a '" + args[0] + "'");
    }
}
