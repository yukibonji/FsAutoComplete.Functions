# FSAC (F# xPlat lang server) running in Azure Functions

It's using simplified version of FSAC (stateless, supporting only script files) hosted inside of (pre-compiled) Azure Function

### How to run

1. Clone
2. `build.cmd` / `build.sh`
3. Start using Functions CLI `func host start --cors http://localhost:8888` (setting CORS is important)