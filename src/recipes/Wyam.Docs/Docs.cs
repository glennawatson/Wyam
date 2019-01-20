﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wyam.CodeAnalysis;
using Wyam.Common.Configuration;
using Wyam.Common.Documents;
using Wyam.Common.Execution;
using Wyam.Common.IO;
using Wyam.Common.Meta;
using Wyam.Common.Modules;
using Wyam.Common.Util;
using Wyam.Core.Modules.Contents;
using Wyam.Core.Modules.Control;
using Wyam.Core.Modules.Metadata;
using Wyam.Docs.Pipelines;
using Wyam.Feeds;
using Wyam.Html;
using Wyam.Web;
using Wyam.Web.Pipelines;
using ValidateLinks = Wyam.Web.Pipelines.ValidateLinks;

namespace Wyam.Docs
{
    /// <summary>
    /// A recipe for creating documentation websites.
    /// </summary>
    /// <metadata cref="DocsKeys.Title" usage="Setting">The title of the site.</metadata>
    /// <metadata cref="DocsKeys.Title" usage="Input">The title of the post or page.</metadata>
    /// <metadata cref="DocsKeys.ProcessIncludes" usage="Setting" />
    /// <metadata cref="DocsKeys.ProcessIncludes" usage="Input" />
    /// <metadata cref="DocsKeys.Logo" usage="Setting" />
    /// <metadata cref="DocsKeys.SourceFiles" usage="Setting" />
    /// <metadata cref="DocsKeys.SolutionFiles" usage="Setting" />
    /// <metadata cref="DocsKeys.ProjectFiles" usage="Setting" />
    /// <metadata cref="DocsKeys.AssemblyFiles" usage="Setting" />
    /// <metadata cref="DocsKeys.BaseEditUrl" usage="Setting" />
    /// <metadata cref="DocsKeys.IncludeGlobalNamespace" usage="Setting" />
    /// <metadata cref="DocsKeys.AutoLinkTypes" usage="Setting" />
    /// <metadata cref="DocsKeys.IncludeDateInPostPath" usage="Setting" />
    /// <metadata cref="DocsKeys.SearchIndex" usage="Setting" />
    /// <metadata cref="DocsKeys.CaseInsensitiveCategories" usage="Setting" />
    /// <metadata cref="DocsKeys.CaseInsensitiveTags" usage="Setting" />
    /// <metadata cref="DocsKeys.CaseInsensitiveAuthors" usage="Setting" />
    /// <metadata cref="DocsKeys.BlogPageSize" usage="Setting" />
    /// <metadata cref="DocsKeys.CategoryPageSize" usage="Setting" />
    /// <metadata cref="DocsKeys.TagPageSize" usage="Setting" />
    /// <metadata cref="DocsKeys.AuthorPageSize" usage="Setting" />
    /// <metadata cref="DocsKeys.MonthPageSize" usage="Setting" />
    /// <metadata cref="DocsKeys.YearPageSize" usage="Setting" />
    /// <metadata cref="DocsKeys.MarkdownConfiguration" usage="Setting" />
    /// <metadata cref="DocsKeys.MarkdownExtensionTypes" usage="Setting" />
    /// <metadata cref="DocsKeys.IgnoreFolders" usage="Setting" />
    /// <metadata cref="DocsKeys.MetaRefreshRedirects" usage="Setting" />
    /// <metadata cref="DocsKeys.NetlifyRedirects" usage="Setting" />
    /// <metadata cref="DocsKeys.BlogRssPath" usage="Setting" />
    /// <metadata cref="DocsKeys.BlogAtomPath" usage="Setting" />
    /// <metadata cref="DocsKeys.BlogRdfPath" usage="Setting" />
    /// <metadata cref="DocsKeys.BlogPath" usage="Setting" />
    /// <metadata cref="DocsKeys.BlogTitle" usage="Setting" />
    /// <metadata cref="DocsKeys.ValidateAbsoluteLinks" usage="Setting" />
    /// <metadata cref="DocsKeys.ValidateRelativeLinks" usage="Setting" />
    /// <metadata cref="DocsKeys.ValidateLinksAsError" usage="Setting" />
    /// <metadata cref="DocsKeys.MarkdownPrependLinkRoot" usage="Setting" />
    /// <metadata cref="DocsKeys.Description" usage="Input" />
    /// <metadata cref="DocsKeys.Category" usage="Input" />
    /// <metadata cref="DocsKeys.Tags" usage="Input" />
    /// <metadata cref="DocsKeys.Order" usage="Input" />
    /// <metadata cref="DocsKeys.NoSidebar" usage="Input" />
    /// <metadata cref="DocsKeys.NoContainer" usage="Input" />
    /// <metadata cref="DocsKeys.NoTitle" usage="Input" />
    /// <metadata cref="DocsKeys.NoGutter" usage="Input" />
    /// <metadata cref="DocsKeys.Published" usage="Input" />
    /// <metadata cref="DocsKeys.Author" usage="Input" />
    /// <metadata cref="DocsKeys.ShowInNavbar" usage="Input" />
    /// <metadata cref="DocsKeys.ShowInSidebar" usage="Input" />
    /// <metadata cref="DocsKeys.ImplicitInheritDoc" usage="Input" />
    /// <metadata cref="WebKeys.EditFilePath" usage="Output" />
    public class Docs : Recipe
    {
        /// <summary>
        /// Passes type names and paths from the Api pipeline to following ones for auto-linking.
        /// </summary>
        private static readonly ConcurrentDictionary<string, string> TypeNamesToLink = new ConcurrentDictionary<string, string>();

