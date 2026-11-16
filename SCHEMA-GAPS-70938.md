# What the SQL output covers vs the tc-forever world schema (sniff 70938)

Compared: `out/forever_70938.sql` against `forevercore/sql/base/dev/world_database.sql` (246 tables) and the packets in the sniff.

## Written to SQL (65 tables, all with 0 bad rows)
creature, creature_addon, creature_template(+_addon, +_difficulty, +_gossip, +_model), creature_equip_template, creature_model_info,
creature_loot_template, creature_queststarter, gameobject(+_addon, +_template, +_questitem), gossip_menu(+_option), npc_text,
quest_template, quest_details, quest_poi(+_points), waypoint_path(+_node), areatrigger_template, spawn_tracking_template,
plus hotfix and DB2 tables (spell_*, item*, map, area_table, broadcast_text, taxi_nodes, ...).
Extra, not in the schema: `creature_classic_level`, `sniff_data`, `quest_v2`, `object_names` (helper tables of this parser).

## Gaps: data is in the sniff, but nothing is written
| Table | In the sniff | Status |
|---|---|---|
| `trainer`, `trainer_spell` | 28 `SMSG_TRAINER_LIST` packets | **Parser reads them but does not store them** (comment in `ForeverTrainerHandler.cs`). The schema needs `trainer(Id, Type, Greeting)` and `trainer_spell(TrainerId, SpellId, MoneyCost, ReqSkillLine, ReqSkillRank, ReqLevel, ...)`; classes `Trainer`/`TrainerSpell` already exist. **Fixable.** |
| `creature_text` | 16 `SMSG_CHAT` packets | not written (needs the NPC text type mapping). Fixable, low volume. |
| `quest_offer_reward`, `quest_request_items` | 0 packets in this sniff | no data; only the 70862 sniff has some. |

## No data in this sniff (not parser gaps)
`npc_vendor` (no `SMSG_VENDOR_INVENTORY`), `page_text`, `points_of_interest` (no `SMSG_GOSSIP_POI`), `gameobject_*_loot_template`, `item_loot_template`,
`pickpocketing/skinning_loot_template` (skipped by design), `creature_questender`, `gameobject_queststarter/questender`, `linked_respawn`, `smart_scripts`, `conditions`.
Most of these cannot be derived from a sniff at all (scripts, conditions, links).

## Not implemented
`creature_classic_level_spawn`.

## Recommendation
1. Store trainers (`trainer`, `trainer_spell`): the data is already parsed. Smallest useful next step.
2. Then `creature_text` from `SMSG_CHAT` of NPCs.
3. In the next capture visit vendors, trainers, quest givers (offer reward / request items), signposts with POIs, and read books.

## Check against TDB (TDB_full_forever_2026_10_14)
| Table | Rows in TDB |
|---|---|
| `trainer` | 708 |
| `trainer_spell` | ~34 700 |
| `creature_text` | ~21 250 |

- The sniff has 7 trainer ids (4944, 4946, 4948, 5008, 5289, 5529, 5530). **None of them is in TDB's `trainer` table** (TDB has 174 ids below 1000 and the rest above 1 000 000; nothing in 4900-6000).
- So the sniff would add 7 trainers that TDB does not have. They all come from one capture, so this is a small gain: 7 trainers.
- `creature_text`: TDB already has ~21 000 rows; the sniff has only 16 chat packets, so the gain is negligible.
