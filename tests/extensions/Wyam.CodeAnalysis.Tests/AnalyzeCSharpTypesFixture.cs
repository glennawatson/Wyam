﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using Wyam.Common.Documents;
using Wyam.Common.IO;
using Wyam.Common.Meta;
using Wyam.Common.Modules;
using Wyam.Common.Execution;

namespace Wyam.CodeAnalysis.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.Self | ParallelScope.Children)]
    public class AnalyzeCSharpTypesFixture : AnalyzeCSharpBaseFixture
    {
        public class ExecuteTests : AnalyzeCSharpTypesFixture
        {
            [Test]
            public void ReturnsAllTypes()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        public class Blue
                        {
                        }

                        class Green
                        {
                            class Red
                            {
                            }
                        }

                        internal struct Yellow
                        {
                        }

                        enum Orange
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                CollectionAssert.AreEquivalent(new[] { string.Empty, "Foo", "Blue", "Green", "Red", "Yellow", "Orange" }, results.Select(x => x["Name"]));
            }

            [Test]
            public void ReturnsExtensionMethods()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        public class Blue
                        {
                        }

                        public static class Green
                        {
                            public static void Ext(this Blue blue)
                            {
                            }
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                CollectionAssert.AreEquivalent(
                    new[] { "Ext" },
                    results.Single(x => x["Name"].Equals("Blue")).Get<IEnumerable<IDocument>>("ExtensionMethods").Select(x => x["Name"]));
            }

            [Test]
            public void ReturnsExtensionMethodsForBaseClass()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        public class Red
                        {
                        }

                        public class Blue : Red
                        {
                        }

                        public static class Green
                        {
                            public static void Ext(this Red red)
                            {
                            }
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                CollectionAssert.AreEquivalent(
                    new[] { "Ext" },
                    results.Single(x => x["Name"].Equals("Blue")).Get<IEnumerable<IDocument>>("ExtensionMethods").Select(x => x["Name"]));
            }

            [Test]
            public void MemberTypesReturnsNestedTypes()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Green
                        {
                            public class Blue
                            {
                            }

                            private struct Red
                            {
                            }

                            enum Yellow
                            {
                            }
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                CollectionAssert.AreEquivalent(
                    new[] { "Blue", "Red", "Yellow" },
                    results.Single(x => x["Name"].Equals("Green")).Get<IEnumerable<IDocument>>("MemberTypes").Select(x => x["Name"]));
            }

            [Test]
            public void FullNameContainsContainingType()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        public class Green
                        {
                            private class Blue
                            {
                            }
                        }

                        struct Red
                        {
                        }
                    }

                    namespace Foo.Bar
                    {
                        enum Yellow
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                CollectionAssert.AreEquivalent(new[] { string.Empty, "Foo", "Green", "Green.Blue", "Red", "Yellow", "Bar" }, results.Select(x => x["FullName"]));
            }

            [Test]
            public void DisplayNameContainsContainingType()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        public class Green
                        {
                            private class Blue
                            {
                            }
                        }

                        struct Red
                        {
                        }
                    }

                    namespace Foo.Bar
                    {
                        enum Yellow
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                CollectionAssert.AreEquivalent(new[] { "global", "Foo", "Green", "Green.Blue", "Red", "Yellow", "Foo.Bar" }, results.Select(x => x["DisplayName"]));
            }

            [Test]
            public void QualifiedNameContainsNamespaceAndContainingType()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        public class Green
                        {
                            private class Blue
                            {
                            }
                        }

                        struct Red
                        {
                        }
                    }

                    namespace Foo.Bar
                    {
                        enum Yellow
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                CollectionAssert.AreEquivalent(new[] { string.Empty, "Foo", "Foo.Green", "Foo.Green.Blue", "Foo.Red", "Foo.Bar.Yellow", "Foo.Bar" }, results.Select(x => x["QualifiedName"]));
            }

            [Test]
            public void ContainingNamespaceIsCorrect()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Green
                        {
                            class Blue
                            {
                            }
                        }

                        struct Red
                        {
                        }
                    }

                    namespace Foo.Bar
                    {
                        enum Yellow
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                Assert.AreEqual("Foo", results.Single(x => x["Name"].Equals("Green")).Get<IDocument>("ContainingNamespace")["Name"]);
                Assert.AreEqual("Foo", results.Single(x => x["Name"].Equals("Blue")).Get<IDocument>("ContainingNamespace")["Name"]);
                Assert.AreEqual("Foo", results.Single(x => x["Name"].Equals("Red")).Get<IDocument>("ContainingNamespace")["Name"]);
                Assert.AreEqual("Bar", results.Single(x => x["Name"].Equals("Yellow")).Get<IDocument>("ContainingNamespace")["Name"]);
            }

            [Test]
            public void ContainingTypeIsCorrect()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Green
                        {
                            class Blue
                            {
                            }
                        }

                        struct Red
                        {
                        }
                    }

                    namespace Foo.Bar
                    {
                        enum Yellow
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                Assert.IsNull(results.Single(x => x["Name"].Equals("Green")).Get<IDocument>("ContainingType"));
                Assert.AreEqual("Green", results.Single(x => x["Name"].Equals("Blue")).Get<IDocument>("ContainingType")["Name"]);
                Assert.IsNull(results.Single(x => x["Name"].Equals("Red")).Get<IDocument>("ContainingType"));
                Assert.IsNull(results.Single(x => x["Name"].Equals("Yellow")).Get<IDocument>("ContainingType"));
            }

            [Test]
            public void KindIsCorrect()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        public class Green
                        {
                            private class Blue
                            {
                            }
                        }

                        struct Red
                        {
                        }
                    }

                    namespace Foo.Bar
                    {
                        enum Yellow
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                Assert.AreEqual("NamedType", results.Single(x => x["Name"].Equals("Green"))["Kind"]);
                Assert.AreEqual("NamedType", results.Single(x => x["Name"].Equals("Blue"))["Kind"]);
                Assert.AreEqual("NamedType", results.Single(x => x["Name"].Equals("Red"))["Kind"]);
                Assert.AreEqual("NamedType", results.Single(x => x["Name"].Equals("Yellow"))["Kind"]);
            }

            [Test]
            public void SpecificKindIsCorrect()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        public class Green
                        {
                            private class Blue
                            {
                            }
                        }

                        struct Red
                        {
                        }
                    }

                    namespace Foo.Bar
                    {
                        enum Yellow
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                Assert.AreEqual("Class", results.Single(x => x["Name"].Equals("Green"))["SpecificKind"]);
                Assert.AreEqual("Class", results.Single(x => x["Name"].Equals("Blue"))["SpecificKind"]);
                Assert.AreEqual("Struct", results.Single(x => x["Name"].Equals("Red"))["SpecificKind"]);
                Assert.AreEqual("Enum", results.Single(x => x["Name"].Equals("Yellow"))["SpecificKind"]);
            }

            [Test]
            public void BaseTypeIsCorrect()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        public class Red
                        {
                        }

                        public class Green : Red
                        {
                        }

                        struct Blue
                        {
                        }

                        interface Yellow
                        {
                        }

                        interface Purple : Yellow
                        {
                        }

                        enum Orange
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                Assert.AreEqual("Object", results.Single(x => x["Name"].Equals("Red")).DocumentList("BaseTypes").First()["Name"]);
                Assert.AreEqual("Red", results.Single(x => x["Name"].Equals("Green")).DocumentList("BaseTypes").First()["Name"]);
                Assert.AreEqual("ValueType", results.Single(x => x["Name"].Equals("Blue")).DocumentList("BaseTypes").First()["Name"]);
                CollectionAssert.IsEmpty(results.Single(x => x["Name"].Equals("Yellow")).DocumentList("BaseTypes"));
                CollectionAssert.IsEmpty(results.Single(x => x["Name"].Equals("Purple")).DocumentList("BaseTypes"));
                Assert.AreEqual("Enum", results.Single(x => x["Name"].Equals("Orange")).DocumentList("BaseTypes").First()["Name"]);
            }

            [Test]
            public void MembersReturnsAllMembersExceptConstructors()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        public class Blue
                        {
                            public Blue()
                            {
                            }

                            void Green()
                            {
                            }

                            int Red { get; }

                            string _yellow;

                            event ChangedEventHandler Changed;
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                CollectionAssert.AreEquivalent(
                    new[] { "Green", "Red", "_yellow", "Changed", "ToString", "Equals", "Equals", "ReferenceEquals", "GetHashCode", "GetType", "Finalize", "MemberwiseClone" },
                    GetResult(results, "Blue").Get<IReadOnlyList<IDocument>>("Members").Select(x => x["Name"]));
            }

            [Test]
            public void ConstructorsIsPopulated()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        public class Blue
                        {
                            public Blue()
                            {
                            }

                            protected Blue(int x)
                            {
                            }

                            void Green()
                            {
                            }

                            int Red { get; }

                            string _yellow;

                            event ChangedEventHandler Changed;
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                Assert.AreEqual(2, GetResult(results, "Blue").Get<IReadOnlyList<IDocument>>("Constructors").Count);
            }

            [Test]
            public void WritePathIsCorrect()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Red
                        {
                        }

                        enum Green
                        {
                        }

                        namespace Bar
                        {
                            struct Blue
                            {
                            }
                        }
                    }

                    class Yellow
                    {
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                CollectionAssert.AreEquivalent(
                    new[] { "Foo/Green/index.html", "Foo.Bar/Blue/index.html", "global/Yellow/index.html", "Foo/Red/index.html" },
                    results.Where(x => x["Kind"].Equals("NamedType")).Select(x => ((FilePath)x[Keys.WritePath]).FullPath));
            }

            [Test]
            public void GetDocumentForExternalBaseType()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Red
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                Assert.AreEqual("Object", GetResult(results, "Red").DocumentList("BaseTypes").First()["Name"]);
            }

            [Test]
            public void GetDocumentsForExternalInterfaces()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Red : IBlue, IFoo
                        {
                        }

                        interface IBlue
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                CollectionAssert.AreEquivalent(new[] { string.Empty, "Foo", "Red", "IBlue" }, results.Select(x => x["Name"]));
                CollectionAssert.AreEquivalent(new[] { "IBlue", "IFoo" }, GetResult(results, "Red").Get<IEnumerable<IDocument>>("AllInterfaces").Select(x => x["Name"]));
            }

            [Test]
            public void GetDerivedTypes()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Red : IBlue, IFoo
                        {
                        }

                        class Blue : Red, IBlue
                        {
                        }

                        class Green : Blue
                        {
                        }

                        class Yellow : Blue
                        {
                        }

                        interface IBlue
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                CollectionAssert.AreEquivalent(new[] { "Blue" }, GetResult(results, "Red").Get<IEnumerable<IDocument>>("DerivedTypes").Select(x => x["Name"]));
                CollectionAssert.AreEquivalent(new[] { "Green", "Yellow" }, GetResult(results, "Blue").Get<IEnumerable<IDocument>>("DerivedTypes").Select(x => x["Name"]));
                CollectionAssert.IsEmpty(GetResult(results, "Green").Get<IEnumerable<IDocument>>("DerivedTypes"));
                CollectionAssert.IsEmpty(GetResult(results, "Yellow").Get<IEnumerable<IDocument>>("DerivedTypes"));
                CollectionAssert.IsEmpty(GetResult(results, "IBlue").Get<IEnumerable<IDocument>>("DerivedTypes"));
            }

            [Test]
            public void GetTypeParams()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Red<T>
                        {
                        }

                        interface IBlue<TKey, TValue>
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                CollectionAssert.AreEqual(new[] { "T" }, GetResult(results, "Red").Get<IEnumerable<IDocument>>("TypeParameters").Select(x => x["Name"]));
                CollectionAssert.AreEqual(new[] { "TKey", "TValue" }, GetResult(results, "IBlue").Get<IEnumerable<IDocument>>("TypeParameters").Select(x => x["Name"]));
            }

            [Test]
            public void TypeParamReferencesClass()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        class Red<T>
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                Assert.AreEqual("Red", GetResult(results, "Red").Get<IEnumerable<IDocument>>("TypeParameters").First().Get<IDocument>("DeclaringType")["Name"]);
            }

            [Test]
            public void TypesExcludedByPredicate()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        public class Blue
                        {
                        }

                        class Green
                        {
                            class Red
                            {
                            }
                        }

                        internal struct Yellow
                        {
                        }

                        enum Orange
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp().WhereSymbol(x => x is INamedTypeSymbol && ((INamedTypeSymbol)x).TypeKind == TypeKind.Class);

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                CollectionAssert.AreEquivalent(new[] { "Blue", "Green", "Red" }, results.Select(x => x["Name"]));
            }

            [Test]
            public void RestrictedToNamedTypes()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        public class Blue
                        {
                            public void Foo()
                            {
                            }
                        }

                        public interface Red
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp().WithNamedTypes();

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                CollectionAssert.AreEquivalent(new[] { "Blue", "Red" }, results.Select(x => x["Name"]));
            }

            [Test]
            public void RestrictedToNamedTypesWithPredicate()
            {
                // Given
                const string code = @"
                    namespace Foo
                    {
                        public class Blue
                        {
                            public void Foo()
                            {
                            }
                        }

                        public interface Red
                        {
                        }
                    }
                ";
                IDocument document = GetDocument(code);
                IExecutionContext context = GetContext();
                IModule module = new AnalyzeCSharp().WithNamedTypes(x => x.TypeKind == TypeKind.Class);

                // When
                List<IDocument> results = module.Execute(new[] { document }, context).ToList();  // Make sure to materialize the result list

                // Then
                CollectionAssert.AreEquivalent(new[] { "Blue" }, results.Select(x => x["Name"]));
            }
        }
    }
}