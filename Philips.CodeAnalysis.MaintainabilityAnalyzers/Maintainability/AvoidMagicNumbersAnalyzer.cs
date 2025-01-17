﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Philips.CodeAnalysis.Common;

namespace Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AvoidMagicNumbersAnalyzer : SingleDiagnosticAnalyzer<LiteralExpressionSyntax, AvoidMagicNumbersSyntaxNodeAction>
	{
		private const string Title = @"Avoid inline magic numbers";
		private const string MessageFormat = Title;
		private const string Description = @"Avoid inline magic number, define them as constant or include in an enumeration instead.";

		public AvoidMagicNumbersAnalyzer()
			: base(DiagnosticId.AvoidMagicNumbers, Title, MessageFormat, Description, Categories.Maintainability, isEnabled: false)
		{ }
	}

	public class AvoidMagicNumbersSyntaxNodeAction : SyntaxNodeAction<LiteralExpressionSyntax>
	{
		private const long FirstInvalidNumber = 3L;

		public override void Analyze()
		{
			TestHelper helper = new();
			if (helper.IsInTestClass(Context))
			{
				return;
			}

			if (!Node.Token.IsKind(SyntaxKind.NumericLiteralToken))
			{
				return;
			}

			if (IsAllowedNumber(Node.Token.Text))
			{
				return;
			}

			// Magic number are allowed in enumerations, as they give meaning to the number.
			if (Node.Ancestors().OfType<EnumMemberDeclarationSyntax>().Any())
			{
				return;
			}

			// If in a field, the magic number should be defined in a static field.
			FieldDeclarationSyntax field = Node.Ancestors().OfType<FieldDeclarationSyntax>().FirstOrDefault();
			if (field != null && IsStaticOrConst(field))
			{
				return;
			}

			LocalDeclarationStatementSyntax local = Node.Ancestors().OfType<LocalDeclarationStatementSyntax>().FirstOrDefault();
			if (local != null && local.Modifiers.Any(SyntaxKind.ConstKeyword))
			{
				return;
			}

			Location location = Node.GetLocation();
			ReportDiagnostic(location);
		}

		private static bool IsAllowedNumber(string text)
		{
			// Initialize with first number that is NOT allowed.
			var parsed = FirstInvalidNumber;
			var trimmed = text.ToLower(CultureInfo.InvariantCulture).TrimEnd('f', 'd', 'l', 'm', 'u');
			if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
			{
				parsed = integer;
			}
			else if (double.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
			{
				double rounded = (long)value;
				if (Math.Abs(rounded - value) < double.Epsilon)
				{
					parsed = (long)value;
				}
			}

			return
				parsed is -1 or 0 or 90 or 180 or 270 or 360 ||
				IsPowerOf(parsed, 2) ||
				IsPowerOf(parsed, 10);
		}

		private static bool IsPowerOf(long nut, int bas)
		{
			var current = 1L;
			while (nut > current)
			{
				current *= bas;
			}

			return nut == current;
		}

		private static bool IsStaticOrConst(FieldDeclarationSyntax field)
		{
			var isStatic = field.Modifiers.Any(SyntaxKind.StaticKeyword);
			var isConst = field.Modifiers.Any(SyntaxKind.ConstKeyword);
			return isStatic || isConst;
		}
	}
}
