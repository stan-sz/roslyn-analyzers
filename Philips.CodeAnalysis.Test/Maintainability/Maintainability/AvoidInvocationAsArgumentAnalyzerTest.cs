﻿// © 2021 Koninklijke Philips N.V. See License.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class AvoidInvocationAsArgumentAnalyzerTest : CodeFixVerifier
	{

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsArgumentTest()
		{
			var template = @"
class Foo
{{
  public Foo() : base(Do()) {}
  public Foo(string h) {}
  public static string Do() {{ return ""hi"";}}
  public string Moo(string s) {{ return ""hi"";}}
  public string ToString() {{ return ""hi"";}}
  public string ToList() {{ return ""hi"";}}
  public string ToArray() {{ return ""hi"";}}

  public void MyTest()
  {{
    string.Format(Foo.Do());
    string.Format(Foo.ToString());
    string.Format(Foo.ToList());
    string.Format(Foo.ToArray());
    string.Format(nameof(MyTest));
    string.Format(5.ToString());
    string.Format(Moo(Do()));	  // Finding
    Assert.Format(Moo("""").Format(""""));
  }}
}}
";
			await VerifyDiagnostic(template, DiagnosticId.AvoidInvocationAsArgument, line: 20, column: 19).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsArgumentReturnTest()
		{
			var errorContent = @"
class Foo
{{
  public string Do() {{ return ""hi"";}}
  public string Moo(string s) {{ return ""hi"";}}

  public string MyTest()
  {{
    return Moo(Do());
  }}
}}
";

			var fixedContent = @"
class Foo
{{
  public string Do() {{ return ""hi"";}}
  public string Moo(string s) {{ return ""hi"";}}

  public string MyTest()
  {{
    var s = Do();
    return Moo(s);
  }}
}}
";
			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsUnknownSymbolArgumentReturnTest()
		{
			var errorContent = @"
class Foo
{{
  public string Moo(string s) {{ return ""hi"";}}

  public string MyTest()
  {{
    return Moo(Do());
  }}
}}
";

			var fixedContent = @"
class Foo
{{
  public string Moo(string s) {{ return ""hi"";}}

  public string MyTest()
  {{
    var resultOfDo = Do();
    return Moo(resultOfDo);
  }}
}}
";
			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsArgumentFixTest()
		{
			var errorContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public void Moo(string x) { }
  public void MyTest()
  {
     Moo(Do());
  }
}
";
			var fixedContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public void Moo(string x) { }
  public void MyTest()
  {
     var x = Do();
     Moo(x);
  }
}
";

			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationInIfStatementTest()
		{
			var errorContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public bool Moo(string x) { }
  public void MyTest()
  {
     if (Moo(Do()))
       { var y = 1; }
  }
}
";
			var fixedContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public bool Moo(string x) { }
  public void MyTest()
  {
     var x = Do();
     if (Moo(x))
       { var y = 1; }
  }
}
";

			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationInWhileStatementTest()
		{
			var errorContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public bool Moo(string x) { }
  public void MyTest()
  {
     while (Moo(Do()))
       { var y = 1; }
  }
}
";
			var fixedContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public bool Moo(string x) { }
  public void MyTest()
  {
     var x = Do();
     while (Moo(x))
       { var y = 1; }
  }
}
";

			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsMemberAccessArgumentFixTest()
		{
			var errorContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public void Moo(string x) { }
  public void MyTest()
  {
     Moo(this.Do());
  }
}
";
			var fixedContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public void Moo(string x) { }
  public void MyTest()
  {
     var x = this.Do();
     Moo(x);
  }
}
";

			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsStaticMemberAccessArgumentFixTest()
		{
			var errorContent = @"
class Foo
{
  public static string Do() { return ""hi"";}
  public static void Moo(string value) { }
  public void MyTest()
  {
     Foo.Moo(Do());
  }
}
";
			var fixedContent = @"
class Foo
{
  public static string Do() { return ""hi"";}
  public static void Moo(string value) { }
  public void MyTest()
  {
     var resultOfDo = Do();
     Foo.Moo(resultOfDo);
  }
}
";

			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsMixedStaticAndInstanceMemberAccessArgumentFixTest()
		{
			var errorContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public static void Moo(string value) { }
  public void Moew(string value) { }
  public void MyTest()
  {
     Meow(Foo.Moo(Do()));
  }
}
";
			var fixedContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public static void Moo(string value) { }
  public void Moew(string value) { }
  public void MyTest()
  {
     var resultOfDo = Do();
     Meow(Foo.Moo(resultOfDo));
  }
}
";

			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsArgumentLocalAssignmentTest()
		{
			var errorContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public string Moo(string x) { }
  public void MyTest()
  {
     string y = Moo(Do());
  }
}
";
			var fixedContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public string Moo(string x) { }
  public void MyTest()
  {
     var x = Do();
     string y = Moo(x);
  }
}
";

			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsArgumentAssignmentTest()
		{
			var errorContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public string Moo(string x) { }
  public void MyTest()
  {
     string y;
     y = Moo(Do());
  }
}
";
			var fixedContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public string Moo(string x) { }
  public void MyTest()
  {
     string y;
     var x = Do();
     y = Moo(x);
  }
}
";

			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsArgumentFixReturnTest()
		{
			var errorContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public void Moo(string x) { }
  public string MyTest()
  {
     // Comment
     return Moo(Do());
  }
}
";
			var fixedContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public void Moo(string x) { }
  public string MyTest()
  {
     // Comment
     var x = Do();
     return Moo(x);
  }
}
";

			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsArgumentFixIfTest()
		{
			var errorContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public void Moo(string x) { }
  public void MyTest()
  {
     if (true)
       Moo(Do());
  }
}
";
			var fixedContent = @"