        /// <inheritdoc cref="Pipelines.Code" />
        [SourceInfo]
        public static Code Code { get; } = new Code();

        /// <inheritdoc cref="Pipelines.Api" />
        [SourceInfo]
        public static Api Api { get; } = new Api(TypeNamesToLink);

        /// <inheritdoc cref="Web.Pipelines.BlogPosts" />
        // Contains an ugly hack to re-escape @ symbols in Markdown since AngleSharp unescapes them if it
        // changes text content to add an auto link, can be removed if AngleSharp #494 is addressed
        [SourceInfo]
        public static BlogPosts BlogPosts { get; } = new BlogPosts(
            nameof(BlogPosts),
            new BlogPostsSettings
            {
                PublishedKey = DocsKeys.Published,
                MarkdownConfiguration = ctx => ctx.String(DocsKeys.MarkdownConfiguration),
                MarkdownExtensionTypes = ctx => ctx.List<Type>(DocsKeys.MarkdownExtensionTypes),
                ProcessIncludes = (doc, ctx) => doc.Bool(DocsKeys.ProcessIncludes),
                IncludeDateInPostPath = ctx => ctx.Bool(DocsKeys.IncludeDateInPostPath),
                PostsPath = ctx => ctx.DirectoryPath(DocsKeys.BlogPath, ".").FullPath,
                PrependLinkRoot = ctx => ctx.Bool(DocsKeys.MarkdownPrependLinkRoot)
            })
                .InsertAfter(
                    BlogPosts.RazorPosts,
                    new If(
                        ctx => ctx.Bool(DocsKeys.AutoLinkTypes),
                        new AutoLink(TypeNamesToLink)
                            .WithQuerySelector("code")
                            .WithMatchOnlyWholeWord()
                            .WithStartWordSeparators('<')
                            .WithEndWordSeparators('>'),
                        new If(
                            (doc, ctx) => doc.String(Keys.SourceFileExt) == ".md",
                            new Replace("@", "&#64;"))));

        /// <inheritdoc cref="Web.Pipelines.Pages" />
        // Contains an ugly hack to re-escape @ symbols in Markdown since AngleSharp unescapes them if it
        // changes text content to add an auto link, can be removed if AngleSharp #494 is addressed
        [SourceInfo]
        public static Pages Pages { get; } = new Pages(
            nameof(Pages),
            new PagesSettings
            {
                IgnorePaths = ctx => new[] { ctx.DirectoryPath(DocsKeys.BlogPath)?.FullPath, "api" }
                    .Concat(ctx.List(DocsKeys.IgnoreFolders, Array.Empty<string>()))
                    .Where(x => x != null),
                MarkdownConfiguration = ctx => ctx.String(DocsKeys.MarkdownConfiguration),
                MarkdownExtensionTypes = ctx => ctx.List<Type>(DocsKeys.MarkdownExtensionTypes),
                ProcessIncludes = (doc, ctx) => doc.Bool(DocsKeys.ProcessIncludes),
                PrependLinkRoot = ctx => ctx.Bool(DocsKeys.MarkdownPrependLinkRoot),
                CreateTree = true,
                TreePlaceholderFactory = TreePlaceholderFactory
            })
                .InsertAfter(
                    Pages.RazorFiles,
                    new If(
                        ctx => ctx.Bool(DocsKeys.AutoLinkTypes),
                        new AutoLink(TypeNamesToLink)
                            .WithQuerySelector("code")
                            .WithMatchOnlyWholeWord()
                            .WithStartWordSeparators('<')
                            .WithEndWordSeparators('>'),
                        new If(
                            (doc, ctx) => doc.String(Keys.SourceFileExt) == ".md",
                            new Replace("@", "&#64;"))));

