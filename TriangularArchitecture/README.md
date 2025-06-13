# Triangular Architecture

In the below diagram, an arrow means "depends on" or "uses".

```
Application ──▶ Persistence
      └──▶ Domain ◀──┘
```