# Forever (1.60.1) parser notes — WPP-full

Documentation for the WoW "Forever" client 1.60.1 support in this fork.
Reference build: **70938**. Capture: `dump_1.60.1.70938_2026-11-14_12-41-07.pkt`
(119,594 packets, Orgrimmar, 8.5 min).

| file | language | what it is |
|---|---|---|
| [`TODO-70938.md`](TODO-70938.md) | EN | What is still broken and what to do next. Start here. |
| [`PARSER-ERRORS-70938.md`](PARSER-ERRORS-70938.md) | EN | Current state: 0 exceptions, 44 unread tails, why each one is blocked or fixable. |
| [`BLEDY-70938.md`](BLEDY-70938.md) | PL | Full per-packet error dump (Polish). Historical, before the Karrz1 patches. |
| [`PARSER-ERRORS-70938-prefix.md`](PARSER-ERRORS-70938-prefix.md) | EN | Same report as it stood before patches 0001–0005 (460 errors). Kept for comparison. |

Session notes and the wire-layout reverse-engineering log live in
[`../FOREVER-STAN.md`](../FOREVER-STAN.md).

## Current result

| bucket | packets | % |
|---|---|---|
| OK | 117,307 | 98.088 % |
| errors | 0 | 0.000 % |
| skipped (no handler) | 1,181 | 0.988 % |
| without structure | 1,046 | 0.875 % |

100.000 % of the packets the parser actually attempts.

## Reproduce

```
dotnet build WowPacketParser.slnx -c Release -v q --nologo
cd WowPacketParser\bin\Release
dotnet WowPacketParser.dll --ConfigFile WowPacketParser.dll.config ^
  "C:\Users\devov\Desktop\regedit_70790\dumps\dump_1.60.1.70938_2026-11-14_12-41-07.pkt"
```

## Note on data

These files contain packet layouts and opcode statistics only. No raw capture
bytes, player names, account data or server addresses are included.
