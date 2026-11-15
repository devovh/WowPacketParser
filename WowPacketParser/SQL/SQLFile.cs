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

        public SQLFile(string file)
        {
            // only delete file if no global file name was specified, unless KeepSQLFile is set
            if (string.IsNullOrWhiteSpace(Settings.SQLFileName) || !Settings.KeepSQLFile)
                File.Delete(file);
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

            _file = new StreamWriter(_fileName, true);

            _file.WriteLine(header);

            // The spawn rows are written with a placeholder GUID offset, so the file only imports
            // once these variables hold a number. Take the first free GUID from the database the
            // file is imported into instead of leaving a value that has to be typed in by hand.
            var genCreatures = Settings.SQLOutputFlag.HasAnyFlagBit(Enums.SQLOutput.creature);
            if (genCreatures)
                _file.WriteLine("SET @CGUID := (SELECT IFNULL(MAX(`guid`), 0) + 1 FROM `creature`);");

            var genGameObjects = Settings.SQLOutputFlag.HasAnyFlagBit(Enums.SQLOutput.gameobject);
            if (genGameObjects)
                _file.WriteLine("SET @OGUID := (SELECT IFNULL(MAX(`guid`), 0) + 1 FROM `gameobject`);");

            var genAreaTriggers = Settings.SQLOutputFlag.HasAnyFlagBit(Enums.SQLOutput.areatrigger);
            if (genAreaTriggers)
            {
                _file.WriteLine("SET @ATSPAWNID := (SELECT IFNULL(MAX(`SpawnId`), 0) + 1 FROM `areatrigger`);");
                _file.WriteLine("SET @ATPROPERTIESID := (SELECT IFNULL(MAX(`Id`), 0) + 1 FROM `areatrigger_create_properties`);");
            }

            if (genCreatures || genGameObjects || genAreaTriggers)
                _file.WriteLine();

            foreach (var sql in _sqls)
                _file.WriteLine(sql);

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
