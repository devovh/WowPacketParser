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
            //
            // They have to be recomputed for every block, not just read from the top of the file:
            // @CGUID+n never becomes a real guid, so the creature rows alone leave MAX(guid)
            // where it was. A later block in a file that is appended to (several sniffs into one
            // SQLFileName) would then reuse the very same guids and every spawn would be in the
            // database twice. Setting them before each block makes a second import a no-op.
            var genCreatures = Settings.SQLOutputFlag.HasAnyFlagBit(Enums.SQLOutput.creature);
            var genGameObjects = Settings.SQLOutputFlag.HasAnyFlagBit(Enums.SQLOutput.gameobject);
            var genAreaTriggers = Settings.SQLOutputFlag.HasAnyFlagBit(Enums.SQLOutput.areatrigger);

            _file.WriteLine("-- Spawn id ranges for the block below. Recomputed from the database so the file can be");
            _file.WriteLine("-- imported twice, or appended to by another sniff, without duplicating what it creates.");
            _file.WriteLine("-- The primary key step of the inserts on `creature` and `gameobject` belongs to this,");
            _file.WriteLine("-- so do not change AUTO_INCREMENT of those tables after running this file.");

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

            // Give the file back its AUTO_INCREMENT. Setting the following ids with
            // MAX(guid)+1 while the AUTO_INCREMENT counter still sits below them would make MySQL
            // hand out ids in the middle of the range the block above just used on the next insert.
            if (genCreatures || genGameObjects)
            {
                _file.WriteLine("-- Give the tables back their AUTO_INCREMENT past the ids this block used.");
                if (genCreatures)
                    _file.WriteLine("ALTER TABLE `creature` AUTO_INCREMENT = (SELECT IFNULL(MAX(`guid`), 0) + 1 FROM `creature`);");
                if (genGameObjects)
                    _file.WriteLine("ALTER TABLE `gameobject` AUTO_INCREMENT = (SELECT IFNULL(MAX(`guid`), 0) + 1 FROM `gameobject`);");
                _file.WriteLine();
            }

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