        /// <summary>
        /// Generates the index pages for blog posts.
        /// </summary>
        [SourceInfo]
        public static Archive BlogIndexes { get; } = new Archive(
            nameof(BlogIndexes),
            new ArchiveSettings
            {
                Pipelines = new string[] { BlogPosts },
                TemplateFile = ctx => "_BlogIndex.cshtml",
                Layout = "/_BlogLayout.cshtml",
                PageSize = ctx => ctx.Get(DocsKeys.BlogPageSize, int.MaxValue),
                Title = (doc, ctx) => ctx.Get(DocsKeys.BlogTitle, "Blog"),
                RelativePath = (doc, ctx) => $"{ctx.DirectoryPath(DocsKeys.BlogPath, ".").FullPath}"
            });

        /// <summary>
        /// Generates the category pages for blog posts.
        /// </summary>
        [SourceInfo]
        public static Archive BlogCategories { get; } = new Archive(
            nameof(BlogCategories),
            new ArchiveSettings
            {
                Pipelines = new string[] { BlogPosts },
                TemplateFile = ctx => "_BlogIndex.cshtml",
                Layout = "/_BlogLayout.cshtml",
                Group = (doc, ctx) => doc.List<string>(DocsKeys.Category),
                CaseInsensitiveGroupComparer = ctx => ctx.Bool(DocsKeys.CaseInsensitiveCategories),
                PageSize = ctx => ctx.Get(DocsKeys.CategoryPageSize, int.MaxValue),
                Title = (doc, ctx) => doc.String(Keys.GroupKey),
                RelativePath = (doc, ctx) => $"{ctx.DirectoryPath(DocsKeys.BlogPath, ".").FullPath}/{doc.String(Keys.GroupKey)}"
            });

        /// <summary>
        /// Generates the tag pages for blog posts.
        /// </summary>
        [SourceInfo]
        public static Archive BlogTags { get; } = new Archive(
            nameof(BlogTags),
            new ArchiveSettings
            {
                Pipelines = new string[] { BlogPosts },
                TemplateFile = ctx => "_BlogIndex.cshtml",
                Layout = "/_BlogLayout.cshtml",
                Group = (doc, ctx) => doc.List<string>(DocsKeys.Tags),
                CaseInsensitiveGroupComparer = ctx => ctx.Bool(DocsKeys.CaseInsensitiveTags),
                PageSize = ctx => ctx.Get(DocsKeys.TagPageSize, int.MaxValue),
                Title = (doc, ctx) => doc.String(Keys.GroupKey),
                RelativePath = (doc, ctx) => $"{ctx.DirectoryPath(DocsKeys.BlogPath, ".").FullPath}/tag/{doc.String(Keys.GroupKey)}"
            });

        /// <summary>
        /// Generates the author pages for blog posts.
        /// </summary>
        [SourceInfo]
        public static Archive BlogAuthors { get; } = new Archive(
            nameof(BlogAuthors),
            new ArchiveSettings
            {
                Pipelines = new string[] { BlogPosts },
                TemplateFile = ctx => "_BlogIndex.cshtml",
                Layout = "/_BlogLayout.cshtml",
                Group = (doc, ctx) => doc.List<string>(key: DocsKeys.Author),
                CaseInsensitiveGroupComparer = ctx => ctx.Bool(key: DocsKeys.CaseInsensitiveAuthors),
                PageSize = ctx => ctx.Get(key: DocsKeys.AuthorPageSize, defaultValue: int.MaxValue),
                Title = (doc, ctx) => doc.String(key: Keys.GroupKey),
                RelativePath = (doc, ctx) => $"{ctx.DirectoryPath(DocsKeys.BlogPath, ".").FullPath}/author/{doc.String(key: Keys.GroupKey)}"
            });

        /// <summary>
        /// Generates the monthly archive pages for blog posts.
        /// </summary>
        [SourceInfo]
        public static Archive BlogArchives { get; } = new Archive(
            nameof(BlogArchives),
            new ArchiveSettings
            {
                Pipelines = new string[] { BlogPosts },
                TemplateFile = ctx => "_BlogIndex.cshtml",
                Layout = "/_BlogLayout.cshtml",
                Group = (doc, ctx) => new DateTime(year: doc.Get<DateTime>(key: DocsKeys.Published).Year, month: doc.Get<DateTime>(key: DocsKeys.Published).Month, day: 1),
                PageSize = ctx => ctx.Get(key: DocsKeys.MonthPageSize, defaultValue: int.MaxValue),
                Title = (doc, ctx) => doc.Get<DateTime>(key: Keys.GroupKey).ToString(format: "MMMM, yyyy"),
                RelativePath = (doc, ctx) => $"{ctx.DirectoryPath(DocsKeys.BlogPath, ".").FullPath}/archive/{doc.Get<DateTime>(key: Keys.GroupKey):yyyy/MM}"
            });

