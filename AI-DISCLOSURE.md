#  AI Disclosure

This mod and apworld were built with heavy AI pair-programming.

- Design: all ideas, game design (including logic), code structure and feature direction are mine (some also come from friends and people on this server now).
- Code and reverse engineering: the AI helps decompile and analyse the game’s internals, and writes the code under my direction, one feature at a time. I review and validate every piece of code it generates before even testing it on my machine. I remain responsible for ensuring that the final implementation is correct and maintainable.
- Automated tests: AI writes build-pipeline tests, which check, for instance, that every change still generates a working seed. It is also reviewed by me. I also manually run fuzzing and multiworld generation scenarios to ensure quality of each release.
- In-game testing: done entirely by me before each release (since 1.1.0 (before I was targeting only obvious changes and things that matter for multiplayer purposes)), on several game setups and seeds. Community testers now help after release. Nothing ships without me testing it in-game first.
- Docs: I typically write a first pass myself and use AI to rephrase (code comments are hinted by AI, and I confirm it afterwards), help on translation, add detail and check for inconsistencies.

I initially started this project for myself, partly as an experiment with AI tools (as a computer science graduate). I still rely on AI, and will keep doing so, mainly for speed, and for areas where it would be too time-consuming (decompilation, reverse engineering and assembly code). However, I remain responsible for every aspect of the project, including security, user experience and developer experience.

No AI-generated artwork will be included in the project.
