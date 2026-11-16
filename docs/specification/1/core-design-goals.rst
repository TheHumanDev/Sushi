1.1 Core Design Goals
=============================================

What Sushi Is
--------------
Sushi is intended to be a native, strongly-typed systems programming language with compile-time memory management
with zero-cost abstractions.

Safety
-------
Sushi enforces compile-time memory and concurrency safety.
Memory is enforced as much as possible by the compiler,
in order to make runtime memory footprint and performance
as small as possible. Sushi should never allow compilation
of memory unsafe code. Resource ownership should always
remain linear.
