﻿// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	/// <summary>
	/// Avoid the usage of <see cref="SuppressMessageAttribute"/>.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidSuppressMessageAttributeAnalyzer : DiagnosticAnalyzer
	{
		public const string AvoidSuppressMessageAttributeWhitelist = @"AvoidSuppressMessageAttributeWhitelist.txt";

		private static readonly AttributeModel attribute = GetAttributeModel();

		public static readonly ImmutableArray<DiagnosticDescriptor> Rules = ImmutableArray.Create(attribute.Rule);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Rules;

		private Helper _helper;

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

			context.RegisterCompilationStartAction(startContext =>
			{
				_helper = new Helper(startContext.Options, startContext.Compilation);
				ImmutableHashSet<string> whitelist = null;

				if (startContext.Compilation.GetTypeByMetadataName(attribute.FullName) != null)
				{

					whitelist ??= PopulateWhitelist(startContext.Options);

					startContext.RegisterSyntaxNodeAction(
						(c) => Analyze(c, whitelist),
						SyntaxKind.AttributeList);
				}
			});
		}

		private ImmutableHashSet<string> PopulateWhitelist(AnalyzerOptions options)
		{
			foreach (AdditionalText file in options.AdditionalFiles)
			{
				if (Path.GetFileName(file.Path) != AvoidSuppressMessageAttributeWhitelist)
				{
					continue;
				}

				Microsoft.CodeAnalysis.Text.SourceText text = file.GetText();

				ImmutableHashSet<string>.Builder builder = ImmutableHashSet.CreateBuilder<string>();
				if (text != null)
				{
					foreach (Microsoft.CodeAnalysis.Text.TextLine textLine in text.Lines)
					{
						var line = textLine.ToString();
						_ = builder.Add(line);
					}
				}

				return builder.ToImmutable();
			}

			return ImmutableHashSet<string>.Empty;
		}

		private void Analyze(SyntaxNodeAnalysisContext context, ImmutableHashSet<string> whitelist)
		{
			GeneratedCodeDetector generatedCodeDetector = new(_helper);
			if (generatedCodeDetector.IsGeneratedCode(context))
			{
				return;
			}

			var attributesNode = (AttributeListSyntax)context.Node;

			if (
				_helper.ForAttributes.HasAttribute(attributesNode, context, attribute.Name, attribute.FullName, out Location descriptionLocation) &&
				!IsWhitelisted(whitelist, context.SemanticModel, attributesNode.Parent, out var id))
			{
				var diagnostic = Diagnostic.Create(attribute.Rule, descriptionLocation, id);
				context.ReportDiagnostic(diagnostic);
			}
		}

		private bool IsWhitelisted(ImmutableHashSet<string> whitelist, SemanticModel semanticModel, SyntaxNode node, out string id)
		{
			ISymbol symbol = semanticModel.GetDeclaredSymbol(node);

			if (symbol == null)
			{
				id = null;
				return false;
			}

			id = symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

			return whitelist.Contains(id);
		}

		private static AttributeModel GetAttributeModel()
		{
			return new AttributeModel(@"SuppressMessage",
				@"System.Diagnostics.CodeAnalysis.SuppressMessageAttribute",
				@"SuppressMessage not allowed",
				@"SuppressMessage is not allowed.",
				@"SuppressMessage results in violations of codified coding guidelines.",
				DiagnosticId.AvoidSuppressMessage,
				isSuppressible: false,
				isEnabledByDefault: true);
		}
	}
}
