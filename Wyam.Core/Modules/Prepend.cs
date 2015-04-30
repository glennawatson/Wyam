﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wyam.Extensibility;

namespace Wyam.Core.Modules
{
    // Prepends the specified content to the existing content
    public class Prepend : ContentModule
    {
        public Prepend(object content)
            : base(content)
        {
        }

        public Prepend(Func<IModuleContext, object> content)
            : base(content)
        {
        }

        public Prepend(params IModule[] modules)
            : base(modules)
        {
        }

        protected override IEnumerable<IModuleContext> Execute(object content, IModuleContext input, IPipelineContext pipeline)
        {
            return new[] { content == null ? input : input.Clone(content + input.Content) };
        }
    }
}