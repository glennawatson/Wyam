﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shouldly;
using Wyam.Common.Documents;
using Wyam.Common.Execution;
using Wyam.Common.Modules;
using Wyam.Core.Execution;
using Wyam.Core.Modules.Extensibility;
using Wyam.Testing;
using Wyam.Testing.Documents;
using Wyam.Testing.Execution;
using Wyam.Testing.Modules;

namespace Wyam.Core.Tests.Modules.Extensibility
{
    [TestFixture]
    [NonParallelizable]
    public class ExecuteFixture : BaseFixture
    {
        public class ExecuteTests : ExecuteFixture
        {
            [Test]
            public void DoesNotThrowForNullResultWithDocumentConfig()
            {
                // Given
                Engine engine = new Engine();
                Execute execute = new Execute((d, c) => null);
                engine.Pipelines.Add(execute);

                // When
                engine.Execute();

                // Then
            }

            [Test]
            public void DoesNotThrowForNullResultWithContextConfig()
            {
                // Given
                Engine engine = new Engine();
                Execute execute = new Execute(c => null);
                engine.Pipelines.Add(execute);

                // When
                engine.Execute();

                // Then
            }

            [Test]
            public void ThrowsForObjectResultWithContextConfig()
            {
                // Given
                Engine engine = new Engine();
                Execute execute = new Execute(c => 1);
                engine.Pipelines.Add(execute);

                // When, Then
                Assert.Throws<Exception>(() => engine.Execute());
            }

            [Test]
            public void ReturnsInputsForNullResultWithDocumentConfig()
            {
                // Given
                IExecutionContext context = new TestExecutionContext();
                IDocument[] inputs =
                {
                    new TestDocument(),
                    new TestDocument()
                };
                Execute execute = new Execute((d, c) => null);

                // When
                IEnumerable<IDocument> outputs = ((IModule)execute).Execute(inputs, context);

                // Then
                CollectionAssert.AreEqual(inputs, outputs);
            }

            [Test]
            public void ReturnsInputsForNullResultWithContextConfig()
            {
                // Given
                IExecutionContext context = new TestExecutionContext();
                IDocument[] inputs =
                {
                    new TestDocument(),
                    new TestDocument()
                };
                Execute execute = new Execute(c => null);

                // When
                IEnumerable<IDocument> outputs = ((IModule)execute).Execute(inputs, context);

                // Then
                CollectionAssert.AreEqual(inputs, outputs);
            }

            [Test]
            public void DoesNotRequireReturnValueForDocumentConfig()
            {
                // Given
                int a = 0;
                Engine engine = new Engine();
                Execute execute = new Execute((d, c) => { a = a + 1; });
                engine.Pipelines.Add(execute);

                // When
                engine.Execute();

                // Then
            }

            [Test]
            public void DoesNotRequireReturnValueForContextConfig()
            {
                // Given
                int a = 0;
                Engine engine = new Engine();
                Execute execute = new Execute(c => { a = a + 1; });
                engine.Pipelines.Add(execute);

                // When
                engine.Execute();

                // Then
            }

            [Test]
            public void ReturnsDocumentForSingleResultDocumentFromContextConfig()
            {
                // Given
                Engine engine = new Engine();
                IDocument document = new TestDocument();
                Execute execute = new Execute(c => document);
                engine.Pipelines.Add("Test", execute);

                // When
                engine.Execute();

                // Then
                CollectionAssert.AreEquivalent(new[] { document }, engine.Documents["Test"]);
            }

            [Test]
            public void ReturnsDocumentForSingleResultDocumentFromDocumentConfig()
            {
                // Given
                Engine engine = new Engine();
                IDocument document = new TestDocument();
                Execute execute = new Execute((d, c) => document);
                engine.Pipelines.Add("Test", execute);

                // When
                engine.Execute();

                // Then
                CollectionAssert.AreEquivalent(new[] { document }, engine.Documents["Test"]);
            }

            [Test]
            public void RunsModuleAgainstEachInputDocument()
            {
                // Given
                IExecutionContext context = new TestExecutionContext();
                IDocument[] inputs =
                {
                    new TestDocument(),
                    new TestDocument()
                };
                int count = 0;
                Execute execute = new Execute((d, c) =>
                {
                    count++;
                    return null;
                });

                // When
                ((IModule)execute).Execute(inputs, context).ToList();

                // Then
                count.ShouldBe(2);
            }

            [Test]
            public void RunsModuleAgainstInputDocuments()
            {
                // Given
                IExecutionContext context = new TestExecutionContext();
                IDocument[] inputs =
                {
                    new TestDocument(),
                    new TestDocument()
                };
                int count = 0;
                Execute execute = new Execute(c =>
                {
                    count++;
                    return null;
                });

                // When
                ((IModule)execute).Execute(inputs, context).ToList();

                // Then
                count.ShouldBe(1);
            }

            [Test]
            public void SetsNewContentForInputDocuments()
            {
                // Given
                IExecutionContext context = new TestExecutionContext();
                IDocument[] inputs =
                {
                    new TestDocument(),
                    new TestDocument()
                };
                int count = 0;
                Execute execute = new Execute((d, c) => count++);

                // When
                List<IDocument> results = ((IModule)execute).Execute(inputs, context).ToList();

                // Then
                CollectionAssert.AreEquivalent(results.Select(x => x.Content), new[] { "0", "1" });
            }
        }
    }
}
