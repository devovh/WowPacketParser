using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WowPacketParser.Misc;

namespace WowPacketParser.SQL
{
    public class SQLFile : IDisposable
    {
        private StreamWriter _file;

        private readonly string _fileName;

        private readonly List<string> _sqls = new List<string>();

        private readonly bool _append;

        public SQLFile(string file)
        {
            // A global SQLFileName means every sniff in the run writes into the same file.
            // In that case the file is kept and appended to, unless the user asked otherwise
            // with KeepSQLFile = false. Without a global name each sniff gets its own file, so
            // it is deleted first rather than left over from an earlier run.
            bool global = !string.IsNullOrWhiteSpace(Settings.SQLFileName);
            if (!global)
                File.Delete(file);
            else
                _append = Settings.KeepSQLFile;

            _fileName = file;
        }

        ~SQLFile()
        {
            Dispose(false);
        }

        public void WriteData(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return;

            _sqls.Add(sql);
        }

        public bool WriteToFile(string header)
        {
            if (_sqls.All(String.IsNullOrWhiteSpace))
                return false;

            _file = new StreamWriter(_fileName, _append);

            _file.WriteLine(header);

            // The spawn rows are written with a placeholder GUID offset, so the file only imports
            // once these variables hold a number. Take the first free GUID from the database the
            // file is imported into instead of leaving a value that has to be typed in by hand.
            // They are set again before every block, so a block appended by a later sniff starts
            // after the rows the earlier block inserted.
            //
            // This alone does not make a second import harmless: the second time MAX(guid)+1 lies
            // past the first import's rows, so every spawn would be inserted again. For the
            // Forever target the spawn builders end each block with a pass that removes the rows
            // of the block that duplicate a spawn already in the database (see Spawns.cs).
            var genCreatures = Settings.SQLOutputFlag.HasAnyFlagBit(Enums.SQLOutput.creature);
            var genGameObjects = Settings.SQLOutputFlag.HasAnyFlagBit(Enums.SQLOutput.gameobject);
            var genAreaTriggers = Settings.SQLOutputFlag.HasAnyFlagBit(Enums.SQLOutput.areatrigger);

            _file.WriteLine("-- Spawn id ranges for the block below, taken from the database the file is imported into.");

            if (genCreatures)
                _file.WriteLine("SET @CGUID := (SELECT IFNULL(MAX(`guid`), 0) + 1 FROM `creature`);");

            if (genGameObjects)
                _file.WriteLine("SET @OGUID := (SELECT IFNULL(MAX(`guid`), 0) + 1 FROM `gameobject`);");

            if (genAreaTriggers)
            {
                _file.WriteLine("SET @ATSPAWNID := (SELECT IFNULL(MAX(`SpawnId`), 0) + 1 FROM `areatrigger`);");
                _file.WriteLine("SET @ATPROPERTIESID := (SELECT IFNULL(MAX(`Id`), 0) + 1 FROM `areatrigger_create_properties`);");
            }

            if (genCreatures || genGameObjects || genAreaTriggers)
                _file.WriteLine();

            foreach (var sql in _sqls)
                _file.WriteLine(sql);

            // No ALTER TABLE ... AUTO_INCREMENT here: `creature`.`guid` and `gameobject`.`guid` are
            // not AUTO_INCREMENT columns in the TrinityCore world database, and AUTO_INCREMENT only
            // takes a literal, so "AUTO_INCREMENT = (SELECT ...)" is a syntax error that stops the
            // import at that line.

            return true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
                _sqls.Clear();

            if (_file != null)
            {
                _file.Flush();
                _file.Close();
                _file = null;
            }
        }
    }
}
