﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	public class Helper
	{
		private static readonly char[] TrimCharacters = { '/', '\\' };

		public Helper(AnalyzerOptions options, Compilation compilation)
		{
			ForAdditionalFiles = new AdditionalFilesHelper(options, compilation);
			ForAttributes = new AttributeHelper();
			ForConstructors = new ConstructorSyntaxHelper();
			ForLiterals = new LiteralHelper();
			ForTests = new TestHelper();
		}

		public AdditionalFilesHelper ForAdditionalFiles { get; }

		public AttributeHelper ForAttributes { get; }

		public ConstructorSyntaxHelper ForConstructors { get; }

		public LiteralHelper ForLiterals { get; }

		public TestHelper ForTests { get; }

		public static string ToDiagnosticId(DiagnosticId id)
		{
			return @"PH" + ((int)id).ToString();
		}

		public static string ToHelpLinkUrl(string id)
		{
			return $"https://github.com/philips-software/roslyn-analyzers/blob/main/Documentation/Diagnostics/{id}.md";
		}

		public static string ToPrettyList(IEnumerable<Diagnostic> diagnostics)
		{
			IEnumerable<string> values = diagnostics.Select(diagnostic => diagnostic.Id);
			return string.Join(", ", values);
		}

		/// <summary>
		/// Checks for the presence of an "autogenerated" comment in the starting trivia for a file
		/// The compiler generates a version of the AssemblyInfo.cs file for certain projects (not named AssemblyInfo.cs), and this is how to pick it up
		/// </summary>
		public bool HasAutoGeneratedComment(CompilationUnitSyntax node)
		{
			if (node.FindToken(0).IsKind(SyntaxKind.EndOfFileToken))
			{
				return false;
			}

			SyntaxTriviaList first = node.GetLeadingTrivia();

			if (first.Count == 0)
			{
				return false;
			}

			var possibleHeader = first.ToFullString();


			var isAutogenerated = possibleHeader.Contains(@"<autogenerated />") || possibleHeader.Contains("<auto-generated");

			return isAutogenerated;
		}

		public bool IsExtensionClass(INamedTypeSymbol declaredSymbol)
		{
			return
				declaredSymbol is { MightContainExtensionMethods: true } &&
					!declaredSymbol.GetMembers().Any(m =>
						m.Kind == SymbolKind.Method &&
						m.DeclaredAccessibility == Accessibility.Public &&
						!((IMethodSymbol)m).IsExtensionMethod);
		}


		public string GetFileName(string filePath)
		{
			var nodes = filePath.Split(TrimCharacters);
			return nodes[nodes.Length - 1];
		}

		public bool IsAssemblyInfo(SyntaxNodeAnalysisContext context)
		{
			var fileName = GetFileName(context.Node.SyntaxTree.FilePath);

			return fileName.EndsWith("AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase);
		}

		public static bool IsNamespaceExempt(string myNamespace)
		{
			// https://developercommunity.visualstudio.com/t/error-cs0518-predefined-type-systemruntimecompiler/1244809
			List<string> exceptions = new() { "System.Runtime.CompilerServices" };
			return exceptions.Any(e => e == myNamespace);
		}

		public bool IsInheritingFromClass(INamedTypeSymbol inputType, string classTypeName)
		{
			INamedTypeSymbol type = inputType;
			while (type != null)
			{
				if (type.Name == classTypeName)
				{
					return true;
				}
				type = type.BaseType;
			}

			return false;
		}

		public bool IsUserControl(INamedTypeSymbol type)
		{
			return IsInheritingFromClass(type, @"ContainerControl");
		}

		public static IReadOnlyDictionary<string, string> GetUsingAliases(SyntaxNode node)
		{
			var list = new Dictionary<string, string>();
			SyntaxNode root = node.SyntaxTree.GetRoot();
			foreach (UsingDirectiveSyntax child in root.DescendantNodes(n => n is not TypeDeclarationSyntax).OfType<UsingDirectiveSyntax>())
			{
				if (child.Alias != null)
				{
					var alias = child.Alias.Name.GetFullName(list);
					var name = child.Name.GetFullName(list);
					list.Add(alias, name);
				}
			}
			return list;
		}

		public static bool IsCallableFromOutsideClass(MemberDeclarationSyntax method)
		{
			return method.Modifiers.Any(SyntaxKind.PublicKeyword) || method.Modifiers.Any(SyntaxKind.InternalKeyword) || method.Modifiers.Any(SyntaxKind.ProtectedKeyword);
		}
	}
}