class Foo
{
  public string Do() { return ""hi"";}
  public void Moo(string x) { }
  public void MyTest()
  {
     if (true)
     {
       var x = Do();
       Moo(x);
     }
  }
}
";
			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsArgumentFixNewTest()
		{
			var errorContent = @"
class Meow { public Meow(string x) {} }
class Foo
{
  public string Do() { return ""hi"";}
  public void MyTest()
  {
     new Meow(Do());
  }
}
";
			var fixedContent = @"
class Meow { public Meow(string x) {} }
class Foo
{
  public string Do() { return ""hi"";}
  public void MyTest()
  {
     var x = Do();
     new Meow(x);
  }
}
";
			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsArgumentFixSwitchTest()
		{
			var errorContent = @"
class Meow { public Meow(string x) {} }
class Foo
{
  public string Do() { return ""hi"";}
  public void Moo(string x) { }
  public void MyTest()
  {
     int i;
     switch (i)
     {
       case 0: Moo(Do()); break;
     }
  }
}
";
			var fixedContent = @"
class Meow { public Meow(string x) {} }
class Foo
{
  public string Do() { return ""hi"";}
  public void Moo(string x) { }
  public void MyTest()
  {
     int i;
     switch (i)
     {
       case 0: var x = Do(); Moo(x); break;
     }
  }
}
";
			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsArgumentFixNamedArgumentsTest()
		{
			var errorContent = @"
class Meow { public Meow(string x) {} }
class Foo
{
  public string Do() { return ""hi"";}
  public void Moo(string x) { }
  public void MyTest()
  {
     int i;
     switch (i)
     {
       case 0: Moo(x: Do()); break;
     }
  }
}
";
			var fixedContent = @"
class Meow { public Meow(string x) {} }
class Foo
{
  public string Do() { return ""hi"";}
  public void Moo(string x) { }
  public void MyTest()
  {
     int i;
     switch (i)
     {
       case 0: var x = Do(); Moo(x: x); break;
     }
  }
}
";
			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
			await VerifyFix(errorContent, fixedContent).ConfigureAwait(false);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public async Task AvoidInvocationAsArgumentFixExpressionTest()
		{
			var errorContent = @"
class Meow { public Meow(string x) {} }
class Foo
{
  public string Do() { return ""hi"";}
  public void Moo(string x) { }
  public void MyTest() => Moo(Do());
}
";

			await VerifyDiagnostic(errorContent).ConfigureAwait(false);
		}

		protected override CodeFixProvider GetCodeFixProvider()
		{
			return new AvoidInvocationAsArgumentCodeFixProvider();
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AvoidInvocationAsArgumentAnalyzer();
		}
	}
}
