﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MsTestAnalyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class TestHasTimeoutAnalyzer : TestMethodDiagnosticAnalyzer
	{
		private const string Title = @"Test must have an appropriate Timeout";
		public const string MessageFormat = @"Test must have an appropriate Timeout attribute.{0}";
		private const string Description = @"Tests that lack a Timeout may indefinitely block.";
		private const string Category = Categories.MsTest;

		public static readonly string DefaultTimeoutKey = "defaultTimeout";

		public static DiagnosticDescriptor Rule => new(Helper.ToDiagnosticId(DiagnosticId.TestHasTimeoutAttribute), Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: false, description: Description);


		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

		protected override TestMethodImplementation OnInitializeTestMethodAnalyzer(AnalyzerOptions options, Compilation compilation, MsTestAttributeDefinitions definitions)
		{
			var additionalFilesHelper = new AdditionalFilesHelper(options, compilation);

			return new TestHasTimeout(definitions, additionalFilesHelper);
		}

		private sealed class TestHasTimeout : TestMethodImplementation
		{
			private readonly AdditionalFilesHelper _additionalFilesHelper;
			private readonly object _lock1 = new();
			private readonly Dictionary<string, ImmutableList<string>> _configuredTimeouts = new();

			public TestHasTimeout(MsTestAttributeDefinitions definitions, AdditionalFilesHelper additionalFilesHelper) : base(definitions)
			{
				_additionalFilesHelper = additionalFilesHelper;
			}

			private bool TryGetAllowedTimeouts(string category, out ImmutableList<string> values)
			{
				lock (_lock1)
				{
					if (_configuredTimeouts.TryGetValue(category, out values))
					{
						return values != null;
					}
				}

				IReadOnlyList<string> allowedTimeouts = _additionalFilesHelper.GetValuesFromEditorConfig(Rule.Id, category);

				lock (_lock1)
				{
					if (allowedTimeouts.Count > 0)
					{
						_configuredTimeouts[category] = values = allowedTimeouts.ToImmutableList();
						return true;
					}
					else
					{
						_configuredTimeouts[category] = null;
						return false;
					}
				}
			}

			protected override void OnTestMethod(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol, bool isDataTestMethod)
			{
				SyntaxList<AttributeListSyntax> attributeLists = methodDeclaration.AttributeLists;

				AttributeHelper attributeHelper = new();
				var hasCategory = attributeHelper.HasAttribute(attributeLists, context, MsTestFrameworkDefinitions.TestCategoryAttribute, out _, out AttributeArgumentSyntax categoryArgumentSyntax);

				if (!attributeHelper.HasAttribute(attributeLists, context, MsTestFrameworkDefinitions.TimeoutAttribute, out Location timeoutLocation, out AttributeArgumentSyntax argumentSyntax))
				{
					ImmutableDictionary<string, string> additionalData = ImmutableDictionary<string, string>.Empty;

					//it doesn't have a timeout.  To help the fixer, see if it has a category...
					if (hasCategory && TryExtractAttributeArgument(context, categoryArgumentSyntax, out _, out string categoryForDiagnostic) && TryGetAllowedTimeouts(categoryForDiagnostic, out ImmutableList<string> allowed) && allowed.Any())
					{
						var firstAllowedTimeout = allowed.First();
						additionalData = additionalData.Add(DefaultTimeoutKey, firstAllowedTimeout);
					}

					Location location = methodDeclaration.Identifier.GetLocation();
					var diagnostic = Diagnostic.Create(Rule, location, additionalData, string.Empty);
					context.ReportDiagnostic(diagnostic);
					return;
				}

				if (!hasCategory)
				{
					return;
				}

				if (!TryExtractAttributeArgument(context, categoryArgumentSyntax, out _, out string category))
				{
					return;
				}

				if (!TryExtractAttributeArgument(context, argumentSyntax, out var timeoutString, out int _))
				{
					return;
				}

				if (IsIncorrectTimeout(timeoutString, category, out var errorText))
				{
					ImmutableDictionary<string, string> additionalData = ImmutableDictionary<string, string>.Empty;

					if (TryGetAllowedTimeouts(category, out ImmutableList<string> allowed) && allowed.Any())
					{
						var firstAllowed = allowed.First();
						additionalData = additionalData.Add(DefaultTimeoutKey, firstAllowed);
					}

					var diagnostic = Diagnostic.Create(Rule, timeoutLocation, additionalData, errorText);
					context.ReportDiagnostic(diagnostic);
				}
			}

			private bool TryExtractAttributeArgument<T>(SyntaxNodeAnalysisContext context, AttributeArgumentSyntax argumentSyntax, out string argumentString, out T value)
			{
				argumentString = argumentSyntax.Expression.ToString();

				SymbolInfo data = context.SemanticModel.GetSymbolInfo(argumentSyntax.Expression);

				if (data.Symbol == null)
				{
					value = default;
					return false;
				}

				if (data.Symbol is IFieldSymbol field && field.HasConstantValue && field.Type.Name == typeof(T).Name)
				{
					value = (T)field.ConstantValue;
					return true;
				}

				value = default;
				return false;
			}

			public bool IsIncorrectTimeout(string argumentString, string category, out string messageFormat)
			{
				if (!TryGetAllowedTimeouts(category, out ImmutableList<string> timeouts))
				{
					messageFormat = default;
					return false;
				}

				if (!timeouts.Contains(argumentString))
				{
					List<string> timeoutStrings = new();
					foreach (var timeoutString in timeouts)
					{
						timeoutStrings.Add($"\"{timeoutString}\"");
					}

					messageFormat = $"  Supported timeouts for category '{category}' are: {string.Join(", ", timeoutStrings)}";
					return true;
				}

				messageFormat = default;
				return false;
			}
		}
	}
}

