﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Wyam.Common.Util;

namespace Wyam.Razor
{
    /// <summary>
    /// A RazorProjectFileSystem that lets us use the Wyam file provider while
    /// allowing replacement of the stream with document content.
    /// </summary>
    internal class WyamRazorProjectFileSystem : FileProviderRazorProjectFileSystem
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public WyamRazorProjectFileSystem(IRazorViewEngineFileProviderAccessor accessor, IHostingEnvironment hostingEnviroment)
            : base(accessor, hostingEnviroment)
        {
            _hostingEnvironment = hostingEnviroment;
        }

        public RazorProjectItem GetItem(string path, Stream stream)
        {
            FileProviderRazorProjectItem projectItem = (FileProviderRazorProjectItem)GetItem(path);
            return new FileProviderRazorProjectItem(
                new StreamFileInfo(projectItem.FileInfo, stream),
                projectItem.BasePath,
                projectItem.FilePath,
                _hostingEnvironment.ContentRootPath);
        }
    }
}