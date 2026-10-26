# Platform subsystem

The platform subsystem holds the cross-cutting capabilities that every Tutu Much
feature inherits: HTTP and API hardening, credential and supply-chain
protection, stateless scaling, the responsive and
accessible application shell, error reporting and observability,
availability and recovery, and the developer tool that resets and seeds a demo install. It refines `L1-013`, `L1-015`, `L1-016`, `L1-017`,
`L1-018` and `L1-019`. Each feature is a vertical slice through middleware, the Angular shell,
infrastructure and CI, and each has an estimate in
[`docs/estimates/platform/`](../../estimates/platform/).

| Feature | L2 requirements |
|---------|-----------------|
| [set-up-the-workspace](set-up-the-workspace/README.md) | `L2-071` |
| [seed-demo-data](seed-demo-data/README.md) | `L2-072`, `L2-073`, `L2-074`, `L2-075`, `L2-076`, `L2-077` |
| [harden-http-and-api](harden-http-and-api/README.md) | `L2-048`, `L2-049`, `L2-050`, `L2-051` |
| [protect-credentials-and-supply-chain](protect-credentials-and-supply-chain/README.md) | `L2-052`, `L2-053` |
| [meet-performance-and-scale](meet-performance-and-scale/README.md) | `L2-060` |
| [responsive-accessible-shell](responsive-accessible-shell/README.md) | `L2-061`, `L2-062`, `L2-063`, `L2-064`, `L2-065` |
| [report-errors-and-observe](report-errors-and-observe/README.md) | `L2-068`, `L2-069`, `L2-070` |
| [keep-available-and-recoverable](keep-available-and-recoverable/README.md) | `L2-066`, `L2-067` |
