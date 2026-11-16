using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.PacketStructures;
using WowPacketParser.Parsing.Proto;
using WowPacketParser.Proto;

namespace WowPacketParser.SQL.Builders
{
    [ProtoBuilderClass]
    public class Movement : BaseProtoQueryBuilder
    {
        private struct Waypoint
        {
            public Vec3 Position { get; init; }
            public float? Orientation { get; init; }
            public bool Point { get; init; }
        }

        public enum PathType
        {
            None = 0, // pathfinding
            ExactPath = 1, // sent fully
            ExactPathFlying = 2, // sent fully + flying flag
            ExactPathFlyingCyclic = 3, // sent fully + flying flag + cyclic flag
            ExactPathAndJump = 4, // sent fully + parabolic movement at the end
            Invalid = 100,
        }

        private class AggregatedPaths
        {
            public List<Path> Paths { get; set; } = new();
            public int PointCount { get; set; } = 0;
        }

        private class Path
        {
            public List<Waypoint> Waypoints { get; init; }
            public PathType Type { get; init; }
        }

        private Dictionary<UniversalGuid, AggregatedPaths> pathsPerGuid = new();

        /// <summary>
        /// Creature GUID (as text) to the "@MOVID+n" expression of its path in waypoint_path.
        /// Filled by GenerateQuery, read by the spawn builder (proto builders run before the spawn builder).
        /// </summary>
        public static readonly Dictionary<string, string> PathIdByGuid = new();

        /// <summary>Ostatni przesuniecie @MOVID+n uzyte w pliku (-1 gdy brak sciezek).</summary>
        public static int LastPathOffset = -1;

        public override bool IsEnabled() => Settings.SQLOutputFlag.HasAnyFlagBit(SQLOutput.creature_movement) &&
                                            Settings.TargetedProject is TargetedProject.Cmangos or TargetedProject.TrinityCore;

        private PathType GetMovementType(PacketMonsterMove packet)
        {
            PathType flags = PathType.None;
            if (packet.Flags.HasFlag(UniversalSplineFlag.EnterCycle) || packet.Flags.HasFlag(UniversalSplineFlag.Cyclic))
                flags = PathType.ExactPathFlyingCyclic;
            else if (packet.Flags.HasFlag(UniversalSplineFlag.Flying))
                flags = PathType.ExactPathFlying;
            else if (packet.Flags.HasFlag(UniversalSplineFlag.UncompressedPath))
                flags = PathType.ExactPath;
            else if (packet.LookTarget != null)
                flags = PathType.Invalid;
            else if (packet.Jump != null && packet.Jump.StartTime > 0)
                flags = PathType.ExactPathAndJump;
            return flags;
        }

        protected override VoidType Process(PacketBase basePacket, PacketMonsterMove packet)
        {
            if (packet.PackedPoints.Count == 0 && packet.Points.Count == 0)
                return default;

            var movementType = GetMovementType(packet);
            if (movementType == PathType.Invalid)
                return default;

            Path path = new Path()
            {
                Waypoints = new List<Waypoint>(),
                Type = movementType
            };

            foreach (var node in packet.PackedPoints)
            {
                path.Waypoints.Add(new Waypoint()
                {
                    Position = node,
                    Orientation = null,
                    Point = false
                });
            }

            foreach (var node in packet.Points)
            {
                path.Waypoints.Add(new Waypoint()
                {
                    Position = node,
                    Orientation = null,
                    Point = true
                });
            }

            bool dest = false;
            if (packet.Destination != null && !(packet.Destination.X == 0 && packet.Destination.Y == 0 && packet.Destination.Z == 0))
            {
                float? ori = packet.HasLookOrientation ? packet.LookOrientation : null;
                path.Waypoints.Add(new Waypoint()
                {
                    Position = packet.Destination,
                    Orientation = ori,
                    Point = true
                });
                dest = true;
            }

            if (!pathsPerGuid.TryGetValue(packet.Mover, out var pathList))
            {
                pathList = new AggregatedPaths();
                pathsPerGuid.Add(packet.Mover, pathList);
            }
            pathList.Paths.Add(path);
            if (Settings.SkipIntermediatePoints == true)
                pathList.PointCount += packet.Points.Count + (dest ? 1 : 0);
            else
                pathList.PointCount += path.Waypoints.Count;

            return default;
        }

        private const string NODE_HEADER = "INSERT INTO waypoint_path_node (`PathId`, `NodeId`, `PositionX`, `PositionY`, `PositionZ`, `Orientation`) VALUES";