        /// <summary>
        /// Generates the yearly archive pages for blog posts.
        /// </summary>
        [SourceInfo]
        public static Archive BlogYearlyArchives { get; } = new Archive(
            nameof(BlogYearlyArchives),
            new ArchiveSettings
            {
                Pipelines = new string[] { BlogPosts },
                TemplateFile = ctx => "_BlogIndex.cshtml",
                Layout = "/_BlogLayout.cshtml",
                Group = (doc, ctx) => new DateTime(year: doc.Get<DateTime>(key: DocsKeys.Published).Year, month: 1, day: 1),
                PageSize = ctx => ctx.Get(key: DocsKeys.MonthPageSize, defaultValue: int.MaxValue),
                Title = (doc, ctx) => doc.Get<DateTime>(key: Keys.GroupKey).ToString(format: "yyyy"),
                RelativePath = (doc, ctx) => $"{ctx.DirectoryPath(DocsKeys.BlogPath, ".").FullPath}/archive/{doc.Get<DateTime>(key: Keys.GroupKey):yyyy}"
            });

        /// <inheritdoc cref="Web.Pipelines.Feeds" />
        [SourceInfo]
        public static Web.Pipelines.Feeds BlogFeed { get; } = new Web.Pipelines.Feeds(
            nameof(BlogFeed),
            new FeedsSettings
            {
                Pipelines = new string[] { BlogPosts },
                RssPath = ctx => ctx.FilePath(DocsKeys.BlogRssPath),
                AtomPath = ctx => ctx.FilePath(DocsKeys.BlogAtomPath),
                RdfPath = ctx => ctx.FilePath(DocsKeys.BlogRdfPath)
            });

        /// <inheritdoc cref="Web.Pipelines.RenderPages" />
        [SourceInfo]
        public static RenderPages RenderPages { get; } = new RenderPages(
            nameof(RenderPages),
            new RenderPagesSettings
            {
                Pipelines = new string[] { Pages },
                Layout = (doc, ctx) => "/_Layout.cshtml"
            })
                .InsertAfter(
                    RenderPages.GetDocuments,
                    new Meta(DocsKeys.NoSidebar, (doc, ctx) => doc.Get(
                        DocsKeys.NoSidebar,
                        (doc.DocumentList(Keys.Children)?.Count ?? 0) == 0
                        && doc.Document(Keys.Parent) == null)))
                .InsertAfter(
                    RenderPages.WriteMetadata,
                    new HtmlInsert("div#infobar-headings", (doc, ctx) => ctx.GenerateInfobarHeadings(doc)));

        /// <inheritdoc cref="Web.Pipelines.RenderBlogPosts" />
        [SourceInfo]
        public static RenderBlogPosts RenderBlogPosts { get; } = new RenderBlogPosts(
            nameof(RenderBlogPosts),
            new RenderBlogPostsSettings
            {
                Pipelines = new string[] { BlogPosts },
                PublishedKey = DocsKeys.Published,
                Layout = (doc, ctx) => "/_BlogPost.cshtml"
            })
                .InsertAfter(
                    RenderPages.WriteMetadata,
                    new HtmlInsert("div#infobar-headings", (doc, ctx) => ctx.GenerateInfobarHeadings(doc)));

        /// <inheritdoc cref="Web.Pipelines.Redirects" />
        [SourceInfo]
        public static Redirects Redirects { get; } = new Redirects(
            nameof(Redirects),
            new RedirectsSettings
            {
                Pipelines = new string[] { RenderPages, RenderBlogPosts },
                MetaRefreshRedirects = ctx => ctx.Bool(DocsKeys.MetaRefreshRedirects),
                NetlifyRedirects = ctx => ctx.Bool(DocsKeys.NetlifyRedirects)
            });

        /// <inheritdoc cref="Pipelines.RenderApi" />
        [SourceInfo]
        public static RenderApi RenderApi { get; } = new RenderApi();

        /// <inheritdoc cref="Pipelines.ApiIndex" />
        [SourceInfo]
        public static ApiIndex ApiIndex { get; } = new ApiIndex();

        /// <inheritdoc cref="Pipelines.ApiSearchIndex" />
        [SourceInfo]
        public static ApiSearchIndex ApiSearchIndex { get; } = new ApiSearchIndex();

        /// <inheritdoc cref="Web.Pipelines.Less" />
        [SourceInfo]
        public static Web.Pipelines.Less Less { get; } = new Web.Pipelines.Less(nameof(Less));

