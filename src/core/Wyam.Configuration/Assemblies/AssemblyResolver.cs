using System;
using System.Collections.Concurrent;
using System.Reflection;
using Wyam.Common.Tracing;
using Wyam.Configuration.ConfigScript;

namespace Wyam.Configuration.Assemblies
{
    internal class AssemblyResolver : IDisposable
    {
        private readonly ConcurrentDictionary<string, Assembly> _fullNameCache = new ConcurrentDictionary<string, Assembly>();
        private readonly ConcurrentDictionary<string, Assembly> _nameCache = new ConcurrentDictionary<string, Assembly>();
        private readonly ScriptManager _scriptManager;

        private bool _disposed;

        public AssemblyResolver(ScriptManager scriptManager)
        {
            _scriptManager = scriptManager;

            // Add any already loaded assemblies to the collection
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                AddToCache(assembly);
            }

            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
            _disposed = true;
        }

        public bool TryGet(string fullName, out Assembly assembly) =>
            _fullNameCache.TryGetValue(fullName, out assembly) || _nameCache.TryGetValue(fullName.Split(',')[0], out assembly);

        private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args) => AddToCache(args.LoadedAssembly);

        private void AddToCache(Assembly assembly)
        {
            if (!assembly.ReflectionOnly)
            {
                AssemblyName assemblyName = assembly.GetName();
                _fullNameCache.TryAdd(assemblyName.FullName, assembly);
                _nameCache.TryAdd(assemblyName.Name, assembly);
            }
        }

        public bool ReportCacheMisses { get; set; } = false;

        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            // Resolve the config compilation if we have one
            if (_scriptManager.Assembly != null && args.Name == _scriptManager.AssemblyFullName)
            {
                // Return the dynamically compiled config assembly if given it's name
                return _scriptManager.Assembly;
            }

            // Return an assembly from the cache (check for a full name first)
            if (TryGet(args.Name, out Assembly assembly))
            {
                return assembly;
            }
            if (ReportCacheMisses)
            {
                Trace.Verbose($"Assembly resolver cache miss for {args.Name} requested from {args.RequestingAssembly?.GetName()?.Name ?? "unknown"}");
            }
            return null;
        }
    }
}