﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Unfoundry
{
    public class Config : IEnumerable<ConfigGroup>
    {
        internal static Dictionary<string, Config> _configs = new Dictionary<string, Config>();

        public static Dictionary<string, Config> allConfigs => _configs;

        private string _path;

        private Dictionary<string, ConfigGroup> _groups = new Dictionary<string, ConfigGroup>();

        public Config(string guid)
        {
            if (_configs.ContainsKey(guid)) throw new Exception($"Config '{guid}' already exists.");

            _path = Path.Combine(Path.GetFullPath("."), $"Config\\{guid.CoerceValidFileName()}.ini");

            _configs.Add(guid, this);
        }

        public static Config Get(string guid)
        {
            if (_configs.TryGetValue(guid, out var config)) return config;
            return null;
        }

        public static ConfigGroup Get(string guid, string groupName)
        {
            var config = Get(guid);
            if (config == null) return null;

            if (config._groups.TryGetValue(groupName, out var group)) return group;

            return null;
        }

        public static ConfigEntry Get(string guid, string groupName, string entryName)
        {
            var group = Get(guid, groupName);
            if (group == null) return null;

            if (group.TryGetEntry(entryName, out var entry)) return entry;

            return null;
        }

        public static T GetOrDefault<T>(string guid, string groupName, string entryName, T defaultValue)
        {
            try
            {
                var entry = Get(guid, groupName, entryName);
                if (entry != null) return entry.Get<T>();
            }
            catch(Exception)
            {
            }

            return defaultValue;
        }

        public ConfigGroup Group(string name, params string[] description)
        {
            if (_groups.TryGetValue(name, out var existingGroup)) return existingGroup;

            var newGroup = new ConfigGroup(this, name, description);
            _groups[name] = newGroup;
            return newGroup;
        }

        public Config Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path));

            using(var writer = new StreamWriter(_path, false, Encoding.UTF8, 1024))
            {
                foreach (var group in _groups)
                {
                    writer.WriteLine();
                    writer.WriteLine();

                    var description = group.Value.description;
                    if (description != null)
                    {
                        foreach (var line in description) writer.WriteLine($"# {line}");
                    }

                    writer.WriteLine($"[{group.Key}]");
                    group.Value.Save(writer);
                }
            }

            return this;
        }

        private readonly char[] keyValueSeparators = new char[]  { '=' };
        public Config Load()
        {
            if (!File.Exists(_path)) return this;


            ConfigGroup group = null;
            var lines = File.ReadAllLines(_path);
            Debug.Log($"Unfoundry: Loading {lines.Length} line from config file '{_path}'");
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine)) continue;
                if (trimmedLine.StartsWith("#")) continue;

                if (trimmedLine.StartsWith("["))
                {
                    if (!trimmedLine.EndsWith("]")) continue;
                    var groupName = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    if (!_groups.TryGetValue(groupName, out group))
                    {
                        Debug.Log($"Unfoundry: Group '{groupName}' not found in config file '{_path}'");
                        group = null;
                    }
                }

                if (group == null) continue;

                var splitLine = trimmedLine.Split(keyValueSeparators, 2);
                if (splitLine.Length != 2) continue;

                var key = splitLine[0].Trim();
                if (group.TryGetEntry(key, out var entry))
                {
                    var value = splitLine[1].Trim();
                    entry.Load(value);
                }
            }

            return this;
        }

        public delegate string TypedSaver(object value);
        public delegate object TypedLoader(string value, Type type);

        private static Dictionary<Type, TypedSaver> _typedSavers = new Dictionary<Type, TypedSaver>
        {
            [typeof(bool)] = (value) => value.ToString().ToLowerInvariant(),
            [typeof(char)] = (value) => $"{(char)value}",
            [typeof(byte)] = (value) => value.ToString(),
            [typeof(sbyte)] = (value) => value.ToString(),
            [typeof(short)] = (value) => value.ToString(),
            [typeof(ushort)] = (value) => value.ToString(),
            [typeof(int)] = (value) => value.ToString(),
            [typeof(uint)] = (value) => value.ToString(),
            [typeof(long)] = (value) => value.ToString(),
            [typeof(ulong)] = (value) => value.ToString(),
            [typeof(float)] = (value) => ((float)value).ToString(NumberFormatInfo.InvariantInfo),
            [typeof(double)] = (value) => ((double)value).ToString(NumberFormatInfo.InvariantInfo),
            [typeof(decimal)] = (value) => ((decimal)value).ToString(NumberFormatInfo.InvariantInfo),
            [typeof(string)] = (value) => ((string)value).Escape(),
            [typeof(Enum)] = (value) => value.ToString(),
            [typeof(Vector2)] = (value) => value.ToString(),
            [typeof(Vector3)] = (value) => value.ToString(),
            [typeof(Vector4)] = (value) => value.ToString(),
            [typeof(Vector2Int)] = (value) => value.ToString(),
            [typeof(Vector3Int)] = (value) => value.ToString(),
            [typeof(Vector4Int)] = (value) => value.ToString(),
        };
        private static Dictionary<Type, TypedLoader> _typedLoaders = new Dictionary<Type, TypedLoader>
        {
            [typeof(bool)] = (value, type) => bool.Parse(value),
            [typeof(char)] = (value, type) => (value.Length > 0) ? value[0] : '\0',
            [typeof(byte)] = (value, type) => byte.Parse(value),
            [typeof(sbyte)] = (value, type) => sbyte.Parse(value),
            [typeof(short)] = (value, type) => short.Parse(value),
            [typeof(ushort)] = (value, type) => ushort.Parse(value),
            [typeof(int)] = (value, type) => int.Parse(value),
            [typeof(uint)] = (value, type) => uint.Parse(value),
            [typeof(long)] = (value, type) => long.Parse(value),
            [typeof(ulong)] = (value, type) => ulong.Parse(value),
            [typeof(float)] = (value, type) => float.Parse(value, NumberFormatInfo.InvariantInfo),
            [typeof(double)] = (value, type) => double.Parse(value, NumberFormatInfo.InvariantInfo),
            [typeof(decimal)] = (value, type) => decimal.Parse(value, NumberFormatInfo.InvariantInfo),
            [typeof(string)] = (value, type) => value.Unescape(),
            [typeof(Enum)] = (value, type) => Enum.Parse(type, value, true),
            [typeof(Vector2)] = (value, type) => value.ToVector2(),
            [typeof(Vector3)] = (value, type) => value.ToVector3(),
            [typeof(Vector4)] = (value, type) => value.ToVector4(),
            [typeof(Vector2Int)] = (value, type) => value.ToVector2Int(),
            [typeof(Vector3Int)] = (value, type) => value.ToVector3Int(),
            [typeof(Vector4Int)] = (value, type) => value.ToVector4Int(),
        };

        internal static bool TryGetSaver(Type type, out TypedSaver saver)
        {
            if (type.IsEnum) type = typeof(Enum);
            return _typedSavers.TryGetValue(type, out saver);
        }

        internal static bool TryGetLoader(Type type, out TypedLoader loader)
        {
            if (type.IsEnum) type = typeof(Enum);
            return _typedLoaders.TryGetValue(type, out loader);
        }

        public IEnumerator<ConfigGroup> GetEnumerator()
        {
            return _groups.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _groups.Values.GetEnumerator();
        }
    }

    public class ConfigGroup : IEnumerable<ConfigEntry>
    {
        private Config _config;
        private string _name;
        private string[] _description;
        private Dictionary<string, ConfigEntry> _entries = new Dictionary<string, ConfigEntry>();

        internal Config config => _config;
        public string name => _name;
        public string[] description => _description;

        internal ConfigGroup(Config config, string name, string[] description)
        {
            _config = config;
            _name = name;
            _description = description;
        }

        public Config EndGroup() => _config;

        public bool TryGetEntry(string name, out ConfigEntry entry)
        {
            return _entries.TryGetValue(name, out entry);
        }

        public ConfigGroup Entry<T>(out TypedConfigEntry<T> entry, string name, T defaultValue, params string[] description)
        {
            if (_entries.ContainsKey(name))
            {
                throw new Exception($"Config entry '{name}' already exists in group '{_name}'.");
            }

            entry = new TypedConfigEntry<T>(this, name, defaultValue, false, description);
            _entries[name] = entry;
            return this;
        }

        public ConfigGroup Entry<T>(out TypedConfigEntry<T> entry, string name, T defaultValue, Action<T, T> onChanged, params string[] description)
        {
            if (_entries.ContainsKey(name))
            {
                throw new Exception($"Config entry '{name}' already exists in group '{_name}'.");
            }

            entry = new TypedConfigEntry<T>(this, name, defaultValue, false, description, onChanged);
            _entries[name] = entry;
            return this;
        }

        public ConfigGroup Entry<T>(out TypedConfigEntry<T> entry, string name, T defaultValue, bool requiresRestart, params string[] description)
        {
            if (_entries.ContainsKey(name))
            {
                throw new Exception($"Config entry '{name}' already exists in group '{_name}'.");
            }

            entry = new TypedConfigEntry<T>(this, name, defaultValue, requiresRestart, description);
            _entries[name] = entry;
            return this;
        }

        public ConfigGroup Entry<T>(out TypedConfigEntry<T> entry, string name, T defaultValue, bool requiresRestart, Action<T, T> onChanged, params string[] description)
        {
            if (_entries.ContainsKey(name))
            {
                throw new Exception($"Config entry '{name}' already exists in group '{_name}'.");
            }

            entry = new TypedConfigEntry<T>(this, name, defaultValue, requiresRestart, description, onChanged);
            _entries[name] = entry;
            return this;
        }

        internal void Save(StreamWriter writer)
        {
            foreach (var entry in _entries)
            {
                writer.WriteLine();

                var description = entry.Value.description;
                if (description != null)
                {
                    foreach (var line in description) writer.WriteLine($"# {line}");
                }

                writer.WriteLine($"{entry.Key} = {entry.Value.Save()}");
            }
        }

        public IEnumerator<ConfigEntry> GetEnumerator()
        {
            return _entries.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _entries.Values.GetEnumerator();
        }
    }

    public abstract class ConfigEntry
    {
        private ConfigGroup _group;
        private string _name;
        private bool _requiresRestart;
        private string[] _description;

        internal ConfigGroup group => _group;
        public string name => _name;
        public string fullName => $"{_group?.name ?? ""}.{_name}";
        public bool requiresRestart => _requiresRestart;
        public string[] description => _description;

        public abstract Type selfType { get; }
        public abstract Type valueType { get; }

        internal ConfigEntry(ConfigGroup group, string name, bool requiresRestart, string[] description)
        {
            _group = group;
            _name = name;
            _requiresRestart = requiresRestart;
            _description = description;
        }

        public T Get<T>()
        {
            if (!(this is TypedConfigEntry<T>)) throw new Exception($"Type mismatch. Expected TypedConfigEntry<{typeof(T).Name}>, got {GetType().Name}");
            return (this as TypedConfigEntry<T>).Get();
        }

        internal abstract void Load(string source);
        internal abstract string Save();
    }

    public class TypedConfigEntry<T> : ConfigEntry
    {
        public event Action<T, T> onChanged;

        private T _defaultValue;
        private T _value;

        internal T value => _value;

        public override Type selfType => typeof(TypedConfigEntry<T>);
        public override Type valueType => typeof(T);

        internal TypedConfigEntry(ConfigGroup group, string name, T defaultValue, bool requiresRestart, string[] description)
            : base(group, name, requiresRestart, description)
        {
            _defaultValue = _value = defaultValue;
        }

        public TypedConfigEntry(ConfigGroup group, string name, T defaultValue, bool requiresRestart, string[] description, Action<T, T> onChanged)
            : base(group, name, requiresRestart, description)
        {
            _defaultValue = _value = defaultValue;
            this.onChanged = onChanged;
        }

        public T Get() => _value;
        public void Set(T value)
        {
            if (EqualityComparer<T>.Default.Equals(_value, value)) return;

            var oldValue = value;
            _value = value;
            onChanged?.Invoke(oldValue, _value);
            group?.config?.Save();
        }

        internal override void Load(string source)
        {
            try
            {
                if (Config.TryGetLoader(typeof(T), out var loader))
                {
                    _value = (T)loader(source, typeof(T));
                }
                else
                {
                    Debug.Log($"Unfoundry: Unsupported config value type {typeof(T).Name} in '{fullName}'. Using default.");
                    _value = _defaultValue;
                }
            }
            catch(Exception)
            {
                Debug.Log($"Unfoundry: Failed to parse config value '{source}' in '{fullName}' as type {typeof(T).Name}. Using default.");
                _value = _defaultValue;
            }
        }

        internal override string Save()
        {
            try
            {
                if (Config.TryGetSaver(typeof(T), out var saver))
                {
                    return saver(_value);
                }
                else
                {
                    Debug.Log($"Unfoundry: Unsupported config value type {typeof(T).Name} in '{fullName}'.");
                }
            }
            catch (Exception)
            {
                Debug.Log($"Unfoundry: Failed to save config value '{_value}' for '{fullName}'.");
            }

            return string.Empty;
        }

        public override string ToString()
        {
            return _value.ToString();
        }
    }
}
