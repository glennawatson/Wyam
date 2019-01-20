﻿using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wyam.Common.Documents;
using Wyam.Common.Execution;
using Wyam.Testing;
using Wyam.Testing.Documents;
using Wyam.Testing.Execution;

namespace Wyam.Html.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.Self | ParallelScope.Children)]
    public class HtmlEscapeFixture : BaseFixture
    {
        public class ExecuteTests : HtmlEscapeFixture
        {
            [Test]
            public void NoReplacementReturnsSameDocument()
            {
                // Given
                const string input = @"<html>
                        <head>
                            <title>Foobar</title>
                        </head>
                        <body>
                            <h1>Title</h1>
                            <p>This is some Foobar text</p>
                        </body>
                    </html>";
                TestExecutionContext context = new TestExecutionContext();
                TestDocument document = new TestDocument(input);
                HtmlEscape htmlEscape = new HtmlEscape();

                // When
                IList<IDocument> results = htmlEscape.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                Assert.That(results, Is.EquivalentTo(new[] { document }));
            }

            [Test]
            public void EscapeWith()
            {
                // Given
                const string input = @"<html>
                        <head>
                            <title>Foobar</title>
                        </head>
                        <body>
                            <h1>Die Sache mit dem Umlaut</h1>
                            <p>Lerchen-Lärchen-Ähnlichkeiten<br/>
                            fehlen.Dieses abzustreiten<br/>
                            mag im Klang der Worte liegen.<br/>
                            Merke, eine Lerch‘ kann fliegen,<br/>
                            Lärchen nicht, was kaum verwundert,<br/>
                            denn nicht eine unter hundert<br/>
                            ist geflügelt.Auch im Singen<br/>
                            sind die Bäume zu bezwingen.<br/>
                            <br/>
                            Die Bätrachtung sollte reichen,<br/>
                            Rächtschreibfählern auszuweichen.<br/>
                            Leicht gälingt’s, zu unterscheiden,<br/>
                            wär ist wär nun von dän beiden.</p>
                            <p>©Ingo Baumgartner, <u>2013</u><br/>
                            Aus der Sammlung<u>Humor, Satire und Nonsens</u>
                            </a>
                            </p>
                        </body>
                    </html>";
                const string output = @"<html>
                        <head>
                            <title>Foobar</title>
                        </head>
                        <body>
                            <h1>Die Sache mit dem Umlaut</h1>
                            <p>Lerchen-L&auml;rchen-&Auml;hnlichkeiten<br/>
                            fehlen.Dieses abzustreiten<br/>
                            mag im Klang der Worte liegen.<br/>
                            Merke, eine Lerch‘ kann fliegen,<br/>
                            L&auml;rchen nicht, was kaum verwundert,<br/>
                            denn nicht eine unter hundert<br/>
                            ist gefl&uuml;gelt.Auch im Singen<br/>
                            sind die B&auml;ume zu bezwingen.<br/>
                            <br/>
                            Die B&auml;trachtung sollte reichen,<br/>
                            R&auml;chtschreibf&auml;hlern auszuweichen.<br/>
                            Leicht g&auml;lingt’s, zu unterscheiden,<br/>
                            w&auml;r ist w&auml;r nun von d&auml;n beiden.</p>
                            <p>&copy;Ingo Baumgartner, <u>2013</u><br/>
                            Aus der Sammlung<u>Humor, Satire und Nonsens</u>
                            </a>
                            </p>
                        </body>
                    </html>";
                TestExecutionContext context = new TestExecutionContext();
                TestDocument document = new TestDocument(input);
                HtmlEscape htmlEscape = new HtmlEscape().WithEscapedChar('ä', 'ö', 'ü', 'Ä', 'Ö', 'Ü', 'ß', '©');

                // When
                IList<IDocument> results = htmlEscape.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                Assert.That(results.Select(x => x.Content), Is.EquivalentTo(new[] { output }));
            }

            [Test]
            public void EscapeNonStandard()
            {
                // Given
                const string input = @"<html>
                        <head>
                            <title>Foobar</title>
                        </head>
                        <body>
                            <h1>Title</h1>
                            <p>This is some Foobar text</p>
                        </body>
                    </html>";
                const string output = @"&lt;html&gt;
                        &lt;head&gt;
                            &lt;title&gt;Foobar&lt;&#47;title&gt;
                        &lt;&#47;head&gt;
                        &lt;body&gt;
                            &lt;h1&gt;Title&lt;&#47;h1&gt;
                            &lt;p&gt;This is some Foobar text&lt;&#47;p&gt;
                        &lt;&#47;body&gt;
                    &lt;&#47;html&gt;";
                TestExecutionContext context = new TestExecutionContext();
                TestDocument document = new TestDocument(input);
                HtmlEscape htmlEscape = new HtmlEscape().EscapeAllNonstandard().WithDefaultStandard();

                // When
                IList<IDocument> results = htmlEscape.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                Assert.That(results.Select(x => x.Content), Is.EquivalentTo(new[] { output }));
            }
        }
    }
}