        /// <inheritdoc cref="Web.Pipelines.Sass" />
        [SourceInfo]
        public static Web.Pipelines.Sass Sass { get; } = new Web.Pipelines.Sass(nameof(Sass));

        /// <inheritdoc cref="Web.Pipelines.Resources" />
        [SourceInfo]
        public static Resources Resources { get; } = new Resources(nameof(Resources));

        /// <inheritdoc cref="Web.Pipelines.ValidateLinks" />
        [SourceInfo]
        public static ValidateLinks ValidateLinks { get; } = new ValidateLinks(
            nameof(ValidateLinks),
            new ValidateLinksSettings
            {
                Pipelines = new string[] { RenderPages, RenderBlogPosts, RenderApi, Resources },
                ValidateAbsoluteLinks = ctx => ctx.Bool(DocsKeys.ValidateAbsoluteLinks),
                ValidateRelativeLinks = ctx => ctx.Bool(DocsKeys.ValidateRelativeLinks),
                ValidateLinksAsError = ctx => ctx.Bool(DocsKeys.ValidateLinksAsError)
            });

        /// <inheritdoc />
        public override void Apply(IEngine engine)
        {
            // Global metadata defaults
            engine.Settings[DocsKeys.SourceFiles] = new[]
            {
                "src/**/{!.git,!bin,!obj,!packages,!*.Tests,}/**/*.cs",
                "../src/**/{!.git,!bin,!obj,!packages,!*.Tests,}/**/*.cs"
            };
            engine.Settings[DocsKeys.IncludeGlobalNamespace] = true;
            engine.Settings[DocsKeys.IncludeDateInPostPath] = false;
            engine.Settings[DocsKeys.MarkdownConfiguration] = "advanced+bootstrap";
            engine.Settings[DocsKeys.ProcessIncludes] = true;
            engine.Settings[DocsKeys.SearchIndex] = true;
            engine.Settings[DocsKeys.MetaRefreshRedirects] = true;
            engine.Settings[DocsKeys.AutoLinkTypes] = true;
            engine.Settings[DocsKeys.BlogPath] = "blog";
            engine.Settings[DocsKeys.BlogTitle] = "Blog";
            engine.Settings[DocsKeys.BlogPageSize] = 5;
            engine.Settings[DocsKeys.CategoryPageSize] = 5;
            engine.Settings[DocsKeys.TagPageSize] = 5;
            engine.Settings[DocsKeys.AuthorPageSize] = 5;
            engine.Settings[DocsKeys.MonthPageSize] = 5;
            engine.Settings[DocsKeys.YearPageSize] = 5;
            engine.Settings[DocsKeys.BlogRssPath] = GenerateFeeds.DefaultRssPath;
            engine.Settings[DocsKeys.BlogAtomPath] = GenerateFeeds.DefaultAtomPath;
            engine.Settings[DocsKeys.BlogRdfPath] = GenerateFeeds.DefaultRdfPath;
            engine.Settings[DocsKeys.ApiPath] = "api";

            base.Apply(engine);
        }

        /// <inheritdoc />
        public override void Scaffold(IFile configFile, IDirectory inputDirectory)
        {
            // Config file
            configFile?.WriteAllText(@"#recipe Docs

// Customize your settings and add new ones here
Settings[Keys.Host] = ""host.com"";
Settings[DocsKeys.Title] = ""My Docs"";

// Add any pipeline customizations here");

            // Add info page
            inputDirectory.GetFile("about.md").WriteAllText(
@"Title: About This Project
---
This project is awesome!");

            // Add docs pages
            inputDirectory.GetFile("docs/command-line.md").WriteAllText(
@"Description: How to use the command line.
---
Here are some instructions on how to use the command line.");
            inputDirectory.GetFile("docs/usage.md").WriteAllText(
@"Description: Library usage instructions.
---
To use this library, take these steps...");

            // Add post page
            inputDirectory.GetFile("blog/new-release.md").WriteAllText(
@"Title: New Release
Published: 1/1/2016
Category: Release
Author: me
---
There is a new release out, go get it now.");
        }

        private static IDocument TreePlaceholderFactory(object[] path, MetadataItems items, IExecutionContext context)
        {
            FilePath indexPath = new FilePath(string.Join("/", path.Concat(new[] { "index.html" })));
            items.Add(Keys.RelativeFilePath, indexPath);
            items.Add(Keys.Title, Title.GetTitle(indexPath));
            return context.GetDocument(context.GetContentStream("@Html.Partial(\"_ChildPages\")"), items);
        }
    }
}