        protected override string GenerateQuery()
        {
            StringBuilder output = new();
            PathIdByGuid.Clear();
            LastPathOffset = -1;
            // Same reason as the spawn GUIDs: take the first free path id from the database
            // instead of leaving a placeholder that has to be filled in by hand.
            output.AppendLine("SET @MOVID := (SELECT IFNULL(MAX(`PathId`), 0) + 1 FROM `waypoint_path`);");
            int pathIdCounter = 0;
            foreach (var (mover, paths) in pathsPerGuid)
            {
                string commentInformation = $"GUID: {mover.ToWowParserString()}";
                output.AppendLine("-- " + commentInformation);
                if (Settings.TargetedProject == TargetedProject.Cmangos)
                {
                    output.AppendLine($"INSERT INTO waypoint_path_name(PathId, Name) VALUES(@MOVID + {pathIdCounter},'{SQLUtil.EscapeString(StoreGetters.GetName(Utilities.ObjectTypeToStore(mover.Type.ToObjectType()), (int)mover.Entry))}');");
                    output.AppendLine("INSERT INTO waypoint_path (PathId, Point, PositionX, PositionY, PositionZ, Orientation, WaitTime, ScriptId, Comment) VALUES");
                }
                else if (Settings.TargetedProject == TargetedProject.TrinityCore)
                {
                    var types = paths.Paths.Select(k => k.Type).Distinct().ToArray();
                    var flags = types.Length == 1 && types[0] is PathType.ExactPath or PathType.ExactPathFlying or PathType.ExactPathFlyingCyclic ? 2 : 0;

                    output.AppendLine("INSERT INTO waypoint_path (`PathId`, `MoveType`, `Flags`, `Velocity`, `Comment`) VALUES");
                    output.AppendLine($"(@MOVID + {pathIdCounter}, 0, {flags}, NULL, '{SQLUtil.EscapeString(StoreGetters.GetName(Utilities.ObjectTypeToStore(mover.Type.ToObjectType()), (int)mover.Entry))}');");
                    output.AppendLine(NODE_HEADER);
                }
                int pointIdCounter = 1;
                int emittedRows = 0;
                foreach (var path in paths.Paths)
                {
                    foreach (var node in path.Waypoints)
                    {
                        if (node.Point == false)
                        {
                            if (Settings.SkipIntermediatePoints)
                                continue;
                            output.Append("-- ");
                        }

                        // A row ends the statement only when the last node of the whole track has
                        // just been written; everything before it stays separated by commas.
                        if (Settings.TargetedProject == TargetedProject.Cmangos)
                            output.Append($"(@MOVID + {pathIdCounter}, '{pointIdCounter}', '{node.Position.X}', '{node.Position.Y}', '{node.Position.Z}', '{node.Orientation ?? 100.0f}', '0', '0', NULL)");
                        else if (Settings.TargetedProject == TargetedProject.TrinityCore)
                            output.Append($"(@MOVID + {pathIdCounter}, {pointIdCounter}, {node.Position.X}, {node.Position.Y}, {node.Position.Z}, {(node.Orientation.HasValue ? node.Orientation.Value.ToString(CultureInfo.InvariantCulture) : "NULL")})");

                        output.Append(pointIdCounter == paths.PointCount ? ';' : ',');
                        ++emittedRows;

                        if (pointIdCounter == 1)
                            output.Append($" -- PathType: {path.Type}");

                        output.AppendLine();

                        ++pointIdCounter;
                    }
                }

                // Each emitted row already ends the statement (';' on the track's last node), so a
                // track whose nodes are all commented out leaves the waypoint_path_node header with
                // a VALUES and no row, and the next statement would be glued onto it. Drop that
                // header in that case: count the rows that actually survived.
                if (emittedRows == 0)
                {
                    string dump = output.ToString();
                    int headerAt = dump.LastIndexOf(NODE_HEADER, System.StringComparison.Ordinal);
                    if (headerAt >= 0)
                    {
                        int headerEnd = dump.IndexOf('\n', headerAt);
                        dump = dump.Remove(headerAt, headerEnd - headerAt + 1);
                        output.Clear();
                        output.Append(dump);
                    }
                }

                if (emittedRows > 0)
                    PathIdByGuid[mover.ToWowParserString()] = $"@MOVID+{pathIdCounter}";

                output.AppendLine();
                output.AppendLine();

                LastPathOffset = pathIdCounter;
                ++pathIdCounter;
            }
            return output.ToString();
        }
    }
}
