﻿// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class PositiveNamingAnalyzer : SingleDiagnosticAnalyzer
	{
		private const string Title = @"Positive Naming";
		private const string MessageFormat = @"Properties and variables should be named using positive wording.";
		private const string Description = MessageFormat;

		private static readonly string[] negativeWords = { "disable", "ignore", "missing", "absent" };

		private readonly TestHelper _testHelper;

		public PositiveNamingAnalyzer()
			: this(new TestHelper())
		{ }

		public PositiveNamingAnalyzer(TestHelper testHelper)
			: base(DiagnosticId.PositiveNaming, Title, MessageFormat, Description, Categories.Naming)
		{
			_testHelper = testHelper;
		}

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(AnalyzeVariable, SyntaxKind.VariableDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
		}

		private void AnalyzeVariable(SyntaxNodeAnalysisContext context)
		{
			var node = (VariableDeclarationSyntax)context.Node;

			GeneratedCodeDetector detector = new();
			if (detector.IsGeneratedCode(context))
			{
				return;
			}

			if (_testHelper.IsInTestClass(context))
			{
				return;
			}

			SeparatedSyntaxList<VariableDeclaratorSyntax> variables = node.Variables;
			if (!variables.Any())
			{
				return;
			}
			foreach (VariableDeclaratorSyntax variable in variables.Where(v => !IsPositiveName(v.Identifier.Text)))
			{
				Location loc = variable.GetLocation();
				CreateDiagnostic(context, loc);
			}
		}

		private void AnalyzeProperty(SyntaxNodeAnalysisContext context)
		{
			var node = (PropertyDeclarationSyntax)context.Node;

			GeneratedCodeDetector detector = new();
			if (detector.IsGeneratedCode(context))
			{
				return;
			}

			if (_testHelper.IsInTestClass(context))
			{
				return;
			}

			if (!IsPositiveName(node.Identifier.Text))
			{
				Location location = node.GetLocation();
				CreateDiagnostic(context, location);
			}
		}

		private void CreateDiagnostic(SyntaxNodeAnalysisContext context, Location location)
		{
			var diagnostic = Diagnostic.Create(Rule, location);
			context.ReportDiagnostic(diagnostic);
		}

		private bool IsPositiveName(string name)
		{
			var lower = name.ToLowerInvariant();
			return !negativeWords.Any(lower.Contains);
		}
	}
}
