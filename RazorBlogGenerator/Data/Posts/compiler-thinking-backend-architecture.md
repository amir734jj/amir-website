# Compiler thinking in production code

A compiler engineer naturally asks:

- What is the syntax and semantics of this API?
- What invariants can be checked at compile-time?
- Where can we fail fast?

![Compiler pipeline](/assets/images/posts/compiler-pipeline.svg)

In distributed systems, this mindset reduces entire classes of runtime incidents.
