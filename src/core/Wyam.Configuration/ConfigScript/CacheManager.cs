﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using Wyam.Common.Execution;
using Wyam.Common.IO;

namespace Wyam.Configuration.ConfigScript
{
    internal class CacheManager
    {
        private readonly IEngine _engine;
        private readonly IScriptManager _scriptManager;

        public FilePath ConfigDllPath { get; }

        public FilePath ConfigHashPath { get; }

        public FilePath OutputScriptPath { get; }

        internal CacheManager(IEngine engine, IScriptManager scriptManager, FilePath configDllPath, FilePath configHashPath, FilePath outputScriptPath)
        {
            _engine = engine;
            _scriptManager = scriptManager;
            ConfigDllPath = configDllPath;
            ConfigHashPath = configHashPath;
            OutputScriptPath = outputScriptPath;
        }

        public void EvaluateCode(string code, IReadOnlyCollection<Type> classes, bool outputScript, bool ignoreConfigHash, bool noOutputConfigAssembly)
        {
            string cachedHash = GetCachedConfigHash();
            string currentHash = HashString(code);

            byte[] cachedConfig = (cachedHash != currentHash || ignoreConfigHash) ? null : GetCachedConfig();
            if (cachedConfig != null)
            {
                // Load from cache if the hashes match and we got a cached config file
                _scriptManager.LoadCompiledConfig(cachedConfig);
            }
            else
            {
                // Otherwise compile the config and save it as a (new) cached version
                _scriptManager.Create(code, classes, _engine.Namespaces);
                WriteScript(_scriptManager.Code, outputScript);
                _scriptManager.Compile(AppDomain.CurrentDomain.GetAssemblies());
                SaveCompiledScript(currentHash, noOutputConfigAssembly);
            }

            _engine.DynamicAssemblies.Add(_scriptManager.RawAssembly);
            _scriptManager.Evaluate(_engine);
        }

        private string GetCachedConfigHash()
        {
            if (ConfigHashPath == null)
            {
                return null;
            }
            IFile configHashFile = _engine.FileSystem.GetRootFile(ConfigHashPath);
            string hash = null;
            if (configHashFile?.Exists == true)
            {
                hash = configHashFile.ReadAllText();
            }
            return hash;
        }

        private byte[] GetCachedConfig()
        {
            if (ConfigDllPath == null)
            {
                return null;
            }
            IFile configDllFile = _engine.FileSystem.GetRootFile(ConfigDllPath);
            if (!configDllFile.Exists)
            {
                return null;
            }
            using (Stream stream = configDllFile.OpenRead())
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    return memory.ToArray();
                }
            }
        }

        private void SaveCompiledScript(string scriptHash, bool noOutputConfigAssembly)
        {
            if (noOutputConfigAssembly || ConfigHashPath == null || ConfigDllPath == null)
            {
                return;
            }

            IFile configHashFile = _engine.FileSystem.GetRootFile(ConfigHashPath);
            configHashFile.WriteAllText(scriptHash);

            IFile configDllFile = _engine.FileSystem.GetRootFile(ConfigDllPath);
            using (MemoryStream memory = new MemoryStream(_scriptManager.RawAssembly))
            {
                using (Stream stream = configDllFile.OpenWrite())
                {
                    memory.CopyTo(stream);
                }
            }
        }

        private void WriteScript(string code, bool outputScript)
        {
            // Output only if requested
            if (outputScript)
            {
                FilePath outputPath = _engine.FileSystem.RootPath.CombineFile(OutputScriptPath ?? new FilePath($"{ScriptManager.AssemblyName}.cs"));
                _engine.FileSystem.GetFile(outputPath)?.WriteAllText(code);
            }
        }

        private static string HashString(string input)
        {
            // https://stackoverflow.com/a/24031467/2001966 with simplifications.
            using (SHA512 sha = SHA512.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                foreach (byte t in hashBytes)
                {
                    sb.Append(t.ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }
}