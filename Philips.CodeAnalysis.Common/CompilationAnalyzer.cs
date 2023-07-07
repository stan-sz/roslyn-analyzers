﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	public abstract class CompilationAnalyzer : DiagnosticAnalyzer
	{
		public DiagnosticId DiagnosticId { get; }
		public string Id { get; }
		protected DiagnosticDescriptor Rule { get; }

		protected CompilationAnalyzer(DiagnosticId id, string title, string messageFormat, string description, string category,
											DiagnosticSeverity severity = DiagnosticSeverity.Error, bool isEnabled = true)
		{
			DiagnosticId = id;
			Id = Helper.ToDiagnosticId(id);
			var helpLink = Helper.ToHelpLinkUrl(Id);
			Rule = new(Id, title, messageFormat, category, severity, isEnabled, description, helpLink);
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
	}
}